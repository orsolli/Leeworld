from csg.core import CSG
from csg.geom import Polygon, Vertex, Vector

from VoxelEngine.int8 import from8Adic


def parseOBJ(mesh: str, translate: Vector = Vector(0, 0, 0)) -> list[Polygon]:
    """
    Parse the OBJ mesh file and return a list of polygons.

    Args:
        mesh (str): The path to the OBJ mesh file.
        translate (Vector, optional): Translation vector for the mesh vertices. Defaults to Vector(0,0,0).

    Returns:
        list[Polygon]: The list of polygons representing the mesh.
    """
    polygons = []

    objVertices: list[Vector] = []
    objNormals: list[Vector] = []

    # Process each line
    for line in mesh.split("\n"):
        # Check the line type
        if line.startswith("v "):
            # Process vertex data
            _, x, y, z = line.split(" ")
            objVertices.append(Vector(float(x), float(y), float(z)) + translate)
        elif line.startswith("vn "):
            # Process normal data
            _, x, y, z = line.split(" ")
            objNormals.append(Vector(float(x), float(y), float(z)))
        elif line.startswith("f "):
            # Process face data
            vertices = []
            for face in line[2:].split(" "):
                vertexData: list[str] = (face + "//").split("/")
                vertexIndex = int(vertexData[0]) - 1
                normalIndex = (
                    int((vertexData[2] + vertexData[1] + vertexData[0])[0]) - 1
                )
                vertices.append(
                    Vertex(objVertices[vertexIndex], normal=objNormals[normalIndex])
                )

            try:
                polygons.append(Polygon(vertices))
            except ZeroDivisionError:
                print(vertices)

    return polygons


def parsePos(position: str) -> Vector:
    """
    Parse the position string and return a Vector object.

    Args:
        position (str): The position string in the format "x,y,z".
        Each scalar is a big-endian number. Least significant number first. "671" means 6/128 + 7/64 + 1/8

    Returns:
        Vector: The parsed position as a Vector object.

    >>> parsePos("30,34,0")
    Vector(0.046875, 0.546875, 0)
    """
    xs, ys, zs = position.split(",")
    return Vector(from8Adic(xs), from8Adic(ys), from8Adic(zs))


def calculate_normal(
    i: int, j: int, k: int, verts: list[tuple[float, float, float]]
) -> int:
    """
    Calculate the normal of a face defined by the given vertex indices.

    Args:
        i (int): The index of the first vertex.
        j (int): The index of the second vertex.
        k (int): The index of the third vertex.
        verts (list[tuple[float, float, float]]): The list of vertices.

    Returns:
        int: The normal of the face.
    """
    a: Vector = Vector(verts[j]) - Vector(verts[i])
    b: Vector = Vector(verts[k]) - Vector(verts[j])
    normal_vector = a.cross(b).unit()
    if normal_vector.x > 0.5:
        return 1
    if normal_vector.x < -0.5:
        return 2
    if normal_vector.y > 0.5:
        return 3
    if normal_vector.y < -0.5:
        return 4
    if normal_vector.z > 0.5:
        return 5
    if normal_vector.z < -0.5:
        return 6
