extends CharacterBody3D


@export var mouseSensitivity = 10
const SPEED = 5.0
const JUMP_VELOCITY = 4.5
var _mouse_motion = Vector2()

# Get the gravity from the project settings to be synced with RigidBody nodes.
var gravity = ProjectSettings.get_setting("physics/3d/default_gravity")


@onready var head = $Head
@onready var CLAMP_Y = PI * 1000/mouseSensitivity
@onready var DELTA_X = 0.5 * mouseSensitivity/1000

func _ready():
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)


func _input(event):
	if event is InputEventMouseMotion:
		if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
			_mouse_motion += event.relative
	if event is InputEventKey and not event.is_pressed():
		if event.keycode == KEY_ESCAPE:
			if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
				Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
			else:
				Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	


func _process(_delta):
	# Mouse movement.
	_mouse_motion.y = clamp(_mouse_motion.y, -CLAMP_Y, CLAMP_Y)
	transform.basis = Basis(Vector3.DOWN, _mouse_motion.x * DELTA_X)
	head.transform.basis = Basis(Vector3.LEFT, _mouse_motion.y * DELTA_X)


func _physics_process(delta):
	# Add the gravity.
	if not is_on_floor():
		velocity.y -= gravity * delta

	# Handle Jump.
	if Input.is_action_just_pressed("move_jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var input_dir = Input.get_vector("move_left", "move_right", "move_forward", "move_backwards")
	var direction = (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

	move_and_slide()
