from multiprocessing import Queue
from time import sleep, monotonic
from VoxelEngine import main

TIME = 2

def intersect(id: str, mesh: str, players: list[tuple[str, str, bool]], queue: Queue):
    """
    Perform modification between meshes and generate an octree mesh.

    Args:
        id (str): Identifier for the modification task will be passed as first element in queue when done.
        mesh (str): Main mesh.
        players (list[tuple[str, str, bool]]): list of player meshes and their properties.
            Each player tuple contains (mesh, position, build).
            Position = 'x,y,z' 8-adics x = 10 => 1, 11 => 1 + 1/8, 27 => 2 + 7/8, 67 => 6 + 7/8, 101 => 1 + 1/64, 111 => 1 + 1/8 + 1/64.
        queue (Queue): Queue to store the result of the modification task.

    Returns:
        str: Octree mesh as a string.
    """
    start_time = monotonic()
    new_ground, triangles = main.mutate(mesh=mesh, players=players)
    used_time = monotonic() - start_time
    print(f"Terraformed in {used_time}")
    sleep(max(0, TIME - used_time - 1))
    queue.put((id, new_ground), timeout=10)
    compression_result = main.compress(triangles)
    queue.put((f'optimize({id})', compression_result), timeout=10)
    return new_ground


if __name__ == '__main__':
    from VoxelEngine import main
    queue = Queue()
    try:
        print(intersect('1', '''v 0 0 0
v 0 0 8
v 0 8 0
v 0 8 8
v 8 0 0
v 8 0 8
v 8 8 0
v 8 8 8

vn 1 0 0
vn -1 0 0
vn 0 1 0
vn 0 -1 0
vn 0 0 1
vn 0 0 -1

f 1//2 2 3//2
f 2 4//2 3//2
f 2//5 6//5 4//5
f 4//5 6//5 8//5
f 1//4 5//4 2//4
f 2//4 5//4 6//4
f 3 8//3 7//3
f 3 4//3 8//3
f 1//6 3//6 7//6
f 1//6 7//6 5//6
f 5//1 8//1 6//1
f 5//1 7//1 8//1
        ''', [('''v 0 0 0
v 0 0 1
v 0 1 0
v 0 1 1
v 1 0 0
v 1 0 1
v 1 1 0
v 1 1 1

vn 1 0 0
vn -1 0 0
vn 0 1 0
vn 0 -1 0
vn 0 0 1
vn 0 0 -1

f 1//2 2 3//2
f 2 4//2 3//2
f 2//5 6//5 4//5
f 4//5 6//5 8//5
f 1//4 5//4 2//4
f 2//4 5//4 6//4
f 3 8//3 7//3
f 3 4//3 8//3
f 1//6 3//6 7//6
f 1//6 7//6 5//6
f 5//1 8//1 6//1
f 5//1 7//1 8//1
        ''', '1,1,1', False)], queue))
    finally:
        queue.close()