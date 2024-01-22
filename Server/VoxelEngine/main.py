from VoxelEngine.csg import parseOBJ, parsePos, calculate_normal, CSG
from VoxelEngine.octree import build_octree, generate_octree_mesh


def process_verts(verts):
    return "\n".join(f"v {v[0]} {v[1]} {v[2]}" for v in verts)


def process_cells(cells: list[list[int]], verts: list[tuple[float]]):
    new_ground = """
vn 1 0 0
vn -1 0 0
vn 0 1 0
vn 0 -1 0
vn 0 0 1
vn 0 0 -1
"""
    triangles: list[
        tuple[
            tuple[float, float, float],
            tuple[float, float, float],
            tuple[float, float, float],
        ]
    ] = []
    for cell in cells:
        n = calculate_normal(*cell[0:3], verts)
        i = 0
        j = 1
        for k in range(2, len(cell)):
            new_ground += f"f {cell[i]+1}"
            if n != cell[i] + 1:
                new_ground += f"//{n}"

            new_ground += f" {cell[j]+1}"
            if n != cell[j] + 1:
                new_ground += f"//{n}"

            new_ground += f" {cell[k]+1}"
            if n != cell[k] + 1:
                new_ground += f"//{n}"

            triangles.append((verts[cell[i]], verts[cell[j]], verts[cell[k]]))
            new_ground += "\n"
            j = k
    return new_ground, triangles


def process_indices(triangles):
    vertices = []
    indices = []

    for triangle in triangles:
        for vertex in triangle.vertices:
            if vertex not in vertices:
                vertices.append(vertex)
            indices.append(vertices.index(vertex))

    return vertices, indices


def mutate(mesh: str, players: list[tuple[str, str, bool]]):
    """
    Perform modification between meshes and generate an octree mesh.

    Args:
        id (str): Identifier for the modification task.
        mesh (str): Main mesh.
        players (list[tuple[str, str, bool]]): list of player meshes and their properties.
            player.mesh (str): The die mesh to apply to the main mesh.
            players.position (str): The position string in the format "x,y,z". Each scalar is a little-endian number. Least significant number last. "176" means 1/8 + 7/64 + 6/128.
            player.build (bool): Wether to add to mesh.

    Returns:
        str: Octree mesh as a string.
    """
    a = CSG.fromPolygons(parseOBJ(mesh)) if mesh else CSG()

    for player_mesh, position, build in players:
        player_csg = CSG.fromPolygons(parseOBJ(player_mesh, parsePos(position) * 8))
        a = a + player_csg if build else a - player_csg

    verts, cells, count = a.toVerticesAndPolygons()
    new_ground, triangles = process_cells(cells, verts)
    new_ground = process_verts(verts) + new_ground

    return new_ground, triangles


def compress(
    triangles: list[
        tuple[
            tuple[float, float, float],
            tuple[float, float, float],
            tuple[float, float, float],
        ]
    ]
) -> str:
    """
    Perform compression on meshes by generate an octree mesh.

    Args:
        triangles (list[tuple[tuple[float,float,float],tuple[float,float,float],tuple[float,float,float]]]): Main mesh.

    Returns:
        str: Octree mesh as a string.
    """
    print("Building octree")
    vertices = sum(((sum(vertices, ())) for vertices in triangles), ())
    fine_grains = vertices
    for i in range(0, 6):
        fine_grains = [
            v for v in fine_grains if (v + 8 / (2**7)) % (8 / (2**i)) > 8 / (2**6)
        ]
        if len(fine_grains) == 0:
            break
    octree = build_octree(mesh=triangles, level=i + 1, size=8, center=(4, 4, 4))
    print("Built octree")
    triangles = generate_octree_mesh(octree)
    vertices, indices = process_indices(triangles)
    faces = [indices[i : i + 3] for i in range(0, len(indices), 3)]
    octo_ground, _ = process_cells(faces, vertices)
    octo_ground = process_verts(vertices) + octo_ground

    return octo_ground


def mesh_to_triangles(mesh: str):
    """
    Build triangle list from mesh

    Args:
        mesh (str): Main mesh.

    Returns:
        list[tuple[tuple[float,float,float],tuple[float,float,float],tuple[float,float,float]]]: List of triangles.
    """

    verts, cells, count = CSG.fromPolygons(parseOBJ(mesh)).toVerticesAndPolygons()
    new_ground, triangles = process_cells(cells, verts)

    return triangles
