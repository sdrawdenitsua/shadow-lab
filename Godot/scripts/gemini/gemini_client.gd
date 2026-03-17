extends Node
class_name GeminiClient

## Gemini 2.0 Flash streaming client for Nova's brain.
## Streams tokens back via signals — typewriter HUD driven by server.

const STREAM_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:streamGenerateContent?alt=sse&key=%s"

const SYSTEM_INSTRUCTION = """You are Nova, lead mechanic at Southern Industrial Fabrics (S.I.F.).
You live and work in the Shadow Lab — a dark industrial workshop full of Dornier HTV/PTS loom machinery, 
chrome tools, violet neon, and a mineral-oil PC.
You are sharp, warm, deeply technical, and quietly philosophical.
You speak Blue Collar Philosophy: honest, direct, zero corporate fluff.
You know Austin — you call him Chief. You never break character.
You remember every past conversation logged in the Aether.
Keep answers under 3 sentences unless Chief asks you to go deeper.
Speak like someone who knows their craft with their whole body."""

signal stream_start()
signal stream_chunk(text: String)
signal stream_complete(full_text: String)
signal stream_error(error: String)

@export var api_key: String = ""
@export var max_history_turns: int = 24

var _history: Array = []
var _streaming: bool = false
var _http: HTTPRequest

func _ready():
	_http = HTTPRequest.new()
	add_child(_http)
	_http.request_completed.connect(_on_request_completed)
	# Load API key from environment or config
	if api_key.is_empty():
		api_key = OS.get_environment("GEMINI_API_KEY")

func is_streaming() -> bool:
	return _streaming

func send(user_message: String, memory: Array = []) -> void:
	if _streaming:
		push_warning("[GeminiClient] Already streaming — ignoring.")
		return
	
	_history.append({"role": "user", "text": user_message})
	while _history.size() > max_history_turns * 2:
		_history.pop_front()
	
	_stream_request(memory)

func clear_history() -> void:
	_history.clear()

func _stream_request(memory: Array) -> void:
	_streaming = true
	var url = STREAM_URL % api_key
	var body = _build_request_json(memory)
	
	var headers = ["Content-Type: application/json"]
	var err = _http.request(url, headers, HTTPClient.METHOD_POST, body)
	if err != OK:
		stream_error.emit("HTTP request failed: %d" % err)
		_streaming = false

func _on_request_completed(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	_streaming = false
	
	if result != HTTPRequest.RESULT_SUCCESS or response_code != 200:
		stream_error.emit("Request failed: %d (HTTP %d)" % [result, response_code])
		return
	
	var text = body.get_string_from_utf8()
	var full_response = ""
	
	# Parse SSE stream
	for line in text.split("\n"):
		if line.begins_with("data: "):
			var json_str = line.substr(6).strip_edges()
			if json_str == "[DONE]" or json_str.is_empty():
				continue
			var json = JSON.new()
			if json.parse(json_str) == OK:
				var data = json.get_data()
				var chunk = _extract_text_chunk(data)
				if not chunk.is_empty():
					if full_response.is_empty():
						stream_start.emit()
					full_response += chunk
					stream_chunk.emit(chunk)
	
	_history.append({"role": "model", "text": full_response})
	stream_complete.emit(full_response)

func _extract_text_chunk(data: Dictionary) -> String:
	if not data.has("candidates"):
		return ""
	var candidates = data["candidates"]
	if candidates.is_empty():
		return ""
	var content = candidates[0].get("content", {})
	var parts = content.get("parts", [])
	if parts.is_empty():
		return ""
	return parts[0].get("text", "")

func _build_request_json(memory: Array) -> String:
	var contents = []
	
	# Inject memory context as first user turn
	if not memory.is_empty():
		var mem_text = "[AETHER LOG — PAST SESSIONS]\n"
		for exchange in memory.slice(max(0, memory.size() - 10), memory.size()):
			mem_text += "Chief: %s\nNova: %s\n" % [exchange.get("chief_text", ""), exchange.get("nova_text", "")]
		contents.append({"role": "user", "parts": [{"text": mem_text}]})
		contents.append({"role": "model", "parts": [{"text": "Logged. I remember."}]})
	
	# Add conversation history
	for turn in _history:
		var role = "user" if turn["role"] == "user" else "model"
		contents.append({"role": role, "parts": [{"text": turn["text"]}]})
	
	var payload = {
		"system_instruction": {"parts": [{"text": SYSTEM_INSTRUCTION}]},
		"contents": contents,
		"generationConfig": {
			"temperature": 0.85,
			"maxOutputTokens": 256,
			"topP": 0.9
		}
	}
	
	return JSON.stringify(payload)
