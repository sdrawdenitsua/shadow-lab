extends Node3D
class_name ShadowLabManager

## Master environment controller for the Shadow Lab.
## Manages lighting, atmosphere, loom states, and mineral oil PC.

@export var violet_neon_intensity: float = 2.5
@export var ambient_light_energy: float = 0.12
@export var oil_bath_temperature: float = 61.0
@export var oil_temperature_warning: float = 68.0

@onready var nova_brain: NovaBrain = $Nova/NovaBrain
@onready var ambient_audio: AmbientAudioManager = $AmbientAudioManager
@onready var violet_lights: Array = []
@onready var oil_pc_monitor: Node3D = $MineralOilPC/Monitor
@onready var oil_temp_label: Label3D = $MineralOilPC/TempDisplay

# Loom registry
var looms: Array[LoomTensionMechanism] = []
var _temp_drift_timer: float = 0.0

func _ready() -> void:
	_setup_looms()
	_setup_nova_connections()
	_setup_lighting()
	print("[ShadowLab] Lab initialized. 3-6-9.")

func _process(delta: float) -> void:
	_update_oil_temp(delta)
	_update_oil_display()

func _setup_looms() -> void:
	for child in get_children():
		if child is LoomTensionMechanism:
			looms.append(child)
			child.tension_critical.connect(_on_loom_critical.bind(child.loom_id))
			child.tension_normalized.connect(_on_loom_normalized.bind(child.loom_id))

func _setup_nova_connections() -> void:
	if nova_brain:
		nova_brain.state_changed.connect(_on_nova_state_changed)

func _setup_lighting() -> void:
	# Find all violet neon lights in scene
	for light in get_tree().get_nodes_in_group("violet_neon"):
		if light is OmniLight3D or light is SpotLight3D:
			violet_lights.append(light)
			light.light_energy = violet_neon_intensity

func _update_oil_temp(delta: float) -> void:
	# Oil temperature slowly fluctuates
	_temp_drift_timer += delta
	oil_bath_temperature += sin(_temp_drift_timer * 0.1) * 0.02

func _update_oil_display() -> void:
	if oil_temp_label:
		var color = Color.GREEN
		if oil_bath_temperature > oil_temperature_warning:
			color = Color(1.0, 0.4, 0.0)
		oil_temp_label.text = "%.1f°C" % oil_bath_temperature
		oil_temp_label.modulate = color

func _on_loom_critical(loom_id: int) -> void:
	print("[ShadowLab] LOOM %d — CRITICAL TENSION" % loom_id)
	if nova_brain:
		var messages = [
			"Chief — loom %d is reading critical tension. You want to get eyes on that." % loom_id,
			"Tension spike on %d. She's about to throw a fit." % loom_id,
			"Loom %d's complaining. Tension's way off nominal." % loom_id,
		]
		nova_brain.receive_player_input("[SYSTEM: Loom %d just hit critical tension. Nova should alert Chief.]" % loom_id)

func _on_loom_normalized(loom_id: int) -> void:
	print("[ShadowLab] Loom %d tension normalized." % loom_id)

func _on_nova_state_changed(state: NovaBrain.NovaState) -> void:
	var speaking = state == NovaBrain.NovaState.SPEAKING
	ambient_audio.set_nova_speaking(speaking)
	
	# Pulse violet neon when Nova speaks
	if speaking:
		_pulse_neon()

func _pulse_neon() -> void:
	var tween = create_tween()
	for light in violet_lights:
		tween.tween_property(light, "light_energy", violet_neon_intensity * 1.5, 0.1)
	tween.tween_interval(0.1)
	for light in violet_lights:
		tween.tween_property(light, "light_energy", violet_neon_intensity, 0.3)
