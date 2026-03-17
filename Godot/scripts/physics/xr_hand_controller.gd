extends XRController3D
class_name XRHandController

## XR hand controller — manages grabbing, using, and haptics.
## Works with Quest 3 hand tracking and controller input.

@export var grab_sphere_radius: float = 0.08
@export var throw_multiplier: float = 1.4

@onready var grab_area: Area3D = $GrabArea
@onready var hand_mesh: MeshInstance3D = $HandMesh
@onready var grab_ray: RayCast3D = $GrabRay

var _grabbed_object: GrabbableTool = null
var _nearby_objects: Array[GrabbableTool] = []
var _prev_position: Vector3
var _velocity_history: Array[Vector3] = []
var _angular_velocity_history: Array[Vector3] = []

func _ready() -> void:
	add_to_group("xr_hand")
	grab_area.body_entered.connect(_on_body_entered)
	grab_area.body_exited.connect(_on_body_exited)
	_prev_position = global_position

func _physics_process(delta: float) -> void:
	_track_velocity(delta)
	_handle_input()

func get_linear_velocity() -> Vector3:
	if _velocity_history.is_empty():
		return Vector3.ZERO
	var avg = Vector3.ZERO
	for v in _velocity_history:
		avg += v
	return avg / _velocity_history.size() * throw_multiplier

func get_angular_velocity() -> Vector3:
	if _angular_velocity_history.is_empty():
		return Vector3.ZERO
	var avg = Vector3.ZERO
	for v in _angular_velocity_history:
		avg += v
	return avg / _angular_velocity_history.size()

func _track_velocity(delta: float) -> void:
	var current_vel = (global_position - _prev_position) / delta
	_velocity_history.append(current_vel)
	if _velocity_history.size() > 5:
		_velocity_history.pop_front()
	_prev_position = global_position

func _handle_input() -> void:
	# Grab / release
	if is_button_pressed("grip_click"):
		if not _grabbed_object:
			_try_grab()
	else:
		if _grabbed_object:
			_release()
	
	# Use / interact
	if is_button_pressed("trigger_click"):
		if _grabbed_object:
			_grabbed_object.use(self)

func _try_grab() -> void:
	# First check nearby objects in grab sphere
	var closest: GrabbableTool = null
	var closest_dist: float = INF
	
	for obj in _nearby_objects:
		if not obj.is_grabbed():
			var dist = global_position.distance_to(obj.global_position)
			if dist < closest_dist:
				closest_dist = dist
				closest = obj
	
	if closest:
		if closest.try_grab(self):
			_grabbed_object = closest
			return
	
	# Raycast grab for distant objects (UI / loom controls)
	if grab_ray and grab_ray.is_colliding():
		var collider = grab_ray.get_collider()
		if collider is GrabbableTool and not collider.is_grabbed():
			if collider.try_grab(self):
				_grabbed_object = collider

func _release() -> void:
	if _grabbed_object:
		_grabbed_object.release(self)
		_grabbed_object = null

func _on_body_entered(body: Node3D) -> void:
	if body is GrabbableTool:
		_nearby_objects.append(body)
		body.set_highlight(true)

func _on_body_exited(body: Node3D) -> void:
	if body is GrabbableTool:
		_nearby_objects.erase(body)
		if not body.is_grabbed():
			body.set_highlight(false)
