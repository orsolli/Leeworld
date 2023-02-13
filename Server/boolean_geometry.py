from multiprocessing import Queue
from time import sleep, monotonic
from csg.core import CSG
from csg.geom import Polygon, Vertex, Vector



def intersect(id: str, mesh: str, players: tuple[str, str, bool], queue: Queue):
    """
    players = (mesh, position)
    position = x,y,z
    x = 10 => 1, 11 => 1 + 1/8, 27 => 2 + 7/8, 87 => 8 + 7/8, 101 => 1 + 1/64, 111 => 1 + 1/8 + 1/64
    """
    start_time = monotonic()
    a = CSG.fromPolygons(parseOBJ(mesh))
    for player_mesh, position, build in players:
        if build:
            a = a + CSG.fromPolygons(parseOBJ(player_mesh, parsePos(position)))
        else:
            a = a - CSG.fromPolygons(parseOBJ(player_mesh, parsePos(position)))

    verts, cells, count = a.toVerticesAndPolygons()

    new_ground = ''
    for v in verts:
        new_ground += f'v {v[0]} {v[1]} {v[2]}\n'

    new_ground += """
vn 1 0 0
vn -1 0 0
vn 0 1 0
vn 0 -1 0
vn 0 0 1
vn 0 0 -1

"""

    for cell in cells:
        n = calculate_normal(*cell[0:3], verts)
        i = 0
        j = 1
        for k in range(2, len(cell)):
            new_ground += f'f {cell[i]+1}'
            if n != cell[i]+1:
                new_ground += f'//{n}'

            new_ground += f' {cell[j]+1}'
            if n != cell[j]+1:
                new_ground += f'//{n}'

            new_ground += f' {cell[k]+1}'
            if n != cell[k]+1:
                new_ground += f'//{n}'

            new_ground += '\n'
            j = k
    used_time = monotonic() - start_time
    sleep(max(0, 4 - used_time)) # Add consistent time
    queue.put((id, new_ground), timeout=10)
    print(f"Terraformed in {used_time}")


def parseOBJ(mesh: str, translate: Vector = Vector(0,0,0)):
    polygons = []

    objVertices: list[Vector] = []
    objNormals: list[Vector] = []

    # Process each line
    for line in mesh.split('\n'):

        # Check the line type
        if line.startswith("v "):
            # Process vertex data
            _, x, y, z = line.split(' ')
            objVertices.append(Vector(float(x), float(y), float(z)) + translate)
        elif line.startswith("vn "):
            # Process normal data
            _, x, y, z = line.split(' ')
            objNormals.append(Vector(float(x), float(y), float(z)))
        elif line.startswith("f "):
            # Process face data
            vertices = []
            for face in line[2:].split(' '):
                vertexData: list[str] = (face + "//").split('/')
                vertexIndex = int(vertexData[0]) - 1
                normalIndex = int((vertexData[2] + vertexData[1] + vertexData[0])[0]) - 1
                vertices.append(Vertex(objVertices[vertexIndex], normal=objNormals[normalIndex]))

            try:
                polygons.append(Polygon(vertices))
            except ZeroDivisionError:
                print(vertices)

    return polygons


def parsePos(position: str):
    xs, ys, zs = position.split(',')
    x, y, z = 0,0,0

    while xs:
        x = x/8 + int(xs[-1], base=8)
        xs = xs[:-1]

    while ys:
        y = y/8 + int(ys[-1], base=8)
        ys = ys[:-1]

    while zs:
        z = z/8 + int(zs[-1], base=8)
        zs = zs[:-1]

    return Vector(x, y, z)


def calculate_normal(i, j, k, verts):
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
