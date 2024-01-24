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
	var instantiate = func (position, size, id):
		var newNode = box.instantiate() as Node3D
		newNode.scale = Vector3(size, size, size)
		newNode.position = position
		newNode.name = id
		add_child(newNode)

	ChunkUpdater.render_octree(
		octreeString,
		func (path, isInside): update_voxel(path, isInside, instantiate)
	)


static func render_octree(octreeString: String, render_voxel: Callable):
	var path = [0]
	while octreeString.length() > 0:
		path[-1] += 1 # Current node is evaluated.
		while path[-1] > 8:
			path.resize(path.size() - 1)
			# What if it is empty? !!!
			if path.size() == 0:
				print("Octree string is invalid.")
				print(octreeString)
				print(path)
				return
			path[-1] += 1 # Current node is evaluated.
		var isLeaf = octreeString[0] == '0'
		if (isLeaf):
			if octreeString[1] == '1' or octreeString[1] == '0':
				render_voxel.call(path, octreeString[1] == '1')
			octreeString = octreeString.substr(2)

		if not isLeaf:
			path.append(0)
			octreeString = octreeString.substr(1)


func update_voxel(path, isInside, instantiate_node: Callable):
	var scale = 1.0
	var position = Vector3(scale,scale,scale) / 2
	var hasSubsections = path.size() > 1
	if hasSubsections:
		for i in path.size():
			scale/=2
			position += direction[path[i]-1] * scale
		# Remove block at one level above if exist
		var oldNode = ",".join(path.slice(0, -1))
		if has_node(oldNode):
			remove_child(get_node(oldNode))

	var stringPath = ",".join(path)
	var oldNodes = find_children(stringPath + ',*', "CSGShape3D", false, false)
	for oldNode in oldNodes:
		remove_child(oldNode)

	var isOperation = (isInside != isSubtract) and hasSubsections
	if isOperation == has_node(stringPath): return
	if isOperation: instantiate_node.call(position, scale*2, stringPath)
	else: remove_child(get_node(stringPath))
