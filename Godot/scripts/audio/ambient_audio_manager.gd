extends Node
class_name AmbientAudioManager

## Shadow Lab ambient audio system.
## 528Hz binaural tone + loom rhythm + spatial industrial sounds.

@export var master_volume: float = 0.8
@export var loom_rhythm_bpm: float = 72.0

@onready var ambient_528: AudioStreamPlayer = $Ambient528Hz
@onready var loom_rhythm: AudioStreamPlayer = $LoomRhythm
@onready var industrial_layer: AudioStreamPlayer = $IndustrialLayer

# Spatial audio sources in the scene
var _loom_sources: Array[AudioStreamPlayer3D] = []
var _is_active: bool = false
var _beat_timer: float = 0.0
var _beat_interval: float = 60.0 / 72.0

func _ready() -> void:
	start()

func start() -> void:
	_is_active = true
	if ambient_528:
		ambient_528.volume_db = linear_to_db(master_volume * 0.3)
		ambient_528.play()
	if industrial_layer:
		industrial_layer.volume_db = linear_to_db(master_volume * 0.5)
		industrial_layer.play()

func stop() -> void:
	_is_active = false
	ambient_528.stop()
	loom_rhythm.stop()
	industrial_layer.stop()

func register_loom(source: AudioStreamPlayer3D) -> void:
	_loom_sources.append(source)

func set_nova_speaking(speaking: bool) -> void:
	# Duck ambient when Nova speaks
	var target_volume = 0.15 if speaking else master_volume * 0.3
	var tween = create_tween()
	if ambient_528:
		tween.tween_property(ambient_528, "volume_db", linear_to_db(target_volume), 0.5)

func _process(delta: float) -> void:
	if not _is_active:
		return
	
	# Loom heartbeat rhythm
	_beat_timer += delta
	if _beat_timer >= _beat_interval:
		_beat_timer -= _beat_interval
		_pulse_loom_rhythm()

func _pulse_loom_rhythm() -> void:
	if loom_rhythm and not loom_rhythm.playing:
		loom_rhythm.volume_db = linear_to_db(master_volume * 0.4)
		loom_rhythm.play()
