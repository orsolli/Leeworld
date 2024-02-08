extends Node

class_name ChunkSpawner

var _chunks = {}
var _super_chunks = {}
var _cache = {}
var _ground: PackedScene = preload("res://Terrain/ground_chunk.tscn");
var _air: PackedScene = preload("res://Terrain/air_chunk.tscn");
var _chunk: PackedScene = preload("res://Terrain/chunk.tscn");
@export
var observer: NodePath
@export
var repository: Node
var distance: int
@export
var maxDistance: int = 3
var previousPos: Vector3

# Called when the node enters the scene tree for the first time.
func _ready():
	pass

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	var obs = get_node(observer)
	var o = obs.position;
	var x: int = int(o.x / 8.0)
	var y: int = int(o.y / 8.0)
	var z: int = int(o.z / 8.0)
	if previousPos.x != x || previousPos.y != y || previousPos.z != z:
		distance = 1
	else:
		distance = (distance + 1) % maxDistance + 1

	SpawnBlock(x, y, z)
	for d in range(1,distance+1):
		SpawnBlock(x, y - d, z);
		SpawnBlock(x + d, y, z);
		SpawnBlock(x - d, y, z);

		SpawnBlock(x, y, z + d);
		SpawnBlock(x, y, z - d);

		SpawnBlock(x, y + d, z);

	for d1 in range(1, distance+1):
		for d2 in range(1, distance+1):
			SpawnBlock(x + d1, y - d2, z);
			SpawnBlock(x - d1, y - d2, z);

			SpawnBlock(x, y - d1, z + d2);
			SpawnBlock(x, y - d1, z - d2);

			SpawnBlock(x + d1, y, z + d2);
			SpawnBlock(x + d1, y, z - d2);
			SpawnBlock(x - d1, y, z + d2);
			SpawnBlock(x - d1, y, z - d2);

			SpawnBlock(x + d1, y + d2, z);
			SpawnBlock(x - d1, y + d2, z);

			SpawnBlock(x, y + d1, z + d2);
			SpawnBlock(x, y + d1, z - d2);

	for d1 in range(distance, 0, -1):
		for d2 in range(distance, 0, -1):
			for d3 in range(distance, 0, -1):
				SpawnBlock(x + d1, y - d2, z + d3);
				SpawnBlock(x + d1, y - d2, z - d3);
				SpawnBlock(x - d1, y - d2, z + d3);
				SpawnBlock(x - d1, y - d2, z - d3);
				SpawnBlock(x + d1, y + d2, z + d3);
				SpawnBlock(x + d1, y + d2, z - d3);
				SpawnBlock(x - d1, y + d2, z + d3);
				SpawnBlock(x - d1, y + d2, z - d3);


func SpawnBlock(x, y, z):
	var superId = str(int(x / 4)) + "_" + str(int(y / 4)) + "_" + str(int(z / 4))

	if !_super_chunks.has(superId):
		var newChunk: Node3D = _chunk.instantiate()
		newChunk.name = superId
		add_child(newChunk)
		_super_chunks[superId] = newChunk.get_path()

	var chunk: Node = get_node(_super_chunks[superId]);

	var id = str(x)+"_"+str(y)+"_"+str(z)
	if !_chunks.has(id):
		var box = _air;
		if y < 0 || x < 0 || z < 0:
			box = _ground;
		var newBlock = box.instantiate()
		newBlock.scale = Vector3(8, 8, 8);
		newBlock.position = Vector3(x * 8 + 4, y * 8 + 4, z * 8 + 4);
		newBlock.name = id;
		chunk.add_child(newBlock);
		_chunks[id] = newBlock.get_path();
	var octree = "01";
	if !_cache.has(id):
		_cache[id] = octree;
		UpdateBlock(chunk.get_node(_chunks[id]), octree);
		octree = await repository.get_octree_block(x, y, z)
		_cache[id] = octree;
		UpdateBlock(chunk.get_node(_chunks[id]), octree);

func UpdateBlock(block, octree):
	var updaters = block.find_children("*", "ChunkUpdater", true, false);
	for updater in updaters:
		updater.call_deferred("update_block", octree);
	var meshers = block.find_children("Mesher", "CSGCombiner3D", true, false);
	for mesher in meshers:
		mesher.call_deferred("makestuff");
