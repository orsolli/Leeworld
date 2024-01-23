using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
public partial class TerrainRepositoryInMemory : Node
{
	private Godot.Collections.Dictionary<string, string> chunks = new Godot.Collections.Dictionary<string, string>(){
		{"0_0_0","01"},
		{"2_0_0","10110000010000000000010001000100"},
		{"2_-1_0","10110000010000000000010001000100"},
		{"0_2_1","10101011010101000101010101011010101010101000101"},
		{"1_0_0","10000001000010000010000000000000000000000000000"},
	};
	public string GetOctreeBlock(int x, int y, int z)
	{
		string storedBlock;
		if (chunks.TryGetValue($"{x}_{y}_{z}", out storedBlock))
		{
			return storedBlock;
		}
		if (x < 0 || y < 0 || z < 0)
		{
			storedBlock = "01";
		}
		else storedBlock = "00";
		chunks.Add($"{x}_{y}_{z}", storedBlock);
		return storedBlock;
	}

	private bool isBefore(List<Tuple<int, int>> self, int[] goal, int index)
	{
		var isIndexedItemBefore = true;
		if (index < self.Count() && index < goal.Length)
			isIndexedItemBefore = self[index].Item1 <= goal[index];
		var previousItemsGoal = goal.Take(index);
		var previousItemsSelf = self.Take(index).Select(v => v.Item1);
		var isPreviousItemsBefore = previousItemsSelf.SequenceEqual(previousItemsGoal);
		return isIndexedItemBefore && isPreviousItemsBefore;
	}

	private Vector3[] direction = new[]{
		new Vector3(0,  0,  0),
		new Vector3(1,  0,  0),
		new Vector3(0,  1,  0),
		new Vector3(1,  1,  0),
		new Vector3(0,  0,  1),
		new Vector3(1,  0,  1),
		new Vector3(0,  1,  1),
		new Vector3(1,  1,  1) // This order is "increment x first"
	};

	public void MutateBlock(float x, float y, float z, int level, bool isInside)
	{
		var block_id = new Vector3(Mathf.Floor(x / 8), Mathf.Floor(y / 8), Mathf.Floor(z / 8));
		var pos = new Vector3(x - block_id.X * 8, y - block_id.Y * 8, z - block_id.Z * 8);
		pos /= 8;
		pos -= Vector3.One / Mathf.Pow(2, level + 1);
		IEnumerable<int> path = new[] { 1 };
		for (int i = 0; i < level; i++)
		{
			int index = 0;
			if (Mathf.PosMod(pos.X, 1) >= 0.5f) index += 1;
			if (Mathf.PosMod(pos.Y, 1) >= 0.5f) index += 2;
			if (Mathf.PosMod(pos.Z, 1) >= 0.5f) index += 4;
			path = path.Append(index + 1);
			pos = pos * 2 - direction[index];
		}
		MutateOctreeBlock((int)block_id.X, (int)block_id.Y, (int)block_id.Z, path.ToArray(), isInside);
	}
	public string MutateOctreeBlock(int x, int y, int z, int[] path, bool isInside)
	{
		string octreeString = GetOctreeBlock(x, y, z);
		var pathMap = new List<Tuple<int, int>> { Tuple.Create(1, 0) };
		int i = 1;
		var startIndex = pathMap[0];

		while (isBefore(pathMap, path, i))
		{
			if (i < path.Length && i < pathMap.Count() && pathMap[i].Item1 == path[i])
			{
				startIndex = pathMap[i];
				i++;
			}
			if (octreeString[pathMap.Last().Item2] == '1') // If node is a branch
			{
				pathMap.Add(Tuple.Create(1, pathMap.Last().Item2 + 1)); // Go to first child one level deeper
			}
			else if (octreeString[pathMap.Last().Item2] == '0') // If node is a leaf
			{
				var nextStringIndex = pathMap.Last().Item2 + 2;
				if (pathMap.Last().Item1 == 8)
					pathMap.RemoveAt(pathMap.Count - 1); // Go to next child in parent
				pathMap[pathMap.Count - 1] = Tuple.Create(pathMap.Last().Item1 + 1, nextStringIndex); // Increment child-index and string-index
				while (pathMap.Last().Item1 > 8)
				{
					pathMap.RemoveAt(pathMap.Count - 1); // Go to next child in parent
					pathMap[pathMap.Count - 1] = Tuple.Create(pathMap.Last().Item1 + 1, nextStringIndex); // Increment child-index and string-index
				}
			}
			if (pathMap.Last().Item2 == octreeString.Length)
				break; // Return if end is reached
		}

		var first_part = octreeString.Substring(0, startIndex.Item2);
		var last_part = pathMap.Count() == 1 ? "" : octreeString.Substring(pathMap.Last().Item2);
		var new_part = toOctree(path.Skip(i).ToArray(), octreeString.Substring(startIndex.Item2 + 1, 1), isInside);
		octreeString = first_part + new_part + last_part;

		chunks.Remove($"{x}_{y}_{z}");
		chunks.Add($"{x}_{y}_{z}", octreeString);
		return octreeString;
	}

	public string toOctree(int[] path, string existingValue, bool isInside)
	{
		string cellValue = isInside ? "1" : "0";
		if (path.IsEmpty()) return "0" + cellValue;
		var octree = "0000000000000000".ToCharArray();

		for (int i = 0; i < 8; i++)
		{
			octree[i * 2 + 1] = i + 1 == path.Last() ? cellValue[0] : existingValue[0];
		}
		for (int j = path.Length - 2; j >= 0; j--)
		{
			var subtree = "";
			for (int i = 0; i < 8; i++)
			{
				if (i + 1 == path[j])
					subtree += "1" + new string(octree);
				else
					subtree += "0" + existingValue;
			}
			octree = subtree.ToCharArray();
		}

		return "1" + new string(octree);
	}

}
