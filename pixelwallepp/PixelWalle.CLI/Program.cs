// PixelWalle.Interpreter/PixelWalle.CLI/Program.cs

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
    // --- Hacemos el canvas y el estado estáticos y persistentes ---
    // (Asegúrate de que Canvas2D y ExecutionState sean inicializados en algún punto,
    // o usa 'default!' como vimos para CS8618)
    private static Canvas2D _currentCanvas = default!;
    private static ExecutionState _currentExecutionState = default!;

    static void Main(string[] args)
    {
        // Valores por defecto
        int canvasWidth = 1080;
        int canvasHeight = 720;
        string codeFilePath = string.Empty; // Inicializar a string.Empty
        bool checkOnly = false;
        bool clearCanvas = false;
        string? inputImageBase64 = null;
        int cursorX = 0;
        int cursorY = 0;
        string? base64Image = null; // Hazlo anulable
        int startLine = 1; // Por defecto, empieza en la línea 1 (1-basada)
        int linesToProcess = -1; // -1 significa "procesar hasta el final"

        // Parsear argumentos de línea de comandos
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
                case "--cursor-x": // No estrictamente necesario si el estado es persistente
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int x))
                    {
                        cursorX = x; // Si necesitas forzar el cursor, úsalo.
                        i++;
                    }
                    break;
                case "--cursor-y": // No estrictamente necesario si el estado es persistente
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int y))
                    {
                        cursorY = y; // Si necesitas forzar el cursor, úsalo.
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

        // Validación inicial
        if (string.IsNullOrEmpty(codeFilePath) && !clearCanvas)
        {
            Console.WriteLine(JsonSerializer.Serialize(new {
                errors = new[] {
                    new { line = 0, column = 0, message = "ERROR: No se proporcionó archivo .gw o comando de limpieza." }
                }
            }));
            return;
        }

        if (!string.IsNullOrEmpty(codeFilePath) && !File.Exists(codeFilePath))
        {
            Console.WriteLine(JsonSerializer.Serialize(new {
                errors = new[] {
                    new { line = 0, column = 0, message = $"ERROR: Archivo '{codeFilePath}' no encontrado o ruta vacía." }
                }
            }));
            return;
        }

        string sourceCode = string.Empty;
        if (!string.IsNullOrEmpty(codeFilePath))
        {
            sourceCode = File.ReadAllText(codeFilePath);
        }

        try
        {
            // Modo de verificación (checkOnly)
            if (checkOnly)
            {
                List<Token> tokens = new Lexer(sourceCode).TokenizeWithRegex();
                ProgramNode program = new Parser(tokens).ParseProgram();
                SemanticAnalyzer semantic = new SemanticAnalyzer();
                semantic.Analyze(program);
                List<SemanticException> semanticErrors = semantic.GetErrors();

                if (semanticErrors.Any())
                {
                    string errorJson = JsonSerializer.Serialize(new {
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

            // --- Modo de ejecución con canvas persistente ---
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

            // Inicializar o reconfigurar el canvas y el estado
            // Se inicializa o se recrea si:
            // 1. Es la primera vez (_currentCanvas es null)
            // 2. Se solicitó limpiar el canvas (clearCanvas es true)
            // 3. Las dimensiones del canvas cambiaron
            // 4. Se proporcionó una imagen inicial y no estamos en un clear.
            //    La carga de imagen inicial podría pasar por un clear previo si se desea.
            
            bool reinitializeCanvas = _currentCanvas == null || _currentExecutionState == null ||
                                      clearCanvas ||
                                      (_currentCanvas.GetCanvasSize().width != canvasWidth || _currentCanvas.GetCanvasSize().height != canvasHeight);

            if (reinitializeCanvas)
            {
                _currentExecutionState = new ExecutionState();
                // Si se limpia o se redimensiona, el initialImageBytes pasado al constructor
                // debe ser null para que Canvas2D lo inicialice como transparente.
                // Si inputImageBase64 tiene un valor, se usará para la inicialización
                // a menos que clearCanvas sea true, en cuyo caso se ignora.
                _currentCanvas = new Canvas2D(canvasWidth, canvasHeight, _currentExecutionState, clearCanvas ? null : initialImageBytes);
                
                // Reiniciar el cursor y la última línea procesada al inicializar o redimensionar
                _currentExecutionState.CursorX = 0;
                _currentExecutionState.CursorY = 0;
                _currentExecutionState.LastExecutedLine = 0; // También reiniciar la línea de ejecución
            }
            else if (initialImageBytes != null) // Si el canvas existe y se proporciona una nueva imagen base para cargar
            {
                 // Recargar la imagen en el canvas existente si se le pasó una nueva.
                 // Esto no afecta el cursor ni la última línea, se mantiene el estado.
                _currentCanvas.LoadFromPngBytes(initialImageBytes);
            }
            
            // Si solo se solicitó limpiar y no hay código para ejecutar
            if (clearCanvas && string.IsNullOrEmpty(codeFilePath))
            {
                byte[] cleanedImageBytes = _currentCanvas.SaveAsPngBytes();
                base64Image = Convert.ToBase64String(cleanedImageBytes);
                Console.WriteLine(JsonSerializer.Serialize(new { image = base64Image, cursorX = _currentExecutionState.CursorX, cursorY = _currentExecutionState.CursorY, lastProcessedLine = 0 }));
                return;
            }

            // Si hay código para ejecutar, ejecutarlo
            if (!string.IsNullOrEmpty(sourceCode))
            {
                List<Token> tokens = new Lexer(sourceCode).TokenizeWithRegex();
                ProgramNode program = new Parser(tokens).ParseProgram(); // Parsear el programa completo una vez

                // Crear el intérprete con los parámetros de rango de ejecución
                Interpreter interpreter = new Interpreter(_currentExecutionState, _currentCanvas, startLine, linesToProcess);
                interpreter.Execute(program); // Ejecutar el programa (parcialmente si se especificó)
                
                // Obtener la última línea procesada del intérprete
                _currentExecutionState.LastExecutedLine = interpreter.LastExecutedLine;
            }

            // Devolver el bitmap final como Base64
            byte[] finalImageBytes = _currentCanvas.SaveAsPngBytes();
            base64Image = Convert.ToBase64String(finalImageBytes);
            Console.WriteLine(JsonSerializer.Serialize(new {
                image = base64Image,
                cursorX = _currentExecutionState.CursorX,
                cursorY = _currentExecutionState.CursorY,
                lastProcessedLine = _currentExecutionState.LastExecutedLine
            }));
        }
        catch (InterpreterException ex)
        {
            string errorJson = JsonSerializer.Serialize(new {
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
            string errorJson = JsonSerializer.Serialize(new {
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