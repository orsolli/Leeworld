extends MeshInstance3D

var s = 1
@export var digg: bool = false :
	set (value):
		digg = value
		s = -1 if digg else 1
	get:
		return digg

var size: float = 1.0
@export var level = 3.0 :
	set (value):
		level = value
		size = 8 / (2.0 ** level)
		scale = 1.1 * size * Vector3.ONE
	get:
		return level

@export var ray: RayCast3D

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta):
	var col = ray.get_collision_point()
	var offset = ray.get_collision_normal() / 512
	offset *= s
	col = col + offset
	var pos = Vector3(snap(col.x), snap(col.y), snap(col.z)) + Vector3.ONE * size / 2 if col != null else null
	if pos != position:
		position = pos

func _input(event):
	if event is InputEventMouseButton and event.pressed and event.button_index == 1:
		var col = ray.get_collision_point()
		if col != null:
			var block_id = position/8
			var repository = get_parent().find_child("Repository")
			repository.call_deferred(
				"MutateBlock",
				position.x,position.y,position.z,
				level,
				not digg
			)

func snap(num):
	return floor(num / size) * size
