extends Node3D
class_name LoomTensionMechanism

## Dornier loom tension mechanism — interactive physics object.
## Player can grab the tension knob and adjust warp beam pressure.
## Nova comments on tension state. Critical tension triggers alert.

signal tension_changed(value: float)
signal tension_critical(value: float)
signal tension_normalized()

@export var loom_id: int = 1
@export var min_tension: float = 0.0
@export var max_tension: float = 100.0
@export var nominal_tension: float = 55.0
@export var critical_low: float = 30.0
@export var critical_high: float = 80.0
@export var tension_drift_speed: float = 0.2  # How fast it drifts over time

@onready var tension_knob: GrabbableTool = $TensionKnob
@onready var tension_gauge: MeshInstance3D = $TensionGauge
@onready var gauge_needle: Node3D = $TensionGauge/Needle
@onready var warning_light: OmniLight3D = $WarningLight
@onready var loom_sound: AudioStreamPlayer3D = $LoomAudio

var tension: float = 55.0
var _drift_timer: float = 0.0
var _drift_direction: float = 1.0
var _was_critical: bool = false

func _ready() -> void:
	tension = nominal_tension
	if tension_knob:
		tension_knob.grabbed.connect(_on_knob_grabbed)
		tension_knob.released.connect(_on_knob_released)
	_update_visuals()

func _process(delta: float) -> void:
	_handle_drift(delta)
	_check_critical_state()
	_update_visuals()

func adjust_tension(delta_amount: float) -> void:
	tension = clamp(tension + delta_amount, min_tension, max_tension)
	tension_changed.emit(tension)

func get_tension_normalized() -> float:
	return (tension - min_tension) / (max_tension - min_tension)

func is_nominal() -> bool:
	return tension >= nominal_tension - 10.0 and tension <= nominal_tension + 10.0

func _handle_drift(delta: float) -> void:
	# Looms naturally drift — tension creeps up or down
	_drift_timer -= delta
	if _drift_timer <= 0.0:
		_drift_direction = 1.0 if randf() > 0.5 else -1.0
		_drift_timer = randf_range(8.0, 20.0)
	
	tension += _drift_direction * tension_drift_speed * delta
	tension = clamp(tension, min_tension, max_tension)

func _check_critical_state() -> void:
	var is_critical = tension < critical_low or tension > critical_high
	
	if is_critical and not _was_critical:
		_was_critical = true
		tension_critical.emit(tension)
	elif not is_critical and _was_critical:
		_was_critical = false
		tension_normalized.emit()

func _update_visuals() -> void:
	# Rotate gauge needle
	if gauge_needle:
		var normalized = get_tension_normalized()
		gauge_needle.rotation_degrees.z = lerp(-90.0, 90.0, normalized)
	
	# Warning light — red when critical
	if warning_light:
		var is_critical = tension < critical_low or tension > critical_high
		warning_light.visible = is_critical
		warning_light.light_color = Color(1.0, 0.1, 0.1) if tension > critical_high else Color(1.0, 0.5, 0.0)
	
	# Loom audio pitch changes with tension
	if loom_sound and loom_sound.playing:
		var pitch_shift = lerp(0.85, 1.15, get_tension_normalized())
		loom_sound.pitch_scale = pitch_shift

func _on_knob_grabbed(_hand: XRController3D) -> void:
	pass  # Visual feedback handled by GrabbableTool

func _on_knob_released(_hand: XRController3D) -> void:
	pass
