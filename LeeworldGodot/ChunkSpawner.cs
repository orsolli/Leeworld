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
	private int distance;
	[Export]
	public int maxDistance = 3;
	private Vector3 previousPos;

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
			if (previousPos.X != x
			 || previousPos.Y != y
			 || previousPos.Z != z
			)
			{
				distance = 1;
			}
			else
			{
				distance = (distance + 1) % maxDistance + 1;
			}

			SpawnBlock(x, y, z);
			for (int d = 1; d <= distance; d++)
			{
				SpawnBlock(x, y - d, z);
				SpawnBlock(x + d, y, z);
				SpawnBlock(x - d, y, z);

				SpawnBlock(x, y, z + d);
				SpawnBlock(x, y, z - d);

				SpawnBlock(x, y + d, z);
			}
			for (int d1 = 1; d1 <= distance; d1++)
			{
				for (int d2 = 1; d2 <= distance; d2++)
				{
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
				}
			}
			for (int d1 = distance; d1 > 0; d1--)
			{
				for (int d2 = distance; d2 > 0; d2--)
				{
					for (int d3 = distance; d3 > 0; d3--)
					{
						SpawnBlock(x + d1, y - d2, z + d3);
						SpawnBlock(x + d1, y - d2, z - d3);
						SpawnBlock(x - d1, y - d2, z + d3);
						SpawnBlock(x - d1, y - d2, z - d3);
						SpawnBlock(x + d1, y + d2, z + d3);
						SpawnBlock(x + d1, y + d2, z - d3);
						SpawnBlock(x - d1, y + d2, z + d3);
						SpawnBlock(x - d1, y + d2, z - d3);
					}
				}
			}
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
