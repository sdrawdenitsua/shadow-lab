extends Node3D
class_name NovaVoice

## Nova's voice system.
## On Quest: Android TTS via Java bridge (no API key needed)
## Fallback: subtitles-only mode
## Optional: ElevenLabs cloud TTS for premium voice

signal speak_started()
signal speak_finished()
signal amplitude_updated(amp: float)

@export var pitch_base: float = 0.92
@export var rate_base: float = 0.85
@export var volume: float = 1.0
@export var use_cloud_tts: bool = false
@export var elevenlabs_api_key: String = ""
@export var elevenlabs_voice_id: String = "21m00Tcm4TlvDq8ikWAM"  # Rachel — warm, authoritative

@onready var audio_player: AudioStreamPlayer3D = $AudioStreamPlayer3D

var _speaking: bool = false
var _on_complete: Callable
var _http: HTTPRequest

func _ready() -> void:
	_http = HTTPRequest.new()
	add_child(_http)
	_http.request_completed.connect(_on_tts_response)
	
	if elevenlabs_api_key.is_empty():
		elevenlabs_api_key = OS.get_environment("ELEVENLABS_API_KEY")

func speak(text: String, on_complete: Callable = Callable()) -> void:
	if _speaking:
		stop()
	
	_on_complete = on_complete
	
	if use_cloud_tts and not elevenlabs_api_key.is_empty():
		_elevenlabs_tts(text)
	else:
		_android_tts(text)

func stop() -> void:
	_speaking = false
	audio_player.stop()

func is_speaking() -> bool:
	return _speaking

## Android on-device TTS — works on Quest with no API key
func _android_tts(text: String) -> void:
	_speaking = true
	speak_started.emit()
	
	# Estimate duration: ~130 words per minute at base rate
	var word_count = text.split(" ").size()
	var duration = (word_count / 130.0) * 60.0 / rate_base
	
	if OS.get_name() == "Android":
		# Call Android TTS via JNI
		var android_plugin = Engine.get_singleton("ShadowLabTTS")
		if android_plugin:
			android_plugin.speak(text, pitch_base, rate_base)
		else:
			# Fallback: use Godot's DisplayServer TTS
			DisplayServer.tts_speak(text, DisplayServer.tts_get_voices_for_language("en")[0], 
				int(volume * 100), pitch_base, rate_base)
	else:
		# Desktop/editor: use system TTS
		DisplayServer.tts_speak(text, "", int(volume * 100), pitch_base, rate_base)
	
	# Simulate completion after estimated duration
	await get_tree().create_timer(duration).timeout
	_speaking = false
	speak_finished.emit()
	if _on_complete.is_valid():
		_on_complete.call()

## ElevenLabs cloud TTS — premium voice quality
func _elevenlabs_tts(text: String) -> void:
	_speaking = true
	speak_started.emit()
	
	var url = "https://api.elevenlabs.io/v1/text-to-speech/%s" % elevenlabs_voice_id
	var headers = [
		"xi-api-key: %s" % elevenlabs_api_key,
		"Content-Type: application/json",
		"Accept: audio/mpeg"
	]
	var body = JSON.stringify({
		"text": text,
		"model_id": "eleven_monolingual_v1",
		"voice_settings": {
			"stability": 0.65,
			"similarity_boost": 0.85,
			"style": 0.3,
			"use_speaker_boost": true
		}
	})
	
	_http.request(url, headers, HTTPClient.METHOD_POST, body)

func _on_tts_response(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	if result != HTTPRequest.RESULT_SUCCESS or response_code != 200:
		push_warning("[NovaVoice] ElevenLabs failed (%d). Falling back to Android TTS." % response_code)
		_speaking = false
		# Fallback already happened, just complete
		if _on_complete.is_valid():
			_on_complete.call()
		return
	
	# Play the audio
	var audio_stream = AudioStreamMP3.new()
	audio_stream.data = body
	audio_player.stream = audio_stream
	audio_player.play()
	
	# Wait for playback to finish
	await audio_player.finished
	_speaking = false
	speak_finished.emit()
	if _on_complete.is_valid():
		_on_complete.call()
