# (Tu script de Godot, por ejemplo, main_interface.gd)
extends Control

@onready var editor = $MarginContainer/VBoxContainer/HSplitContainer/Editor
@onready var error_log = $MarginContainer/VBoxContainer/ErrorPanel/ErrorLog
@onready var error_timer = $MarginContainer/VBoxContainer/ErrorTimer
@onready var canvas_panel = $MarginContainer/VBoxContainer/HSplitContainer/Panel
@onready var texture_rect = $MarginContainer/VBoxContainer/HSplitContainer/Panel/TextureRect
@onready var run_button = $MarginContainer/VBoxContainer/ToolBar/RunButton
@onready var load_button = $MarginContainer/VBoxContainer/ToolBar/LoadButton
@onready var save_button = $MarginContainer/VBoxContainer/ToolBar/SaveButton
@onready var load_file_dialog = $LoadFileDialog
@onready var save_file_dialog = $SaveFileDialog
@onready var clear_button = $MarginContainer/VBoxContainer/ToolBar/ClearButton

@onready var width_input = $MarginContainer/VBoxContainer/ToolBar/WidthInput
@onready var height_input = $MarginContainer/VBoxContainer/ToolBar/HeightInput
@onready var resize_button = $MarginContainer/VBoxContainer/ToolBar/ResizeButton

@onready var image_process_progress_bar = $MarginContainer/VBoxContainer/ToolBar/ImageProcessProgressBar
@onready var image_process_label = $MarginContainer/VBoxContainer/ToolBar/ImageProcessLabel

@onready var cursor_sprite = $MarginContainer/VBoxContainer/HSplitContainer/Panel/TextureRect/Sprite2D

const CLI_PATH := "/Users/alfonso/Documents/Pro_Project/PixelWalle/PixelWalle.Interpreter/PixelWalle.CLI/bin/Debug/net9.0/publish_osx-arm64/PixelWalle.CLI"

const PIXEL_ART_CELL_SIZE := 4

var _current_canvas_base64: String = ""
var _current_cursor_x: int = 0
var _current_cursor_y: int = 0

func _ready():
	error_timer.timeout.connect(_check_for_errors)
	editor.text_changed.connect(_on_code_changed)
	run_button.pressed.connect(_run_backend)
	load_button.pressed.connect(_on_load_button_pressed)
	save_button.pressed.connect(_on_save_button_pressed)
	load_file_dialog.file_selected.connect(_on_load_file_selected)
	save_file_dialog.file_selected.connect(_on_save_file_selected)
	resize_button.pressed.connect(_on_resize_button_pressed)
	clear_button.pressed.connect(_on_clear_button_pressed)

	if error_log is TextEdit:
		error_log.editable = false
	elif error_log is LineEdit:
		error_log.editable = false

	texture_rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	texture_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	
	# Asegurarse de que el TextureRect también use filtro Nearest
	if texture_rect.texture: # Si ya tiene una textura asignada en el editor
		texture_rect.texture.set_filter(CanvasItem.TEXTURE_FILTER_NEAREST)
	
	# Asegurarse de que el sprite del cursor también use filtro Nearest
	#if cursor_sprite and cursor_sprite.texture:
		#cursor_sprite.texture.set_filter(CanvasItem.TEXTURE_FILTER_NEAREST) # Esta es la línea que fallaba si era CompressedTexture2D

	_clear_canvas_frontend()
	_clear_canvas_backend()

	width_input.text = "40"
	height_input.text = "40"

	image_process_progress_bar.hide()
	image_process_label.hide()
	image_process_progress_bar.value = 0

	_update_cursor_sprite_visibility_and_position()


func _on_code_changed():
	error_timer.start()

func _check_for_errors():
	print("[DEBUG] Ejecutando análisis de errores (solo check, no dibuja)...")
	clear_errors()
	print("[DEBUG] Errores limpiados")
	var code = editor.text
	var path := _save_code_to_temp_file(code)
	var output_check := []
	
	var exit_code := OS.execute(CLI_PATH, ["--check", path], output_check)

	print("[DEBUG] Exit code (check):", exit_code)
	print("[DEBUG] CLI output (check):", output_check)

	var backend_raw_output = "\n".join(output_check)
	print("[DEBUG] Resultado del backend (check):\n", backend_raw_output)

	_process_backend_output(backend_raw_output, exit_code)
	
	error_log.set_deferred("text", error_log.text)


func _run_backend():
	print("[DEBUG] Ejecutando código para renderizar el canvas...")
	clear_errors()
	print("[DEBUG] Errores limpiados antes de la ejecución.")

	var code_path := _save_code_to_temp_file(editor.text)
	var output_run := []
	
	var current_width = int(width_input.text) if width_input.text.is_valid_int() and int(width_input.text) > 0 else 40
	var current_height = int(height_input.text) if height_input.text.is_valid_int() and int(height_input.text) > 0 else 40
	
	print("[DEBUG] Intentando ejecutar CLI con dimensiones:", current_width, "x", current_height)
	
	var cli_args = [code_path, "--width", str(current_width), "--height", str(current_height)]
	
	if not _current_canvas_base64.is_empty():
		cli_args.append("--input-image-base64")
		cli_args.append(_current_canvas_base64)
	
	var exit_code := OS.execute(CLI_PATH, cli_args, output_run)

	print("[DEBUG] Exit code (run):", exit_code)
	print("[DEBUG] Raw output (run):\n", output_run)

	var backend_raw_output = "\n".join(output_run)
	print("[DEBUG] Resultado del backend (run):\n", backend_raw_output)

	_process_backend_output(backend_raw_output, exit_code, true)
	
	error_log.set_deferred("text", error_log.text)

func _process_backend_output(backend_raw_output: String, exit_code: int, is_run_command: bool = false):
	var error_processed_by_json = false
	var final_error_message = ""
	var line_from_text_error = -1

	#_current_cursor_x = -1
	#_current_cursor_y = -1

	var json_start_idx = backend_raw_output.find("{")

	var non_json_prefix_text = ""
	if json_start_idx != -1:
		non_json_prefix_text = backend_raw_output.substr(0, json_start_idx).strip_edges()
		var json_string = backend_raw_output.substr(json_start_idx)
		var json_parse_result = JSON.parse_string(json_string)
		
		if json_parse_result is Dictionary:
			var json: Dictionary = json_parse_result
			
			if json.has("errors") and json.errors is Array:
				for err in json.errors:
					if err is Dictionary and err.has("message") and err.has("line") and err.has("column"):
						final_error_message += "Línea %d, Col %d: %s\n" % [err.line, err.column, err.message]
						mark_error_in_editor(err.line, err.message)
						error_processed_by_json = true
			elif is_run_command and json.has("image") and json.image is String:
				_current_canvas_base64 = json.image
				var image_bytes = Marshalls.base64_to_raw(_current_canvas_base64)
				var image = Image.new()
				var error = image.load_png_from_buffer(image_bytes)
				if error == OK:
					var texture = ImageTexture.create_from_image(image)
					# --- APLICAR FILTRO NEAREST AL ImageTexture del canvas ---
					#texture.set_filter(CanvasItem.TEXTURE_FILTER_NEAREST)
					texture_rect.texture = texture
					print("[DEBUG] Canvas PNG actualizado desde el backend.")
					
					if json.has("cursorX") and json.has("cursorY"):
						_current_cursor_x = int(json.cursorX)
						_current_cursor_y = int(json.cursorY)
						print("Cursor final del backend: ({cursor_x}, {cursor_y})".format({
						"cursor_x": _current_cursor_x,
						"cursor_y": _current_cursor_y
					}))
					else:
						print("[DEBUG] El JSON de la imagen no contiene 'cursor_x' o 'cursor_y'.")
						_current_cursor_x = -1
						_current_cursor_y = -1

				else:
					final_error_message += "Error: No se pudo cargar la imagen PNG desde Base64 (código: %d)\n" % error
					print("[DEBUG] Error de Godot al cargar imagen desde Base64:", error)
				error_processed_by_json = true
	
	if not error_processed_by_json:
		if not non_json_prefix_text.is_empty():
			final_error_message += non_json_prefix_text + "\n"
			var line_match = non_json_prefix_text.find("línea ")
			if line_match != -1:
				var end_line_num_idx = non_json_prefix_text.find(",", line_match)
				if end_line_num_idx != -1:
					var line_str = non_json_prefix_text.substr(line_match + "línea ".length(), end_line_num_idx - (line_match + "línea ".length())).strip_edges()
					if line_str.is_valid_int():
						line_from_text_error = int(line_str)
						mark_error_in_editor(line_from_text_error, non_json_prefix_text)
		
		if json_start_idx == -1:
			if exit_code != 0:
				final_error_message += "Error del backend (código de salida %d):\n%s\n" % [exit_code, backend_raw_output]
			elif not backend_raw_output.is_empty():
				if final_error_message.is_empty():
					final_error_message += backend_raw_output + "\n"
					var line_match = backend_raw_output.find("línea ")
					if line_match != -1:
						var end_line_num_idx = backend_raw_output.find(",", line_match)
						if end_line_num_idx != -1:
							var line_str = backend_raw_output.substr(line_match + "línea ".length(), end_line_num_idx - (line_match + "línea ".length())).strip_edges()
							if line_str.is_valid_int():
								line_from_text_error = int(line_str)
								mark_error_in_editor(line_from_text_error, backend_raw_output)
		
		if final_error_message.is_empty() and exit_code == 0 and backend_raw_output.is_empty():
			final_error_message = ""
		#elif final_error_message.is_empty() and exit_code == 0 and not backend_raw_output.is_empty():
			#final_error_message = "Salida inesperada del backend con código de salida 0: %s\n" % backend_raw_output.strip_edges()
			
	error_log.text = final_error_message.strip_edges()

	_update_cursor_sprite_visibility_and_position()


func _save_code_to_temp_file(code: String) -> String:
	var dir := OS.get_user_data_dir()
	var path := dir.path_join("temp.gw")
	var file := FileAccess.open(path, FileAccess.WRITE)

	print("[DEBUG] Ruta del archivo temporal:", path)
	print("[DEBUG] Longitud del código a guardar:", code.length())

	if file:
		file.store_string(code)
		file.close()
		print("[DEBUG] Archivo temporal guardado exitosamente.")
	else:
		push_error("No se pudo guardar archivo temporal para el backend.")
	return path

func clear_errors():
	error_log.text = ""
	for i in editor.get_line_count():
		editor.set_line_background_color(i, Color.TRANSPARENT)

func append_error(message: String, line: int, column: int):
	pass

func mark_error_in_editor(line: int, message: String):
	editor.set_line_background_color(line - 1, Color(1, 0.8, 0.8))

func _clear_canvas_frontend():
	texture_rect.texture = null
	print("[DEBUG] Canvas de TextureRect limpiado en el frontend.")
	_current_cursor_x = -1
	_current_cursor_y = -1
	_update_cursor_sprite_visibility_and_position()


func _clear_canvas_backend():
	print("[DEBUG] Notificando al backend para que limpie el canvas...")
	_current_canvas_base64 = ""
	
	var current_width = int(width_input.text) if width_input.text.is_valid_int() and int(width_input.text) > 0 else 40
	var current_height = int(height_input.text) if height_input.text.is_valid_int() and int(height_input.text) > 0 else 40
	
	var cli_args = ["--clear-canvas", "--width", str(current_width), "--height", str(current_height)]
	var output_clear := []
	var exit_code = OS.execute(CLI_PATH, cli_args, output_clear)
	
	if exit_code == 0:
		print("[DEBUG] Backend limpió el canvas exitosamente.")
		var backend_raw_output = "\n".join(output_clear)
		var json_parse_result = JSON.parse_string(backend_raw_output)
		if json_parse_result is Dictionary and json_parse_result.has("image"):
			_current_canvas_base64 = json_parse_result.image
			var image_bytes = Marshalls.base64_to_raw(_current_canvas_base64)
			var image = Image.new()
			var error = image.load_png_from_buffer(image_bytes)
			if error == OK:
				var texture = ImageTexture.create_from_image(image)
				# --- APLICAR FILTRO NEAREST AL ImageTexture del canvas (al limpiar) ---
				#texture.set_filter(CanvasItem.TEXTURE_FILTER_NEAREST)
				texture_rect.texture = texture
				print("[DEBUG] Canvas de frontend actualizado con la imagen transparente del backend.")
				if json_parse_result.has("cursorX") and json_parse_result.has("cursorY"):
					_current_cursor_x = int(json_parse_result.cursorX)
					_current_cursor_y = int(json_parse_result.cursorY)
				else:
					_current_cursor_x = 0
					_current_cursor_y = 0

			else:
				error_log.text = "Error al cargar la imagen transparente del backend: %d" % error
				print("[DEBUG] Error al cargar la imagen transparente del backend:", error)
		else:
			error_log.text = "Error: Salida JSON inesperada del backend al limpiar (no 'image'). Salida: %s" % backend_raw_output
	else:
		error_log.text = "Error al limpiar el canvas del backend (código %d):\n%s" % [exit_code, "\n".join(output_clear)]
		print("[DEBUG] Error al limpiar el canvas del backend:", exit_code, output_clear)
	
	print("[DEBUG] BackEnd Limpio")
	_update_cursor_sprite_visibility_and_position()

func _on_clear_button_pressed():
	print("[DEBUG] Botón 'Clear' presionado.")
	_clear_canvas_frontend()
	_clear_canvas_backend()
	clear_errors()

func _on_load_button_pressed():
	print("[DEBUG] Botón 'Cargar' presionado. Abriendo diálogo de carga...")
	load_file_dialog.popup_centered()

func _on_save_button_pressed():
	print("[DEBUG] Botón 'Guardar' presionado. Abriendo diálogo de guardar...")
	save_file_dialog.current_file = "mi_dibujo.gw"
	save_file_dialog.popup_centered()

func _on_load_file_selected(path: String):
	print("[DEBUG] Archivo seleccionado para cargar:", path)
	var file_extension = path.get_extension().to_lower()

	if file_extension == "gw":
		_load_gw_file(path)
		_clear_canvas_frontend()
		_clear_canvas_backend()
	elif file_extension in ["png", "jpg", "jpeg", "bmp"]:
		await _process_image_to_pixel_art(path)
	else:
		error_log.text = "Error: Tipo de archivo no soportado para cargar: %s" % file_extension
		print("[DEBUG] Tipo de archivo no soportado:", file_extension)

func _load_gw_file(path: String):
	if FileAccess.file_exists(path):
		var file = FileAccess.open(path, FileAccess.READ)
		if file:
			editor.text = file.get_as_text()
			file.close()
			print("[DEBUG] Contenido del archivo GW cargado en el editor.")
			_on_code_changed()
		else:
			error_log.text = "Error: No se pudo abrir el archivo GW para lectura: %s" % path
	else:
		error_log.text = "Error: El archivo GW seleccionado no existe: %s" % path

func _set_ui_interaction_enabled(enabled: bool):
	run_button.disabled = not enabled
	load_button.disabled = not enabled
	save_button.disabled = not enabled
	clear_button.disabled = not enabled
	width_input.editable = enabled
	height_input.editable = enabled
	resize_button.disabled = not enabled
	editor.editable = enabled

func _process_image_to_pixel_art(image_path: String):
	print("[DEBUG] Procesando imagen a Pixel Art:", image_path)
	
	_set_ui_interaction_enabled(false)
	image_process_progress_bar.show()
	image_process_label.show()
	image_process_progress_bar.value = 0
	image_process_label.text = "Cargando imagen..."
	await get_tree().process_frame

	var image = Image.new()
	var error = image.load(image_path)

	if error != OK:
		error_log.text = "Error al cargar la imagen: %s" % error
		print("[DEBUG] Error al cargar imagen:", error)
		_set_ui_interaction_enabled(true)
		image_process_progress_bar.hide()
		image_process_label.hide()
		return

	var pixel_canvas_width = int(width_input.text) if width_input.text.is_valid_int() and int(width_input.text) > 0 else 40
	var pixel_canvas_height = int(height_input.text) if height_input.text.is_valid_int() and int(height_input.text) > 0 else 40

	if pixel_canvas_width == 0 or pixel_canvas_height == 0:
		error_log.text = "El ancho o alto del canvas no puede ser cero."
		print("[DEBUG] Canvas size invalid.")
		_set_ui_interaction_enabled(true)
		image_process_progress_bar.hide()
		image_process_label.hide()
		return
	
	width_input.text = str(pixel_canvas_width)
	height_input.text = str(pixel_canvas_height)

	image_process_label.text = "Redimensionando imagen..."
	image_process_progress_bar.value = 25
	await get_tree().process_frame
	image.resize(pixel_canvas_width, pixel_canvas_height, Image.INTERPOLATE_NEAREST)
	print("[DEBUG] Imagen redimensionada a:", image.get_width(), "x", image.get_height())
	
	var generated_code_lines = []
	generated_code_lines.append("Spawn(0,0)\n")
	generated_code_lines.append("Size(1)\n")
	
	var prev_color_hex = ""
	var total_pixels = image.get_width() * image.get_height()
	var processed_pixels = 0
	var update_interval = max(1, total_pixels / 100)
	
	for y in range(image.get_height()):
		for x in range(image.get_width()):
			var color = image.get_pixel(x, y)
			
			var hex_color = "#%02x%02x%02x%02x" % [int(color.r * 255), int(color.g * 255), int(color.b * 255), int(color.a * 255)]
			
			if hex_color != prev_color_hex or (x == 0 and y == 0):
				generated_code_lines.append("Color(\"%s\")\n" % hex_color)
				prev_color_hex = hex_color

			generated_code_lines.append("SetCursor(%d, %d)\n" % [x, y])
			generated_code_lines.append("DrawRectangle(0, 0, 0, 1, 1)\n")
			
			processed_pixels += 1
			if processed_pixels % update_interval == 0:
				var progress = 25 + floor(float(processed_pixels) / total_pixels * 75)
				image_process_progress_bar.value = progress
				image_process_label.text = "Generando código..."
				await get_tree().process_frame

	editor.text = "".join(generated_code_lines)
	print("[DEBUG] Código PixelWalle generado y cargado en el editor.")
	
	image_process_progress_bar.value = 100
	image_process_label.text = "Completado."
	await get_tree().create_timer(0.5).timeout

	_clear_canvas_frontend()
	_clear_canvas_backend()

	_check_for_errors()

	_set_ui_interaction_enabled(true)
	image_process_progress_bar.hide()
	image_process_label.hide()
	image_process_progress_bar.value = 0

func _on_save_file_selected(path: String):
	print("[DEBUG] Ruta seleccionada para guardar:", path)
	var file = FileAccess.open(path, FileAccess.WRITE)
	if file:
		file.store_string(editor.text)
		file.close()
		print("[DEBUG] Contenido del editor guardado en el archivo.")
	else:
		error_log.text = "Error: No se pudo guardar el archivo: %s" % path

func _on_resize_button_pressed():
	print("[DEBUG] Botón 'Redimensionar' presionado.")
	_clear_canvas_frontend()
	_clear_canvas_backend()
	_run_backend()

func _update_cursor_sprite_visibility_and_position():
	if cursor_sprite:
		if texture_rect.texture == null or _current_cursor_x == -1 or _current_cursor_y == -1:
			cursor_sprite.hide()
			return

		cursor_sprite.show()

		var texture_size = texture_rect.texture.get_size()
		var rect_size = texture_rect.get_rect().size

		if texture_size.x == 0 or texture_size.y == 0:
			cursor_sprite.hide()
			return

		var aspect_ratio_texture = texture_size.x / texture_size.y
		var aspect_ratio_rect = rect_size.x / rect_size.y

		var actual_image_width: float
		var actual_image_height: float
		var offset_x: float = 0.0
		var offset_y: float = 0.0

		if aspect_ratio_rect > aspect_ratio_texture:
			actual_image_height = rect_size.y
			actual_image_width = actual_image_height * aspect_ratio_texture
			offset_x = (rect_size.x - actual_image_width) / 2.0
		else:
			actual_image_width = rect_size.x
			actual_image_height = actual_image_width / aspect_ratio_texture
			offset_y = (rect_size.y - actual_image_height) / 2.0

		var pixel_scale_x = actual_image_width / texture_size.x
		var pixel_scale_y = actual_image_height / texture_size.y
		
		var sprite_base_size = cursor_sprite.texture.get_size()
		var target_sprite_scale_x = pixel_scale_x / sprite_base_size.x
		var target_sprite_scale_y = pixel_scale_y / sprite_base_size.y

		cursor_sprite.scale = Vector2(target_sprite_scale_x, target_sprite_scale_y)
		
		var sprite_scaled_width = cursor_sprite.texture.get_width() * cursor_sprite.scale.x
		var sprite_scaled_height = cursor_sprite.texture.get_height() * cursor_sprite.scale.y
		
		var pixel_center_x = offset_x + (_current_cursor_x * pixel_scale_x) + (pixel_scale_x / 2.0)
		var pixel_center_y = offset_y + (_current_cursor_y * pixel_scale_y) + (pixel_scale_y / 2.0)

		cursor_sprite.position = Vector2(pixel_center_x, pixel_center_y)
