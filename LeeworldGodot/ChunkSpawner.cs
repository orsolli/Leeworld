using Godot;
using System;

public partial class ChunkSpawner : Node
{
	private Godot.Collections.Dictionary<string, NodePath> _chunks = new Godot.Collections.Dictionary<string, NodePath>();
	private Godot.Collections.Dictionary<string, NodePath> _super_chunks = new Godot.Collections.Dictionary<string, NodePath>();
	private Godot.Collections.Dictionary<string, string> _cache = new Godot.Collections.Dictionary<string, string>();
	private PackedScene _ground = (PackedScene)GD.Load("res://Terrain/ground_chunk.tscn");
	private PackedScene _air = (PackedScene)GD.Load("res://Terrain/air_chunk.tscn");
	private PackedScene _chunk = (PackedScene)GD.Load("res://Terrain/chunk.tscn");
	[Export]
	public NodePath observer;
	[Export]
	public TerrainRepositoryInMemory repository;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GetNode(observer) is Node3D obs)
		{
			var o = obs.Position;
			int x = Mathf.FloorToInt(o.X / 8f);
			int y = Mathf.FloorToInt(o.Y / 8f);
			int z = Mathf.FloorToInt(o.Z / 8f);
			SpawnBlock(x, y - 1, z);

			SpawnBlock(x + 1, y - 1, z);
			SpawnBlock(x - 1, y - 1, z);

			SpawnBlock(x, y - 1, z + 1);
			SpawnBlock(x, y - 1, z - 1);

			SpawnBlock(x + 1, y - 1, z + 1);
			SpawnBlock(x + 1, y - 1, z - 1);
			SpawnBlock(x - 1, y - 1, z + 1);
			SpawnBlock(x - 1, y - 1, z - 1);

			SpawnBlock(x, y, z);

			SpawnBlock(x + 1, y, z);
			SpawnBlock(x - 1, y, z);

			SpawnBlock(x, y, z + 1);
			SpawnBlock(x, y, z - 1);

			SpawnBlock(x + 1, y, z + 1);
			SpawnBlock(x + 1, y, z - 1);
			SpawnBlock(x - 1, y, z + 1);
			SpawnBlock(x - 1, y, z - 1);

			SpawnBlock(x, y + 1, z);

			SpawnBlock(x + 1, y + 1, z);
			SpawnBlock(x - 1, y + 1, z);

			SpawnBlock(x, y + 1, z + 1);
			SpawnBlock(x, y + 1, z - 1);

			SpawnBlock(x + 1, y + 1, z + 1);
			SpawnBlock(x + 1, y + 1, z - 1);
			SpawnBlock(x - 1, y + 1, z + 1);
			SpawnBlock(x - 1, y + 1, z - 1);
		}
	}

	private void SpawnBlock(int x, int y, int z)
	{
		string superId = $"{Mathf.FloorToInt(x / 4)}_{Mathf.FloorToInt(y / 4)}_{Mathf.FloorToInt(z / 4)}";

		if (!_super_chunks.ContainsKey(superId))
		{
			if (_chunk.Instantiate() is Node3D newChunk)
			{
				newChunk.Name = superId;
				AddChild(newChunk);
				_super_chunks.Add(superId, newChunk.GetPath());
			}
		}

		Node chunk = GetNode(_super_chunks[superId]);

		string id = $"{x}_{y}_{z}";
		if (!_chunks.ContainsKey(id))
		{
			PackedScene box = _air;
			if (y < 0 || x < 0 || z < 0)
			{
				box = _ground;
			}
			if (box.Instantiate() is Node3D newBlock)
			{
				newBlock.Scale = new Vector3(8, 8, 8);
				newBlock.Position = new Vector3(x * 8 + 4, y * 8 + 4, z * 8 + 4);
				newBlock.Name = id;
				chunk.AddChild(newBlock);
				_chunks.Add(id, newBlock.GetPath());
				repository.MutateOctreeBlock(0, 0, 0, new[] { 1, ((x + 8) % 8) + 1, ((z + 8) % 8) + 1, ((y + 8) % 8) + 1 }, true);
			}
		}
		string octree = repository.GetOctreeBlock(x, y, z);
		if (!_cache.ContainsKey(id))
		{
			_cache.Add(id, octree);
		}
		else if (_cache[id] == octree) return;

		var block = chunk.GetNode(_chunks[id]);
		var updaters = block.FindChildren("*", "ChunkUpdater", true, false);
		foreach (Node updater in updaters)
		{
			updater.CallDeferred("update_block", octree);
		}
		var meshers = block.FindChildren("Mesher", "CSGCombiner3D", true, false);
		foreach (Node mesher in meshers)
		{
			mesher.CallDeferred("makestuff");
		}
	}
}
