using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using PixelWalle.Interpreter.Errors;
using PixelWalle.Interpreter.Lexer;
using PixelWalle.Interpreter.Parser;
using PixelWalle.Interpreter.Runtime;
using PixelWalle.Interpreter.Semantic;
using PixelWalle.Interpreter.AST;
using System.Collections.Generic;

static class Program
{
    // --- Variables estáticas y persistentes para el estado del CLI ---
    private static Canvas2D _currentCanvas = default!; // Estado del lienzo de dibujo
    private static ExecutionState _currentExecutionState = default!; // Estado de la ejecución (cursor, etc.)
    private static string _loadedCode = string.Empty; // El código PixelWalle cargado previamente
    private static ProgramNode? _parsedProgram; // El Árbol de Sintaxis Abstracta (AST) del código parseado

    static void Main(string[] args)
    {
        // --- 1. Inicialización de variables de argumentos ---
        int canvasWidth = 1080;
        int canvasHeight = 720;
        string codeFilePath = string.Empty;
        bool checkOnly = false;
        bool clearCanvas = false;
        string? inputImageBase64 = null;
        int startLine = 1; // 1-basada para las líneas de código
        int linesToProcess = -1; // -1 significa "procesar hasta el final"

        // --- 2. Parseo de argumentos de línea de comandos ---
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--width":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedWidth))
                    {
                        canvasWidth = parsedWidth;
                        i++;
                    }
                    break;
                case "--height":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedHeight))
                    {
                        canvasHeight = parsedHeight;
                        i++;
                    }
                    break;
                case "--check":
                    checkOnly = true;
                    break;
                case "--clear-canvas":
                    clearCanvas = true;
                    break;
                case "--input-image-base64":
                    if (i + 1 < args.Length)
                    {
                        inputImageBase64 = args[i + 1];
                        i++;
                    }
                    break;
                case "--start-line":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedStartLine))
                    {
                        startLine = parsedStartLine;
                        i++;
                    }
                    break;
                case "--lines-to-process":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedLinesToProcess))
                    {
                        linesToProcess = parsedLinesToProcess;
                        i++;
                    }
                    break;
                default:
                    if (args[i].EndsWith(".gw"))
                    {
                        codeFilePath = args[i];
                    }
                    break;
            }
        }

        // --- 3. Validación inicial y carga de código ---
        string newSourceCode = string.Empty;
        if (!string.IsNullOrEmpty(codeFilePath))
        {
            if (!File.Exists(codeFilePath))
            {
                Console.WriteLine(JsonSerializer.Serialize(new {
                    errors = new[] {
                        new { line = 0, column = 0, message = $"ERROR: Archivo '{codeFilePath}' no encontrado." }
                    }
                }));
                return;
            }
            newSourceCode = File.ReadAllText(codeFilePath);
        }

        try
        {
            // --- 4. Lógica de "Check Only" ---
            // Este modo siempre re-tokeniza y re-parsea para asegurar que los errores
            // se reporten con el código más reciente, incluso si el _parsedProgram no se ha actualizado.
            if (checkOnly)
            {
                var tokens = new Lexer(newSourceCode).TokenizeWithRegex();
                var program = new Parser(tokens).ParseProgram();
                var semantic = new SemanticAnalyzer();
                semantic.Analyze(program);
                var semanticErrors = semantic.GetErrors();

                if (semanticErrors.Any())
                {
                    var errorJson = JsonSerializer.Serialize(new {
                        errors = semanticErrors.Select(ex => new {
                            line = ex.Line,
                            column = ex.Column,
                            message = ex.Message
                        }).ToArray()
                    });
                    Console.WriteLine(errorJson);
                }
                else
                {
                    Console.WriteLine("{\"errors\":[]}");
                }
                return; // Salir después del modo check
            }

            // --- 5. Lógica para re-compilar el código si ha cambiado o es la primera carga ---
            bool recompileCode = false;
            // Si el código actual en memoria es diferente al que se intenta cargar, o si no hay código cargado
            if (_loadedCode != newSourceCode || string.IsNullOrEmpty(_loadedCode))
            {
                recompileCode = true;
                _loadedCode = newSourceCode; // Actualiza el código cargado en memoria
                _parsedProgram = null; // Reinicia el AST para que se vuelva a parsear
            }

            // Si el código debe ser re-compilado y no está vacío
            if (recompileCode && !string.IsNullOrEmpty(_loadedCode))
            {
                var tokens = new Lexer(_loadedCode).TokenizeWithRegex();
                _parsedProgram = new Parser(tokens).ParseProgram();

                // Al re-compilar el código, también debemos reiniciar el estado de ejecución
                // para que la nueva ejecución comience desde cero.
                if (_currentExecutionState == null)
                {
                    _currentExecutionState = new ExecutionState();
                }
                _currentExecutionState.CursorX = 0;
                _currentExecutionState.CursorY = 0;
                _currentExecutionState.LastExecutedLine = 0; // Reiniciar la línea de ejecución
            }
            else if (string.IsNullOrEmpty(_loadedCode))
            {
                // Si el código está vacío, asegúrate de que _parsedProgram sea null
                _parsedProgram = null;
            }


            // --- 6. Inicializar o reconfigurar el canvas y el estado de ejecución ---
            byte[]? initialImageBytes = null;
            if (!string.IsNullOrEmpty(inputImageBase64))
            {
                try
                {
                    initialImageBytes = Convert.FromBase64String(inputImageBase64);
                }
                catch (FormatException)
                {
                    throw new InterpreterException("Formato Base64 de imagen inicial no válido.", 0, 0);
                }
            }

            bool reinitializeCanvas = _currentCanvas == null || _currentExecutionState == null ||
                                      clearCanvas ||
                                      (_currentCanvas.GetCanvasSize().width != canvasWidth || _currentCanvas.GetCanvasSize().height != canvasHeight);

            if (reinitializeCanvas)
            {
                // Si se limpia o se redimensiona, la imagen de entrada se ignora para inicializar transparente
                _currentCanvas = new Canvas2D(canvasWidth, canvasHeight, _currentExecutionState, clearCanvas ? null : initialImageBytes);
                
                // Reiniciar el cursor y la última línea procesada si el canvas se re-inicializa
                // (aunque ya lo hacemos al re-compilar, es buena redundancia si el canvas cambia solo por tamaño)
                if (_currentExecutionState == null)
                {
                    _currentExecutionState = new ExecutionState();
                }
                _currentExecutionState.CursorX = 0;
                _currentExecutionState.CursorY = 0;
                _currentExecutionState.LastExecutedLine = 0;
            }
            else if (initialImageBytes != null)
            {
                 // Si el canvas ya existe y se proporciona una nueva imagen base, recargarla
                _currentCanvas.LoadFromPngBytes(initialImageBytes);
            }
            
            // --- 7. Manejo específico para "Clear Canvas" sin ejecución de código ---
            if (clearCanvas && string.IsNullOrEmpty(codeFilePath))
            {
                byte[] cleanedImageBytes = _currentCanvas.SaveAsPngBytes();
                string base64ImageOutput = Convert.ToBase64String(cleanedImageBytes);
                Console.WriteLine(JsonSerializer.Serialize(new { image = base64ImageOutput, cursorX = _currentExecutionState.CursorX, cursorY = _currentExecutionState.CursorY, lastProcessedLine = _currentExecutionState.LastExecutedLine }));
                return;
            }

            // --- 8. Ejecución del código (chunks) ---
            if (_parsedProgram != null) // Solo ejecuta si hay un AST parseado
            {
                var interpreter = new Interpreter(_currentExecutionState, _currentCanvas, startLine, linesToProcess);
                interpreter.Execute(_parsedProgram); // Ejecutar el programa (parcialmente si se especificó)
                
                // La última línea procesada es crucial para que Godot sepa dónde continuar
                _currentExecutionState.LastExecutedLine = interpreter.LastExecutedLine;
            }
            else if (!string.IsNullOrEmpty(codeFilePath))
            {
                 // Si codeFilePath no está vacío pero _parsedProgram es null, significa que hubo un error de parseo/léxico
                 // que debería haber sido capturado anteriormente o el código estaba vacío.
                 // Podrías lanzar una excepción o registrar un error aquí si esto no debería ocurrir.
                 throw new InvalidOperationException("No se pudo ejecutar: El código fuente no se parseó correctamente o está vacío.");
            }

            // --- 9. Devolver el estado actual del canvas y la ejecución ---
            byte[] finalImageBytes = _currentCanvas.SaveAsPngBytes();
            string finalBase64Image = Convert.ToBase64String(finalImageBytes);
            Console.WriteLine(JsonSerializer.Serialize(new {
                image = finalBase64Image,
                cursorX = _currentExecutionState.CursorX,
                cursorY = _currentExecutionState.CursorY,
                lastProcessedLine = _currentExecutionState.LastExecutedLine
            }));
        }
        catch (InterpreterException ex)
        {
            var errorJson = JsonSerializer.Serialize(new {
                errors = new[] {
                    new {
                        line = ex.Line,
                        column = ex.Column,
                        message = ex.Message
                    }
                }
            });
            Console.WriteLine(errorJson);
        }
        catch (Exception ex)
        {
            var errorJson = JsonSerializer.Serialize(new {
                errors = new[] {
                    new {
                        line = 0,
                        column = 0,
                        message = $"Error inesperado en el backend: {ex.Message}"
                    }
                }
            });
            Console.WriteLine(errorJson);
        }
    }
}