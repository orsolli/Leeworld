
class Triangle:
    def __init__(self, vertices: tuple[tuple[float, float, float], tuple[float, float, float], tuple[float, float, float]]):
        """
        Initialize a Triangle object with three vertices.
        
        Args:
            vertices (tuple[tuple[float, float, float], tuple[float, float, float], tuple[float, float, float]]): The vertices of the triangle.
        """
        self.vertices = vertices

class OctreeNode:
    def __init__(self, level: int, size: float, center: tuple[float, float, float]):
        """
        Initialize an OctreeNode object.
        
        Args:
            level (int): The level of the node in the octree.
            size (float): The size of the node.
            center (tuple[float, float, float]): The center coordinates of the node.
        """
        self.level = level
        self.size = size
        self.center = center
        self.children: list[OctreeNode] = [None] * 8
        self.is_inside = False
        self.is_outside = False

    def is_inside_mesh(self, mesh: list[tuple[tuple[float,float,float],tuple[float,float,float],tuple[float,float,float]]], point: tuple[float,float,float]) -> bool:
        """
        Check if a point is inside the mesh represented by the given list of triangles.
        
        Args:
            mesh (list[tuple[tuple[float,float,float],tuple[float,float,float],tuple[float,float,float]]]): The mesh as a list of triangles.
            point (tuple[float,float,float]): The point to check.
        
        Returns:
            bool: True if the point is inside the mesh, False otherwise.
        """
        ray_origin = point
        ray_direction = (0.0039061, 0.0078122, 0.999962)  # Direction of the ray, assuming a ray along the x-axis
        
        intersect_count = 0
        
        for face in mesh:
            v0, v1, v2 = face
            
            if self.is_point_on_face(v0, v1, v2, ray_origin):
                return True
            
            if self.ray_intersects_face(v0, v1, v2, ray_origin, ray_direction):
                intersect_count += 1
        
        # If the intersect count is odd, the point is inside the mesh
        return intersect_count % 2 == 1

    def is_point_on_face(self, v0: tuple[float, float, float], v1: tuple[float, float, float], v2: tuple[float, float, float], point: tuple[float, float, float]) -> bool:
        """
        Check if a point is on the face defined by the given vertices.
        
        Args:
            v0 (tuple[float, float, float]): The first vertex of the face.
            v1 (tuple[float, float, float]): The second vertex of the face.
            v2 (tuple[float, float, float]): The third vertex of the face.
            point (tuple[float, float, float]): The point to check.
        
        Returns:
            bool: True if the point is on the face, False otherwise.
        """
        # Check if the point is exactly on one of the face vertices
        return point == v0 or point == v1 or point == v2

    def ray_intersects_face(self, v0, v1, v2, ray_origin, ray_direction):
        # Find the intersection point between the ray and the face plane
        intersection = self.ray_plane_intersection(v0, v1, v2, ray_origin, ray_direction)
        
        if intersection is None:
            return False
        
        # Check if the intersection point is within the face boundaries
        return self.is_point_in_triangle(intersection, v0, v1, v2)

    def ray_plane_intersection(self, v0, v1, v2, ray_origin, ray_direction):
        # Find the intersection point between the ray and the face plane
        edge1 = self.subtract_vector(v1, v0)
        edge2 = self.subtract_vector(v2, v0)
        normal = self.cross_product(edge1, edge2)
        
        denom = self.dot_product(normal, ray_direction)
        
        # Check if the ray is parallel or nearly parallel to the face plane
        if abs(denom) < 1e-6:
            return None
        
        t = self.dot_product(self.subtract_vector(v0, ray_origin), normal) / denom
        
        # Check if the intersection point is behind the ray origin
        if t < 0:
            return None
        
        return self.add_vector(ray_origin, self.scale_vector(ray_direction, t))

    def is_point_in_triangle(self, point, v0, v1, v2):
        # Check if the point is within the triangle formed by v0, v1, and v2
        edge0 = self.subtract_vector(v1, v0)
        edge1 = self.subtract_vector(v2, v1)
        edge2 = self.subtract_vector(v0, v2)
        
        normal = self.cross_product(edge0, edge1)
        
        # Check if the point is on the same side of each triangle edge
        return (self.dot_product(self.cross_product(edge0, self.subtract_vector(point, v0)), normal) >= 0 and
                self.dot_product(self.cross_product(edge1, self.subtract_vector(point, v1)), normal) >= 0 and
                self.dot_product(self.cross_product(edge2, self.subtract_vector(point, v2)), normal) >= 0)

    def scale_vector(self, v1, s):
        return (v1[0] * s, v1[1] * s, v1[2] * s)

    def dot_product(self, v1, v2):
        return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2]

    def cross_product(self, v1, v2):
        return (v1[1] * v2[2] - v1[2] * v2[1],
                v1[2] * v2[0] - v1[0] * v2[2],
                v1[0] * v2[1] - v1[1] * v2[0])

    def subtract_vector(self, v1, v2):
        return (v1[0] - v2[0], v1[1] - v2[1], v1[2] - v2[2])

    def add_vector(self, v1, v2):
        return (v1[0] + v2[0], v1[1] + v2[1], v1[2] + v2[2])


def build_octree(mesh, level, size, center):
    node = OctreeNode(level, size, center)

    if level == 0:
        node.is_inside = node.is_inside_mesh(mesh, center)
        node.is_outside = not node.is_inside
        return node

    child_size = size / 2
    child_centers = get_child_centers(center, child_size)

    for i in range(8):
        child_center = child_centers[i]
        node.children[i] = build_octree(mesh, level - 1, child_size, child_center)

        if node.children[i].is_inside:
            node.is_inside = True
        if node.children[i].is_outside:
            node.is_outside = True

    if node.is_inside != node.is_outside:
        node.children = [None] * 8

    return node


def get_child_centers(center, size):
    x, y, z = center
    half_size = size / 2
    
    child_centers = [
        (x - half_size, y - half_size, z - half_size),
        (x - half_size, y - half_size, z + half_size),
        (x - half_size, y + half_size, z - half_size),
        (x - half_size, y + half_size, z + half_size),
        (x + half_size, y - half_size, z - half_size),
        (x + half_size, y - half_size, z + half_size),
        (x + half_size, y + half_size, z - half_size),
        (x + half_size, y + half_size, z + half_size)
    ]
    
    return child_centers


def generate_octree_mesh(node: OctreeNode):
    if node.is_inside and not node.is_outside:
        return create_box_triangles((*node.center, node.size))

    triangles = []
    for child in node.children:
        if child is not None:
            triangles.extend(generate_octree_mesh(child))

    return triangles

def create_box_triangles(bounds):
    x, y, z, size = bounds
    half_size = size / 2
    min_x, min_y, min_z = x - half_size, y - half_size, z - half_size
    max_x, max_y, max_z = x + half_size, y + half_size, z + half_size

    vertices = [
        (min_x, min_y, min_z),
        (max_x, min_y, min_z),
        (min_x, max_y, min_z),
        (max_x, max_y, min_z),
        (min_x, min_y, max_z),
        (max_x, min_y, max_z),
        (min_x, max_y, max_z),
        (max_x, max_y, max_z),
    ]

    box_triangles = [
        Triangle((vertices[1], vertices[0], vertices[2])),
        Triangle((vertices[1], vertices[2], vertices[3])),
        Triangle((vertices[4], vertices[5], vertices[6])),
        Triangle((vertices[6], vertices[5], vertices[7])),
        Triangle((vertices[0], vertices[1], vertices[4])),
        Triangle((vertices[4], vertices[1], vertices[5])),
        Triangle((vertices[3], vertices[2], vertices[6])),
        Triangle((vertices[3], vertices[6], vertices[7])),
        Triangle((vertices[2], vertices[0], vertices[4])),
        Triangle((vertices[2], vertices[4], vertices[6])),
        Triangle((vertices[1], vertices[3], vertices[5])),
        Triangle((vertices[5], vertices[3], vertices[7])),
    ]

    return box_triangles
