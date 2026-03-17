extends Node3D
class_name MineralOilPC

## The mineral oil PC — heart of the Shadow Lab.
## Displays system stats, Nova's memory logs, and ambient visuals.
## Submerged GPU visible through oil surface.

@export var boot_sequence_duration: float = 3.5
@export var idle_screen_change_interval: float = 12.0

@onready var main_monitor: SubViewport = $MonitorViewport
@onready var oil_surface: MeshInstance3D = $OilTank/Surface
@onready var gpu_glow: OmniLight3D = $GPUGlow
@onready var fan_sound: AudioStreamPlayer3D = $FanHum
@onready var status_label: Label = $MonitorViewport/StatusLabel
@onready var memory_log: RichTextLabel = $MonitorViewport/MemoryLog
@onready var stats_panel: Panel = $MonitorViewport/StatsPanel

var _boot_complete: bool = false
var _screen_timer: float = 0.0
var _aether: AetherLog

const BOOT_LINES = [
	"> SHADOW LAB v4.0 — BOOT SEQUENCE",
	"> Initializing Nova Core... [OK]",
	"> Loading Aether Log... [OK]",
	"> Loom telemetry link... [OK]",
	"> Gemini API handshake... [OK]",
	"> 528Hz resonance lock... [OK]",
	"> All systems nominal. 3-6-9.",
	"> Welcome back, Chief.",
]

func _ready() -> void:
	_aether = get_tree().get_first_node_in_group("aether_log")
	_run_boot_sequence()
	
	# GPU glow — subtle violet pulse
	if gpu_glow:
		gpu_glow.light_color = Color(0.4, 0.0, 0.8)
		gpu_glow.light_energy = 1.2

func _process(delta: float) -> void:
	if not _boot_complete:
		return
	
	_screen_timer += delta
	if _screen_timer >= idle_screen_change_interval:
		_screen_timer = 0.0
		_cycle_display()
	
	# GPU glow pulse
	if gpu_glow:
		gpu_glow.light_energy = 1.2 + sin(Time.get_ticks_msec() * 0.001 * 1.8) * 0.3

func _run_boot_sequence() -> void:
	if not status_label:
		_boot_complete = true
		return
	
	var delay = 0.0
	for line in BOOT_LINES:
		await get_tree().create_timer(delay).timeout
		status_label.text += line + "\n"
		delay += boot_sequence_duration / BOOT_LINES.size()
	
	await get_tree().create_timer(0.5).timeout
	_boot_complete = true
	_show_main_display()

func _show_main_display() -> void:
	if status_label:
		status_label.visible = false
	if memory_log:
		memory_log.visible = true
		_update_memory_display()

func _update_memory_display() -> void:
	if not memory_log or not _aether:
		return
	
	var recent = _aether.get_recent_memory(5)
	memory_log.clear()
	memory_log.add_text("[AETHER LOG — RECENT]\n\n")
	
	for exchange in recent:
		memory_log.add_text("[color=#8b00ff]Nova:[/color] %s\n\n" % exchange.get("nova_text", ""))

func _cycle_display() -> void:
	_update_memory_display()
