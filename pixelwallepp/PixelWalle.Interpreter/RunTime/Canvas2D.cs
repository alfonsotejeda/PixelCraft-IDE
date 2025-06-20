using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;              
using SixLabors.ImageSharp.PixelFormats; 
using SixLabors.ImageSharp.Processing;   
using PixelWalle.Interpreter.Errors;
using PixelWalle.Interpreter.Runtime; 

namespace PixelWalle.Interpreter.Runtime;

public class Canvas2D : ICanvas
{

	private Image<Rgba32> _image; // El lienzo real de SixLabors.ImageSharp
	private readonly int _width;
	private readonly int _height;

	private string _currentBrushName = "Black"; 
	private Rgba32 _currentBrushRgba = new Rgba32(0, 0, 0, 255); // Por defecto negro

	private int _brushSize = 1;

	private readonly ExecutionState _state;

	private static readonly Dictionary<string, Rgba32> _namedColorMap = new Dictionary<string, Rgba32>(StringComparer.OrdinalIgnoreCase)
	{
		{ "White", new Rgba32(255, 255, 255, 255) },
		{ "Black", new Rgba32(0, 0, 0, 255) },
		{ "Red", new Rgba32(255, 0, 0, 255) },
		{ "Green", new Rgba32(0, 255, 0, 255) },
		{ "Blue", new Rgba32(0, 0, 255, 255) },
		{ "Yellow", new Rgba32(255, 255, 0, 255) },
		{ "Transparent" , new Rgba32(0,0,0,0)}
	};


	public Canvas2D(int width, int height, ExecutionState state, byte[] initialImageBytes = null)
	{
		if (width <= 0 || height <= 0)
			throw new InterpreterException("Dimensiones del canvas deben ser positivas", 0, 0);

		_width = width;
		_height = height;
		_state = state;

		if (initialImageBytes != null && initialImageBytes.Length > 0)
		{
			try
			{
				// Cargar la imagen desde los bytes (Godot la enviaría)
				_image = Image.Load<Rgba32>(initialImageBytes);
				if (_image.Width != _width || _image.Height != _height)
				{
					// Esto es importante si el usuario cambia el tamaño del canvas en Godot.
					_image.Mutate(x => x.Resize(new Size(_width, _height)));
				}
			}
			catch (Exception ex)
			{
				// Manejar error de carga de imagen, creando una nueva vacía
				Console.Error.WriteLine($"Advertencia: No se pudo cargar la imagen inicial. Creando un lienzo vacío: {ex.Message}");
				_image = new Image<Rgba32>(_width, _height);
				_image.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.White)); // Inicializa con fondo transparente
			}
		}
		else
		{
			// Si no se proporcionan bytes, crea un nuevo lienzo transparente
			_image = new Image<Rgba32>(_width, _height);
			_image.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.White)); // Inicializa con fondo transparente
		}
		
		SetBrushColor("Black"); 
	}


	public void Clear()
	{
		_image.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.White));
	}

	public void Color(string colorInput)
	{
		colorInput = colorInput.Trim('"');
		SetBrushColor(colorInput);
	}

	private void SetBrushColor(string colorValue)
	{
		if (colorValue.StartsWith("#") && (colorValue.Length == 7 || colorValue.Length == 9))
		{
			try
			{
				byte r = Convert.ToByte(colorValue.Substring(1, 2), 16);
				byte g = Convert.ToByte(colorValue.Substring(3, 2), 16);
				byte b = Convert.ToByte(colorValue.Substring(5, 2), 16);
				byte a = 255;
				
				if (colorValue.Length == 9)
				{
					a = Convert.ToByte(colorValue.Substring(7, 2), 16);
				}

				_currentBrushRgba = new Rgba32(r, g, b, a);
				_currentBrushName = colorValue;
			}
			catch (FormatException)
			{
				throw new InterpreterException($"Formato de color hexadecimal no válido: \"{colorValue}\"", _state.CursorX, _state.CursorY);
			}
			catch (ArgumentOutOfRangeException)
			{
				throw new InterpreterException($"Formato de color hexadecimal incompleto o incorrecto: \"{colorValue}\"", _state.CursorX, _state.CursorY);
			}
		}
		else if (_namedColorMap.TryGetValue(colorValue, out Rgba32 namedRgba))
		{
			_currentBrushRgba = namedRgba;
			_currentBrushName = colorValue;
		}
		else
		{
			throw new InterpreterException($"Color no válido: \"{colorValue}\". Use nombres de color predefinidos o formato hexadecimal #RRGGBB[AA].", _state.CursorX, _state.CursorY);
		}
	}

	public void Size(int size)
	{
		if (size <= 0)
			throw new InterpreterException("El tamaño del pincel debe ser positivo", _state.CursorX, _state.CursorY);

		if (size % 2 == 0)
		{
			_brushSize = size-1;
		}
		else
		{
			_brushSize = size;
		}
	}

	public void DrawLine(int dx, int dy, int length)
	{
		DirectionValidator.EnsureValid(dx, dy, _state.CursorX, _state.CursorY);
		int x = _state.CursorX;
		int y = _state.CursorY;
		for (int step = 0; step < length; step++)
		{
			
			if ( _currentBrushName != "Transparent")
			{
				PaintBrush(x, y);
			}
			x += dx;
			y += dy;
		}
		_state.CursorX = x;
		_state.CursorY = y;
	}

	public void DrawRectangle(int dx, int dy, int distance, int width, int height)
	{
		int centerX = _state.CursorX + dx * distance;
		int centerY = _state.CursorY + dy * distance;
		int startX = centerX - width / 2;
		int startY = centerY - height / 2;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				bool borde = (x == 0 || x == width - 1 || y == 0 || y == height - 1);
				if (borde)
				{
					int px = startX + x;
					int py = startY + y;
					if ( _currentBrushName != "Transparent")
					{
						PaintBrush(px, py);
					};
				}
			}
		}
		_state.CursorX = centerX;
		_state.CursorY = centerY;
	}

	public void DrawCircle(int dx, int dy, int radius)
	{
		if (radius <= 0)
			throw new InterpreterException("El radio del círculo debe ser positivo", _state.CursorX, _state.CursorY);
		
		DirectionValidator.EnsureValid(dx, dy, _state.CursorX, _state.CursorY);

		int centerX = _state.CursorX + dx;
		int centerY = _state.CursorY + dy;

		for (int y = -radius; y <= radius; y++)
		{
			for (int x = -radius; x <= radius; x++)
			{
				double dist = Math.Sqrt(x * x + y * y);
				if (dist >= radius - 0.5 && dist <= radius + 0.5)
				{
					if ( _currentBrushName != "Transparent")
					{
						PaintBrush(centerX + x, centerY + y);
					}
				}
			}
		}
		_state.CursorX = centerX;
		_state.CursorY = centerY;
	}

	public void Fill()
	{

		Rgba32 startColor = GetPixelColor(_state.CursorX, _state.CursorY);
		if (startColor == _currentBrushRgba) return; // Compara con el valor RGBA real del pincel

		bool[,] visited = new bool[_height, _width];
		Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
		queue.Enqueue((_state.CursorX, _state.CursorY));

		while (queue.Count > 0)
		{
			(int x, int y) = queue.Dequeue();

			if (x < 0 || x >= _width || y < 0 || y >= _height) continue;
			if (visited[y, x]) continue;
			
			// Compara con el color inicial, no con el nombre
			if (GetPixelColor(x, y) != startColor) continue; 

			visited[y, x] = true;
			SetPixelColor(x, y, _currentBrushRgba); // Establece el color RGBA real

			queue.Enqueue((x + 1, y));
			queue.Enqueue((x - 1, y));
			queue.Enqueue((x, y + 1));
			queue.Enqueue((x, y - 1));
		}
	}

	public bool IsBrushColor(string color)
	{
		color = color.Trim('"');
		// Usamos SetBrushColor temporalmente para obtener el RGBA del color de comparación
		Rgba32 compareRgba;
		if (color.StartsWith("#"))
		{
			try {
				byte r = Convert.ToByte(color.Substring(1, 2), 16);
				byte g = Convert.ToByte(color.Substring(3, 2), 16);
				byte b = Convert.ToByte(color.Substring(5, 2), 16);
				byte a = 255;
				if (color.Length == 9) a = Convert.ToByte(color.Substring(7, 2), 16);
				compareRgba = new Rgba32(r,g,b,a);
			} catch { return false; } // Si no se puede parsear, no es igual
		}
		else if (_namedColorMap.TryGetValue(color, out Rgba32 namedRgba))
		{
			compareRgba = namedRgba;
		}
		else
		{
			return false; // Color no válido
		}
		return _currentBrushRgba.Equals(compareRgba);
	}

	public bool IsBrushSize(int size)
	{
		return _brushSize == size;
	}

	public bool IsCanvasColor(string color)
	{
		color = color.Trim('"');
		Rgba32 compareRgba;
		if (color.StartsWith("#"))
		{
			try {
				byte r = Convert.ToByte(color.Substring(1, 2), 16);
				byte g = Convert.ToByte(color.Substring(3, 2), 16);
				byte b = Convert.ToByte(color.Substring(5, 2), 16);
				byte a = 255;
				if (color.Length == 9) a = Convert.ToByte(color.Substring(7, 2), 16);
				compareRgba = new Rgba32(r,g,b,a);
			} catch { return false; }
		}
		else if (_namedColorMap.TryGetValue(color, out Rgba32 namedRgba))
		{
			compareRgba = namedRgba;
		}
		else
		{
			return false;
		}

		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				if (!GetPixelColor(x, y).Equals(compareRgba))
					return false;
			}
		}
		return true;
	}

	public (int width, int height) GetCanvasSize()
	{
		return (_width, _height);
	}

	public int GetColorCount(string color, int x1, int y1, int x2, int y2)
	{
		color = color.Trim('"');
		Rgba32 compareRgba;
		if (color.StartsWith("#"))
		{
			try {
				byte r = Convert.ToByte(color.Substring(1, 2), 16);
				byte g = Convert.ToByte(color.Substring(3, 2), 16);
				byte b = Convert.ToByte(color.Substring(5, 2), 16);
				byte a = 255;
				if (color.Length == 9) a = Convert.ToByte(color.Substring(7, 2), 16);
				compareRgba = new Rgba32(r,g,b,a);
			} catch { return 0; }
		}
		else if (_namedColorMap.TryGetValue(color, out Rgba32 namedRgba))
		{
			compareRgba = namedRgba;
		}
		else
		{
			return 0;
		}

		if (!IsInsideCanvas(x1, y1) || !IsInsideCanvas(x2, y2))
			return 0;

		int count = 0;
		int minX = Math.Min(x1, x2);
		int maxX = Math.Max(x1, x2);
		int minY = Math.Min(y1, y2);
		int maxY = Math.Max(y1, y2);

		for (int y = minY; y <= maxY; y++)
		{
			for (int x = minX; x <= maxX; x++)
			{
				if (GetPixelColor(x,y).Equals(compareRgba))
				{
					count++;
				}
			}
		}

		return count;
	}

	private bool IsInsideCanvas(int x, int y)
		=> x >= 0 && x < _width && y >= 0 && y < _height;

	private void Paint(int x, int y)
	{
		if (IsInsideCanvas(x, y))
			_image[x, y] = _currentBrushRgba; // Pinta con el color RGBA real
	}

	private void PaintBrush(int centerX, int centerY)
	{
		int r = _brushSize / 2;
		for (int dy = -r; dy <= r; dy++)
		{
			for (int dx = -r; dx <= r; dx++)
			{
				int px = centerX + dx;
				int py = centerY + dy;
				Paint(px, py);
			}
		}
	}
	

	public Rgba32 GetPixelColor(int x, int y)
	{
		if (x < 0 || x >= _width || y < 0 || y >= _height)
			throw new ArgumentOutOfRangeException($"Coordenadas fuera del canvas: ({x}, {y})");

		return _image[x, y];
	}


	public void SetPixelColor(int x, int y, Rgba32 color)
	{
		if (x < 0 || x >= _width || y < 0 || y >= _height)
			throw new ArgumentOutOfRangeException($"Coordenadas fuera del canvas: ({x}, {y})");
		_image[x, y] = color;
	}

	
	public string GetColorAt(int x, int y)
	{
		Rgba32 pixelColor = GetPixelColor(x,y);
		// Convierte el RGBA a su representación hexadecimal
		return $"#{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}{pixelColor.A:X2}";
	}

	public string DebugColorAt(int x, int y) 
	{
		Rgba32 pixelColor = GetPixelColor(x,y);
		// Representación más simple para debug
		if (pixelColor.Equals(_namedColorMap["White"])) return "W";
		if (pixelColor.Equals(_namedColorMap["Black"])) return "K";
		if (pixelColor.Equals(_namedColorMap["Red"])) return "R";
		if (pixelColor.Equals(_namedColorMap["Green"])) return "G";
		if (pixelColor.Equals(_namedColorMap["Blue"])) return "B";
		if (pixelColor.Equals(_namedColorMap["Yellow"])) return "Y";
		return "X"; // Cualquier otro color (hexadecimal)
	}

	public string DebugView()
	{
		StringBuilder builder = new StringBuilder();
		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				builder.Append(DebugColorAt(x, y));
			}
			builder.AppendLine();
		}
		return builder.ToString();
	}

	public byte[] SaveAsPngBytes()
	{
		using (MemoryStream ms = new MemoryStream())
		{
			_image.SaveAsPng(ms);
			return ms.ToArray();
		}
	}


	public void SetCursor(int x, int y)
	{
		_state.CursorX = x;
		_state.CursorY = y;
	}
	public void LoadFromPngBytes(byte[] pngBytes)
	{
		if (pngBytes == null || pngBytes.Length == 0)
		{
			throw new ArgumentException("Los bytes PNG no pueden ser nulos o vacíos.", nameof(pngBytes));
		}

		try
		{
			using (MemoryStream ms = new MemoryStream(pngBytes))
			{
				// SixLabors.ImageSharp.Image.Load<Rgba32> carga la imagen.
				// Es importante que la imagen se adapte a las dimensiones actuales del canvas.
				Image<Rgba32> loadedImage = Image.Load<Rgba32>(ms);
				if (loadedImage.Width != _width || loadedImage.Height != _height)
				{
					loadedImage.Mutate(x => x.Resize(new Size(_width, _height)));
				}
				_image = loadedImage; // Asigna la imagen cargada/redimensionada al canvas interno
			}
		}
		catch (Exception ex)
		{
			// Manejar errores de carga, quizás limpiando el canvas o re-lanzando.
			throw new InterpreterException($"Error al cargar la imagen PNG desde bytes: {ex.Message}", 0, 0);
		}
	}

}
