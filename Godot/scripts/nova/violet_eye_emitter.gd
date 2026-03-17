extends Node3D
class_name VioletEyeEmitter

## Controls Nova's violet eye emission — the window into her state.
## IDLE     → slow sinusoidal pulse  (soft violet, 1.5s period)
## THINKING → rapid erratic flicker  (dim violet, mimics processing)
## SPEAKING → live amplitude flicker (bright violet)
## ALERT    → sharp red pulse        (danger / lockout state)

enum EyeState { IDLE, THINKING, SPEAKING, ALERT }

@export var left_eye_mesh: MeshInstance3D
@export var right_eye_mesh: MeshInstance3D
@export var mat_index: int = 0

@export_group("State Colors")
@export var col_idle: Color = Color(0.54, 0.0, 1.0)      # #8b00ff
@export var col_thinking: Color = Color(0.28, 0.0, 0.55)  # dim violet
@export var col_speaking: Color = Color(0.75, 0.30, 1.0)  # bright violet
@export var col_alert: Color = Color(1.0, 0.12, 0.12)     # red

@export_group("Intensity Ranges")
@export var idle_range: Vector2 = Vector2(0.8, 1.6)
@export var thinking_range: Vector2 = Vector2(0.2, 1.0)
@export var speaking_range: Vector2 = Vector2(1.5, 3.2)
@export var alert_range: Vector2 = Vector2(1.8, 3.5)

@export_group("Speeds")
@export var idle_pulse_speed: float = 1.1
@export var think_flicker_min: float = 0.03
@export var think_flicker_max: float = 0.09
@export var speak_flicker_min: float = 0.03
@export var speak_flicker_max: float = 0.11
@export var alert_pulse_speed: float = 4.0

var _left_mat: StandardMaterial3D
var _right_mat: StandardMaterial3D
var _current_state: EyeState = EyeState.IDLE
var _time: float = 0.0
var _next_flicker: float = 0.0
var _speak_amplitude: float = 0.0

func _ready() -> void:
	if left_eye_mesh:
		_left_mat = left_eye_mesh.get_active_material(mat_index).duplicate()
		left_eye_mesh.set_surface_override_material(mat_index, _left_mat)
	if right_eye_mesh:
		_right_mat = right_eye_mesh.get_active_material(mat_index).duplicate()
		right_eye_mesh.set_surface_override_material(mat_index, _right_mat)

func set_state(state: EyeState) -> void:
	_current_state = state
	_time = 0.0

func set_speak_amplitude(amp: float) -> void:
	_speak_amplitude = amp

func _process(delta: float) -> void:
	_time += delta
	var emission_color: Color
	var intensity: float
	
	match _current_state:
		EyeState.IDLE:
			intensity = lerp(idle_range.x, idle_range.y, (sin(_time * idle_pulse_speed * TAU) + 1.0) * 0.5)
			emission_color = col_idle * intensity
		
		EyeState.THINKING:
			_next_flicker -= delta
			if _next_flicker <= 0.0:
				intensity = randf_range(thinking_range.x, thinking_range.y)
				_next_flicker = randf_range(think_flicker_min, think_flicker_max)
			else:
				intensity = thinking_range.x
			emission_color = col_thinking * intensity
		
		EyeState.SPEAKING:
			_next_flicker -= delta
			if _next_flicker <= 0.0:
				var amp_boost = 1.0 + _speak_amplitude * 2.0
				intensity = randf_range(speaking_range.x, speaking_range.y) * amp_boost
				_next_flicker = randf_range(speak_flicker_min, speak_flicker_max)
			else:
				intensity = speaking_range.x
			emission_color = col_speaking * intensity
		
		EyeState.ALERT:
			intensity = lerp(alert_range.x, alert_range.y, (sin(_time * alert_pulse_speed * TAU) + 1.0) * 0.5)
			emission_color = col_alert * intensity
	
	_apply_emission(emission_color)

func _apply_emission(color: Color) -> void:
	if _left_mat:
		_left_mat.emission = color
		_left_mat.emission_energy_multiplier = color.get_luminance() * 3.0
	if _right_mat:
		_right_mat.emission = color
		_right_mat.emission_energy_multiplier = color.get_luminance() * 3.0
