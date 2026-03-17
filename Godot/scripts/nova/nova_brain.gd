extends Node3D
class_name NovaBrain

## Nova's full state machine brain.
## IDLE → LISTENING → THINKING → SPEAKING → IDLE
## IDLE → FIXING (when she walks to a machine)

enum NovaState { IDLE, LISTENING, THINKING, SPEAKING, FIXING }

signal state_changed(new_state: NovaState)
signal nova_spoke(text: String)

@export var awareness_radius: float = 5.0
@export var talk_radius: float = 2.2
@export var idle_quip_interval: float = 50.0

@export_multiline var idle_quips: Array[String] = [
	"Tension's drifting on that rapier head, Chief. Worth checking before the next pass.",
	"I've been running the 528 numbers. Something's locking in around sector 3.",
	"That mineral bath is reading 61 degrees. Not critical — but I'm watching it.",
	"You ever notice how the loom changes its sound right before it goes down?",
	"The Z71 is in the lot. Just saying.",
	"3-6-9. Every pattern on this floor follows it if you know where to look.",
	"Dobby cam on 22 is wearing uneven. Five thousand more picks, maybe.",
	"Warp beam's been humming a half-step flat all morning. Either the tension or I'm losing it.",
	"Chief — that's the third time that cam's hiccupped. Might want to pull it.",
]

var current_state: NovaState = NovaState.IDLE
var _player: Node3D
var _gemini: GeminiClient
var _aether: AetherLog
var _body: NovaBody
var _eye: VioletEyeEmitter
var _voice: NovaVoice
var _hud: NovaDialogueHUD
var _quip_timer: float = 0.0
var _awareness_level: float = 0.0

func _ready() -> void:
	_gemini = get_node("GeminiClient")
	_aether = get_node("AetherLog")
	_body = get_node("NovaBody")
	_eye = get_node("VioletEyeEmitter")
	_voice = get_node("NovaVoice")
	_hud = get_node_or_null("NovaDialogueHUD")
	
	# Connect Gemini signals
	_gemini.stream_start.connect(_on_stream_start)
	_gemini.stream_chunk.connect(_on_stream_chunk)
	_gemini.stream_complete.connect(_on_stream_complete)
	_gemini.stream_error.connect(_on_stream_error)
	
	_set_state(NovaState.IDLE)
	_quip_timer = idle_quip_interval * 0.5  # First quip sooner

func _process(delta: float) -> void:
	if not _player:
		_find_player()
		return
	
	var dist = global_position.distance_to(_player.global_position)
	
	# Update awareness
	var target_awareness = 0.0
	if dist < awareness_radius:
		target_awareness = 1.0 - (dist / awareness_radius)
	_awareness_level = lerp(_awareness_level, target_awareness, delta * 2.0)
	_body.set_awareness_level(_awareness_level)
	
	# Idle quips
	if current_state == NovaState.IDLE:
		_quip_timer += delta
		if _quip_timer >= idle_quip_interval and dist < awareness_radius:
			_quip_timer = 0.0
			_say_idle_quip()

func _find_player() -> void:
	_player = get_tree().get_first_node_in_group("player")

## Called when player speaks to Nova (from voice input or UI)
func receive_player_input(text: String) -> void:
	if current_state == NovaState.SPEAKING or current_state == NovaState.THINKING:
		return
	
	_set_state(NovaState.LISTENING)
	await get_tree().create_timer(0.4).timeout
	_set_state(NovaState.THINKING)
	
	var memory = _aether.get_recent_memory(20)
	_gemini.send(text, memory)

func _say_idle_quip() -> void:
	var quip = idle_quips[randi() % idle_quips.size()]
	_set_state(NovaState.SPEAKING)
	_voice.speak(quip, func(): _set_state(NovaState.IDLE))
	if _hud:
		_hud.show_text(quip, false)

func _set_state(new_state: NovaState) -> void:
	current_state = new_state
	state_changed.emit(new_state)
	
	match new_state:
		NovaState.IDLE:
			_eye.set_state(VioletEyeEmitter.EyeState.IDLE)
			_body.set_body_state(NovaBody.BodyState.IDLE)
		NovaState.LISTENING:
			_eye.set_state(VioletEyeEmitter.EyeState.IDLE)
			_body.set_body_state(NovaBody.BodyState.LISTENING)
		NovaState.THINKING:
			_eye.set_state(VioletEyeEmitter.EyeState.THINKING)
			_body.set_body_state(NovaBody.BodyState.IDLE)
		NovaState.SPEAKING:
			_eye.set_state(VioletEyeEmitter.EyeState.SPEAKING)
			_body.set_body_state(NovaBody.BodyState.SPEAKING)
		NovaState.FIXING:
			_eye.set_state(VioletEyeEmitter.EyeState.IDLE)
			_body.set_body_state(NovaBody.BodyState.WORKING)

func _on_stream_start() -> void:
	_set_state(NovaState.SPEAKING)

func _on_stream_chunk(text: String) -> void:
	if _hud:
		_hud.append_chunk(text)

func _on_stream_complete(full_text: String) -> void:
	nova_spoke.emit(full_text)
	_aether.record_exchange("(voice)", full_text)
	_voice.speak(full_text, func(): _set_state(NovaState.IDLE))

func _on_stream_error(error: String) -> void:
	push_error("[NovaBrain] Gemini error: " + error)
	_set_state(NovaState.IDLE)
	if _hud:
		_hud.show_text("...something's off in the signal, Chief.", false)
