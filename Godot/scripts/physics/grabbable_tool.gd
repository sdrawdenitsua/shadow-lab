extends RigidBody3D
class_name GrabbableTool

## Physics-based tool grab system for VR.
## Tools have weight, inertia, and satisfying haptic feedback.

signal grabbed(hand: XRController3D)
signal released(hand: XRController3D)
signal used(tool_name: String)

@export var tool_name: String = "Wrench"
@export var grab_snap_speed: float = 15.0
@export var use_haptic_strength: float = 0.4
@export var grab_haptic_strength: float = 0.6
@export var highlight_color: Color = Color(0.54, 0.0, 1.0)  # Nova's violet

var _grabbed_by: XRController3D = null
var _grab_offset: Transform3D
var _original_material: Material
var _highlight_material: StandardMaterial3D
var _is_highlighted: bool = false

@onready var mesh: MeshInstance3D = $MeshInstance3D
@onready var outline: MeshInstance3D = $OutlineMesh

func _ready() -> void:
	_setup_highlight_material()
	set_highlight(false)

func _physics_process(delta: float) -> void:
	if _grabbed_by:
		# Smooth follow with physics — feels weighty, not teleport
		var target_transform = _grabbed_by.global_transform * _grab_offset
		var current = global_transform
		
		# Position spring
		var pos_error = target_transform.origin - current.origin
		linear_velocity = pos_error * grab_snap_speed
		
		# Rotation spring
		var target_quat = target_transform.basis.get_rotation_quaternion()
		var current_quat = current.basis.get_rotation_quaternion()
		var rot_error = target_quat * current_quat.inverse()
		var rot_axis = Vector3.ZERO
		var rot_angle = 0.0
		rot_error.get_axis_angle(rot_axis, rot_angle)
		angular_velocity = rot_axis * rot_angle * grab_snap_speed

func try_grab(hand: XRController3D) -> bool:
	if _grabbed_by:
		return false
	
	_grabbed_by = hand
	_grab_offset = hand.global_transform.inverse() * global_transform
	
	freeze = false
	gravity_scale = 0.0
	
	set_highlight(false)
	_haptic(hand, grab_haptic_strength, 0.1)
	grabbed.emit(hand)
	return true

func release(hand: XRController3D) -> void:
	if _grabbed_by != hand:
		return
	
	_grabbed_by = null
	gravity_scale = 1.0
	
	# Throw velocity from hand motion
	linear_velocity = hand.get_linear_velocity() if hand.has_method("get_linear_velocity") else Vector3.ZERO
	angular_velocity = hand.get_angular_velocity() if hand.has_method("get_angular_velocity") else Vector3.ZERO
	
	released.emit(hand)

func use(hand: XRController3D) -> void:
	_haptic(hand, use_haptic_strength, 0.05)
	used.emit(tool_name)

func is_grabbed() -> bool:
	return _grabbed_by != null

func set_highlight(on: bool) -> void:
	if _is_highlighted == on or not outline:
		return
	_is_highlighted = on
	outline.visible = on

func _setup_highlight_material() -> void:
	if not outline:
		return
	_highlight_material = StandardMaterial3D.new()
	_highlight_material.albedo_color = highlight_color
	_highlight_material.emission_enabled = true
	_highlight_material.emission = highlight_color
	_highlight_material.emission_energy_multiplier = 1.5
	_highlight_material.cull_mode = BaseMaterial3D.CULL_FRONT
	_highlight_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	outline.set_surface_override_material(0, _highlight_material)

func _haptic(hand: XRController3D, strength: float, duration: float) -> void:
	if hand and hand.has_method("trigger_haptic_pulse"):
		hand.trigger_haptic_pulse("haptic", strength, duration, 0.0, 0.0)

## Called by XR hand when it enters grab range
func _on_grab_area_body_entered(body: Node3D) -> void:
	if body.is_in_group("xr_hand") and not _grabbed_by:
		set_highlight(true)

func _on_grab_area_body_exited(body: Node3D) -> void:
	if body.is_in_group("xr_hand"):
		set_highlight(false)
