extends Node
class_name AetherLog

## Persistent conversation memory — The Aether Log.
## Nova remembers every session with Chief. Always.

const FILENAME = "user://AetherLog.json"
const MAX_SESSIONS = 50

var _data: Dictionary = {
	"current_session": [],
	"sessions": []
}

func _ready() -> void:
	_load()
	print("[AetherLog] Loaded %d past sessions." % _data["sessions"].size())

func get_current_session() -> Array:
	return _data["current_session"]

func get_all_sessions() -> Array:
	return _data["sessions"]

func record_exchange(chief_text: String, nova_text: String) -> void:
	var exchange = {
		"timestamp": Time.get_datetime_string_from_system(),
		"chief_text": chief_text,
		"nova_text": nova_text
	}
	_data["current_session"].append(exchange)
	_save()

func close_session() -> void:
	if _data["current_session"].is_empty():
		return
	
	_data["sessions"].append({
		"date": Time.get_datetime_string_from_system(),
		"exchanges": _data["current_session"].duplicate()
	})
	
	while _data["sessions"].size() > MAX_SESSIONS:
		_data["sessions"].pop_front()
	
	_data["current_session"].clear()
	_save()

func get_recent_memory(count: int = 20) -> Array:
	var all_exchanges: Array = []
	for session in _data["sessions"]:
		all_exchanges.append_array(session["exchanges"])
	all_exchanges.append_array(_data["current_session"])
	
	var start = max(0, all_exchanges.size() - count)
	return all_exchanges.slice(start, all_exchanges.size())

func _save() -> void:
	var file = FileAccess.open(FILENAME, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(_data, "\t"))
		file.close()
	else:
		push_error("[AetherLog] Save failed.")

func _load() -> void:
	if not FileAccess.file_exists(FILENAME):
		return
	var file = FileAccess.open(FILENAME, FileAccess.READ)
	if file:
		var json = JSON.new()
		if json.parse(file.get_as_text()) == OK:
			_data = json.get_data()
		file.close()

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST or what == NOTIFICATION_EXIT_TREE:
		close_session()
