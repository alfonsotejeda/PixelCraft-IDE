# PixelWalle: Intérprete y Entorno de Creación de Pixel Art

## Introducción

PixelWalle es un proyecto que combina un intérprete para un lenguaje de scripting personalizado (con extensión `.gw`) y un entorno de desarrollo integrado (IDE) simple construido con Godot Engine. El objetivo principal es permitir a los usuarios crear y manipular imágenes de pixel art mediante comandos de programación.

El sistema se compone de un backend en C# (.NET) que se encarga de la lógica de interpretación y renderizado de imágenes, y un frontend en Godot (PixelWallePP) que proporciona la interfaz de usuario para escribir código, visualizar el lienzo y gestionar archivos.

## Características Principales

*   **Lenguaje de Scripting Específico (`.gw`):** Un lenguaje simple diseñado para operaciones de dibujo 2D.
*   **IDE Básico en Godot:**
    *   Editor de texto para escribir código `.gw`.
    *   Visualización en tiempo real del lienzo de dibujo.
    *   Consola para mostrar errores de sintaxis, semántica y ejecución.
    *   Controles para ejecutar el código, cargar y guardar archivos `.gw`.
    *   Ajuste dinámico del tamaño del lienzo.
    *   Funcionalidad para limpiar el lienzo.
*   **Backend en C#:**
    *   **Intérprete Completo:** Incluye Lexer, Parser, Analizador Semántico y Ejecutor del AST (Árbol de Sintaxis Abstracta).
    *   **Manipulación de Imágenes:** Utiliza la biblioteca `SixLabors.ImageSharp` para crear y modificar el lienzo.
    *   **Comunicación vía CLI:** El frontend de Godot interactúa con el backend a través de una aplicación de línea de comandos (`PixelWalle.CLI`).
    *   **Salida JSON:** El CLI devuelve el estado del lienzo (imagen en Base64), la posición del cursor y los errores en formato JSON.
*   **Persistencia Parcial del Lienzo:** El estado del lienzo y del cursor puede mantenerse entre ejecuciones de código si no se redimensiona o limpia explícitamente.
*   **Manejo Detallado de Errores:** Los errores indican línea y columna para facilitar la depuración.

## Arquitectura del Proyecto

El proyecto se divide en tres componentes principales:

1.  **`PixelWalle.Interpreter` (Biblioteca C# .NET):**
    *   **`AST/`**: Define las clases para los nodos del Árbol de Sintaxis Abstracta (ej: `ProgramNode`, `FunctionCallNode`, `AssignmentNode`).
    *   **`Lexer/`**: Responsable de la tokenización del código fuente `.gw` (`Lexer.cs`, `Token.cs`, `TokenType.cs`).
    *   **`Parser/`**: Construye el AST a partir de la secuencia de tokens (`Parser.cs`).
    *   **`Semantic/`**: Realiza el análisis semántico sobre el AST para detectar errores lógicos o de tipos antes de la ejecución (`SemanticAnalyzer.cs`, `Type.cs`).
    *   **`RunTime/`**: Contiene la lógica de ejecución del AST (`Interpreter.cs`), la gestión del estado (`ExecutionState.cs`), la representación y manipulación del lienzo (`Canvas2D.cs`, `ICanvas.cs`) y la evaluación de expresiones (`Evaluator.cs`).
    *   **`Errors/`**: Define clases de excepciones personalizadas (`InterpreterException.cs`, `LexerException.cs`, `ParserException.cs`).

2.  **`PixelWalle.CLI` (Aplicación de Consola C# .NET):**
    *   Actúa como puente entre el frontend de Godot y la biblioteca `PixelWalle.Interpreter`.
    *   Recibe argumentos como la ruta al archivo `.gw`, dimensiones del lienzo, imagen base64 actual (opcional), y parámetros de ejecución parcial.
    *   Orquesta el proceso de lexing, parsing, análisis semántico e interpretación.
    *   Serializa el resultado (imagen, cursor, errores) a JSON y lo envía a la salida estándar.

3.  **`pixelwallepp` (Proyecto Godot Engine - Frontend):**
    *   **`main_interface.gd`**: Script principal que gestiona la interfaz de usuario (editor de código, panel de errores, visualización del lienzo, botones de control).
    *   Interactúa con `PixelWalle.CLI` mediante `OS.execute()`.
    *   Guarda el código del editor en un archivo temporal para pasarlo al CLI.
    *   Procesa la salida JSON del CLI para actualizar la textura del lienzo y mostrar errores.

## Pila Tecnológica

*   **Backend:** C# (.NET 9)
*   **Biblioteca de Imágenes (Backend):** `SixLabors.ImageSharp`
*   **Frontend:** Godot Engine (v4.x) con GDScript
*   **Formato de Intercambio de Datos:** JSON

## Estructura de Carpetas del Repositorio
/PixelWalle.Interpreter/
├── .gitignore
├── .idea/                            # Archivos de configuración del IDE (JetBrains)
│   └── .idea.PixelWalle.Interpreter/
├── PixelWalle.CLI/                   # Proyecto de la aplicación de línea de comandos
│   ├── .idea/                        # Archivos de configuración del IDE para el CLI
│   ├── PixelWalle.CLI.csproj
│   ├── Program.cs                    # Punto de entrada del CLI
│   └── code.gw                       # Archivo de ejemplo o temporal para pruebas
├── PixelWalle.Interpreter.sln        # Archivo de solución de Visual Studio
├── PixelWalle.Interpreter/           # Proyecto de la biblioteca del intérprete
│   ├── AST/
│   ├── Errors/
│   ├── FutureCorrections.txt         # Notas sobre futuras mejoras
│   ├── Lexer/
│   ├── Parser/
│   ├── PixelWalle.Interpreter.csproj
│   ├── RunTime/
│   └── Semantic/
├── PixelWalle.Tests/                 # Proyecto de pruebas unitarias
│   ├── ExecutionStateTests.cs
│   ├── InterpreterTest.cs
│   ├── ParserTests.cs
│   ├── RunTimeTest/                  # Pruebas específicas del Runtime
│   └── SemanticTest.cs
├── README.md                         # Este archivo
└── pixelwallepp/                     # Proyecto de Godot (Frontend)
├── .editorconfig
├── .gitattributes
├── .gitignore
├── Captura de pantalla ...png      # Imágenes (ej. capturas de pantalla)
├── Captura de pantalla ...png.import # Metadatos de importación de Godot
├── icon.svg
├── icon.svg.import
├── main_interface.gd             # Lógica principal de la UI
├── main_interface.gd.uid         # Identificador único de recurso de Godot
├── main_interface.tscn           # Escena principal de la UI de Godot
├── pictures_to_pixel_art/        # Posiblemente recursos o scripts adicionales
└── project.godot                 # Archivo de configuración del proyecto Godot


## Lenguaje PixelWalle (`.gw`)

El lenguaje `.gw` está diseñado para ser simple y enfocado en el dibujo. Algunos comandos y características clave incluyen:

*   **`Spawn(x, y)`**: Establece la posición inicial del cursor. Solo puede usarse una vez.
*   **`Color("nombre_color" | "#RRGGBB" | "#RRGGBBAA")`**: Define el color del pincel. Colores predefinidos: "Red", "Green", "Blue", "Black", "White", "Yellow", "Transparent".
*   **`Size(tamaño)`**: Define el grosor del pincel.
*   **`DrawLine(dx, dy, longitud)`**: Dibuja una línea desde la posición actual del cursor en la dirección (dx, dy) con la longitud especificada. `dx` y `dy` deben ser -1, 0, o 1, y no ambos cero.
*   **`DrawRectangle(dx, dy, distancia, ancho, alto)`**: Dibuja el contorno de un rectángulo. El centro del rectángulo se calcula desde la posición actual del cursor, moviéndose `distancia` unidades en la dirección (dx, dy).
*   **`DrawCircle(dx, dy, radio)`**: Dibuja el contorno de un círculo. El centro del círculo se calcula de forma similar a `DrawRectangle`.
*   **`Fill()`**: Rellena el área conectada en la posición actual del cursor con el color del pincel actual (similar a la herramienta "bote de pintura").
*   **`SetCursor(x, y)`**: Mueve el cursor a una posición absoluta en el lienzo.
*   **Variables:** `nombre_variable <- expresion` (ej: `x <- 10`, `y <- x + 5`).
*   **Expresiones:** Soporta operaciones aritméticas (`+`, `-`, `*`, `/`, `%`, `**`), comparaciones (`==`, `>`, `>=`, `<`, `<=`) y lógicas (`&&`, `||`).
*   **Control de Flujo (Limitado):**
    *   **`NombreEtiqueta`**: Define una etiqueta.
    *   **`GoTo [NombreEtiqueta] if (condicion)`**: Salta a la etiqueta si la condición (expresión booleana o entera no cero) es verdadera.
*   **Funciones de Consulta (para usar en expresiones):**
    *   `GetActualX()`: Devuelve la coordenada X actual del cursor.
    *   `GetActualY()`: Devuelve la coordenada Y actual del cursor.
    *   `GetCanvasSize()`: Devuelve un par (ancho, alto) del lienzo.
    *   `GetColorCount("color", x1, y1, x2, y2)`: Cuenta píxeles de un color en un área.
    *   `IsBrushColor("color")`: Verifica el color actual del pincel.
    *   `IsBrushSize(tamaño)`: Verifica el tamaño actual del pincel.
    *   `IsCanvasColor("color")`: Verifica el color del lienzo en la posición actual del cursor.

## Cómo Funciona (Flujo de Ejecución)

1.  **Entrada de Usuario (Godot):** El usuario escribe código `.gw` en el editor de texto del frontend `pixelwallepp`.
2.  **Ejecución (Godot):** Al presionar "Run", `main_interface.gd`:
    a.  Guarda el contenido del editor en un archivo `.gw` temporal.
    b.  Recopila las dimensiones actuales del lienzo y la imagen actual del lienzo (codificada en Base64, si existe).
    c.  Ejecuta `PixelWalle.CLI` como un proceso externo, pasando la ruta al archivo temporal, las dimensiones, y la imagen Base64 como argumentos.
3.  **Procesamiento (PixelWalle.CLI):**
    a.  Lee el código fuente del archivo `.gw`.
    b.  Si se proporciona una imagen Base64, la decodifica para inicializar el lienzo.
    c.  Instancia los componentes de `PixelWalle.Interpreter` (Lexer, Parser, SemanticAnalyzer, Interpreter, Canvas2D).
    d.  El código pasa por las fases de: Tokenización (Lexer) -> Construcción del AST (Parser) -> Análisis Semántico -> Ejecución (Interpreter).
    e.  Durante la ejecución, los comandos de dibujo modifican el objeto `Canvas2D` (que internamente usa `Image<Rgba32>` de SixLabors.ImageSharp).
    f.  Al finalizar, el CLI obtiene la imagen resultante del `Canvas2D` (codificada a PNG y luego a Base64), la posición final del cursor, y cualquier error ocurrido.
    g.  Serializa esta información en un objeto JSON y lo imprime en la consola (stdout).
4.  **Actualización de UI (Godot):**
    a.  `main_interface.gd` captura la salida JSON del CLI.
    b.  Parsea el JSON.
    c.  Si hay una imagen, la decodifica de Base64 y actualiza la `TextureRect` del lienzo.
    d.  Muestra los errores (si los hay) en el panel de errores.
    e.  Actualiza la representación visual del cursor (si aplica).

El modo `--check` del CLI permite un análisis de errores sin ejecutar el dibujo, usado por el frontend para validación en tiempo real mientras se escribe.

## Posibles Mejoras Futuras

(Basado en `FutureCorrections.txt` y observaciones generales)

*   Errores más detallados y descriptivos.
*   Mejoras en el rendimiento para lienzos grandes o scripts complejos.
*   Paleta de colores en la UI.
*   Herramientas de debugging más avanzadas en el frontend.

---

