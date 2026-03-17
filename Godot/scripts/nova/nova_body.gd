extends Node3D
class_name NovaBody

## Controls Nova's physical presence — head tracking, breathing, body language.

enum BodyState { IDLE, LISTENING, SPEAKING, WORKING, STARTLED }

@export var head_track_speed: float = 3.0
@export var max_head_angle: float = 60.0
@export var breath_depth: float = 0.008
@export var breath_rate: float = 0.3

@onready var head_bone: BoneAttachment3D = $Armature/Skeleton3D/HeadBone
@onready var chest_bone: BoneAttachment3D = $Armature/Skeleton3D/ChestBone
@onready var animator: AnimationPlayer = $AnimationPlayer

var _player_head: Node3D
var _awareness_level: float = 0.0
var _breath_time: float = 0.0

func _ready() -> void:
	await get_tree().process_frame
	_find_player_head()

func _process(delta: float) -> void:
	_handle_head_tracking(delta)
	_handle_breathing(delta)

func set_awareness_level(level: float) -> void:
	_awareness_level = lerp(_awareness_level, level, 0.05)

func set_body_state(state: BodyState) -> void:
	match state:
		BodyState.IDLE:
			if animator.has_animation("idle"):
				animator.play("idle")
		BodyState.LISTENING:
			if animator.has_animation("listening"):
				animator.play("listening")
		BodyState.SPEAKING:
			if animator.has_animation("speaking"):
				animator.play("speaking")
		BodyState.WORKING:
			if animator.has_animation("working"):
				animator.play("working")
		BodyState.STARTLED:
			if animator.has_animation("startled"):
				animator.play("startled")
				await get_tree().create_timer(1.2).timeout
				if animator.has_animation("idle"):
					animator.play("idle")

func _find_player_head() -> void:
	var player = get_tree().get_first_node_in_group("player")
	if player:
		_player_head = player.get_node_or_null("XRCamera3D")
		if not _player_head:
			_player_head = player

func _handle_head_tracking(delta: float) -> void:
	if not _player_head or not head_bone:
		return
	
	var target_dir = _player_head.global_position - head_bone.global_position
	if target_dir.length() < 0.1:
		return
	
	var target_basis = Basis.looking_at(target_dir, Vector3.UP)
	var angle = head_bone.basis.get_euler().angle_to(target_basis.get_euler())
	
	if angle <= deg_to_rad(max_head_angle) * _awareness_level:
		head_bone.basis = head_bone.basis.slerp(target_basis, head_track_speed * delta)

func _handle_breathing(delta: float) -> void:
	if not chest_bone:
		return
	_breath_time += delta * breath_rate * TAU
	var breath_offset = sin(_breath_time) * breath_depth
	chest_bone.position.z = breath_offset
