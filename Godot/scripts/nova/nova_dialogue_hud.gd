extends Node3D
class_name NovaDialogueHUD

## Holographic dialogue HUD — floats in VR space near Nova.
## Typewriter effect driven by streaming tokens.

@export var display_duration: float = 6.0
@export var fade_speed: float = 2.0
@export var max_chars: int = 280

@onready var label: Label3D = $DialogueLabel
@onready var bg_mesh: MeshInstance3D = $BackgroundMesh

var _full_text: String = ""
var _display_timer: float = 0.0
var _fading: bool = false
var _visible_alpha: float = 0.0
var _target_alpha: float = 0.0

func _ready() -> void:
	_set_alpha(0.0)

func _process(delta: float) -> void:
	# Fade in/out
	_visible_alpha = move_toward(_visible_alpha, _target_alpha, fade_speed * delta)
	_apply_alpha(_visible_alpha)
	
	# Auto-hide timer
	if _target_alpha > 0.0 and not _fading:
		_display_timer -= delta
		if _display_timer <= 0.0:
			hide_text()

func show_text(text: String, is_streaming: bool = false) -> void:
	_full_text = text
	label.text = text
	_display_timer = display_duration
	_target_alpha = 1.0
	_fading = false

func append_chunk(chunk: String) -> void:
	_full_text += chunk
	if _full_text.length() > max_chars:
		_full_text = _full_text.right(max_chars)
	label.text = _full_text
	_display_timer = display_duration
	_target_alpha = 1.0
	_fading = false

func hide_text() -> void:
	_fading = true
	_target_alpha = 0.0

func clear() -> void:
	_full_text = ""
	label.text = ""
	hide_text()

func _set_alpha(alpha: float) -> void:
	label.modulate.a = alpha
	if bg_mesh and bg_mesh.get_active_material(0):
		var mat = bg_mesh.get_active_material(0)
		mat.albedo_color.a = alpha * 0.7

func _apply_alpha(alpha: float) -> void:
	_set_alpha(alpha)
