@icon("res://interface/icon.png")
class_name ChunkUpdater
extends Node

@export var isSubtract: bool;
var box = load("res://Terrain/box.tscn");

# The order of this array must be according to specifications.
var direction = PackedVector3Array([
	Vector3(-1,	-1,	-1),
	Vector3(1,	-1,	-1),
	Vector3(-1,	1,	-1),
	Vector3(1,	1,	-1),
	Vector3(-1,	-1,	1),
	Vector3(1,	-1,	1),
	Vector3(-1,	1,	1),
	Vector3(1,	1,	1) # This order is "increment x first"
])

func update_block(octreeString):
	
	# This class does not support level 1 mods
	if octreeString == "01":
		octreeString = "0101010101010101"
	elif octreeString == "00":
		octreeString = "0000000000000000"

	var instantiate = func (position, size, id):
		var newNode = box.instantiate() as Node3D
		newNode.scale = Vector3(size, size, size)
		newNode.position = position
		newNode.name = id
		add_child(newNode)

	render_octree(
		octreeString,
		func (path, isInside): update_voxel(path, isInside, instantiate)
	)


func render_octree(octreeString: String, render_voxel: Callable):
	var index = 0
	var paths = [[]]
	while (paths.size() > 0):
		var path = paths.pop_back()
		for i in range(8):
			var value = octreeString[index * 16 + i*2 + 1] == '1'
			render_voxel.call(path + [i], value)
			var more = octreeString[index * 16 + i*2] == '1'
			if more:
				paths.push_front(path + [i])
		index += 1
		if index * 16 >= octreeString.length():
			return


func update_voxel(path, isInside, instantiate_node: Callable):
	var scale = 0.5
	var position = Vector3(0,0,0)
	for i in path.size():
		scale/=2
		position += direction[path[i]] * scale
	for i in range(0, -path.size(), -1):
		# Remove block at above levels if exist
		var oldNode = ",".join(path.slice(0, -1+i))
		if has_node(oldNode):
			remove_child(get_node(oldNode))

	var stringPath = ",".join(path)
	var oldNodes = find_children(stringPath + ',*', "CSGShape3D", false, false)
	for oldNode in oldNodes:
		remove_child(oldNode)

	var isOperation = (isInside != isSubtract)
	if isOperation == has_node(stringPath): return
	if isOperation: instantiate_node.call(position, scale*2, stringPath)
	else: remove_child(get_node(stringPath))
