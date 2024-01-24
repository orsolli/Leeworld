use std::io::BufRead;

#[derive(Clone)]
struct OctreeNode {
    children: Option<[Box<OctreeNode>; 8]>,
    is_inside: bool,
    is_outside: bool,
}

impl OctreeNode {
    fn new() -> Self {
        Self {
            children: None,
            is_inside: false,
            is_outside: false,
        }
    }
}
fn is_inside_mesh(mesh: &[[[f64; 3]; 3]], point: [f64; 3]) -> bool {
    let ray_origin = point;
    let ray_direction = [0.0039061, 0.0078122, 0.999962]; // Direction of the ray, assuming a ray along the x-axis

    let mut intersect_count = 0;

    for face in mesh.iter() {
        let v0 = face[0];
        let v1 = face[1];
        let v2 = face[2];

        if is_point_on_face(v0, v1, v2, ray_origin) {
            return true;
        }

        if ray_intersects_face(v0, v1, v2, ray_origin, ray_direction) {
            intersect_count += 1;
        }
    }

    // If the intersect count is odd, the point is inside the mesh
    intersect_count % 2 == 1
}

fn is_point_on_face(v0: [f64; 3], v1: [f64; 3], v2: [f64; 3], point: [f64; 3]) -> bool {
    // Check if the point is exactly on one of the face vertices
    point == v0 || point == v1 || point == v2
}

fn ray_intersects_face(
    v0: [f64; 3],
    v1: [f64; 3],
    v2: [f64; 3],
    ray_origin: [f64; 3],
    ray_direction: [f64; 3],
) -> bool {
    // Find the intersection point between the ray and the face plane
    if let Some(intersection) = ray_plane_intersection(v0, v1, v2, ray_origin, ray_direction) {
        // Check if the intersection point is within the face boundaries
        return is_point_in_triangle(intersection, v0, v1, v2);
    }
    false
}

fn ray_plane_intersection(
    v0: [f64; 3],
    v1: [f64; 3],
    v2: [f64; 3],
    ray_origin: [f64; 3],
    ray_direction: [f64; 3],
) -> Option<[f64; 3]> {
    // Find the intersection point between the ray and the face plane
    let edge1 = subtract_vector(v1, v0);
    let edge2 = subtract_vector(v2, v0);
    let normal = cross_product(edge1, edge2);

    let denom = dot_product(normal, ray_direction);

    // Check if the ray is parallel or nearly parallel to the face plane
    if f64::abs(denom) < 1e-6 {
        return None;
    }

    let t = dot_product(subtract_vector(v0, ray_origin), normal) / denom;

    // Check if the intersection point is behind the ray origin
    if t < 0.0 {
        return None;
    }

    Some(add_vector(ray_origin, scale_vector(ray_direction, t)))
}

fn is_point_in_triangle(point: [f64; 3], v0: [f64; 3], v1: [f64; 3], v2: [f64; 3]) -> bool {
    // Check if the point is within the triangle formed by v0, v1, and v2
    let edge0 = subtract_vector(v1, v0);
    let edge1 = subtract_vector(v2, v1);
    let edge2 = subtract_vector(v0, v2);

    let normal = cross_product(edge0, edge1);

    // Check if the point is on the same side of each triangle edge
    dot_product(cross_product(edge0, subtract_vector(point, v0)), normal) >= 0.0
        && dot_product(cross_product(edge1, subtract_vector(point, v1)), normal) >= 0.0
        && dot_product(cross_product(edge2, subtract_vector(point, v2)), normal) >= 0.0
}

fn scale_vector(v1: [f64; 3], s: f64) -> [f64; 3] {
    [v1[0] * s, v1[1] * s, v1[2] * s]
}

fn dot_product(v1: [f64; 3], v2: [f64; 3]) -> f64 {
    v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2]
}

fn cross_product(v1: [f64; 3], v2: [f64; 3]) -> [f64; 3] {
    [
        v1[1] * v2[2] - v1[2] * v2[1],
        v1[2] * v2[0] - v1[0] * v2[2],
        v1[0] * v2[1] - v1[1] * v2[0],
    ]
}

fn subtract_vector(v1: [f64; 3], v2: [f64; 3]) -> [f64; 3] {
    [v1[0] - v2[0], v1[1] - v2[1], v1[2] - v2[2]]
}

fn add_vector(v1: [f64; 3], v2: [f64; 3]) -> [f64; 3] {
    [v1[0] + v2[0], v1[1] + v2[1], v1[2] + v2[2]]
}

fn get_child_centers(center: [f64; 3], size: f64) -> [[f64; 3]; 8] {
    let x = center[0];
    let y = center[1];
    let z = center[2];
    let half_size = size / 2.0;

    [
        [x - half_size, y - half_size, z - half_size],
        [x - half_size, y - half_size, z + half_size],
        [x - half_size, y + half_size, z - half_size],
        [x - half_size, y + half_size, z + half_size],
        [x + half_size, y - half_size, z - half_size],
        [x + half_size, y - half_size, z + half_size],
        [x + half_size, y + half_size, z - half_size],
        [x + half_size, y + half_size, z + half_size],
    ]
}

fn build_octree(mesh: &[[[f64; 3]; 3]], level: i32, size: f64, center: [f64; 3]) -> OctreeNode {
    let mut node = OctreeNode::new();

    if level == 0 {
        node.is_inside = is_inside_mesh(mesh, center);
        node.is_outside = !node.is_inside;
        return node;
    }

    let child_size = size / 2.0;
    let child_centers = get_child_centers(center, child_size);
    let mut children: Vec<OctreeNode> = Vec::new();

    for i in 0..8 {
        let child_center = &child_centers[i];
        let ot = build_octree(mesh, level - 1, child_size, *child_center);

        if ot.is_inside {
            node.is_inside = true;
        }
        if ot.is_outside {
            node.is_outside = true;
        }

        children.push(ot);
    }

    if node.is_inside == node.is_outside {
        node.children = Some(*Box::new(
            <Vec<Box<OctreeNode>> as TryInto<[Box<OctreeNode>; 8]>>::try_into(
                children
                    .into_iter()
                    .map(|x| Box::new(x))
                    .collect::<Vec<Box<OctreeNode>>>(),
            )
            .unwrap_or_else(|_v: Vec<Box<OctreeNode>>| panic!()),
        ));
    }

    node
}

fn main() {
    let level: i32 = 6;
    let mut triangles: Vec<((f64, f64, f64), (f64, f64, f64), (f64, f64, f64))> = Vec::new();
    let stdin = std::io::stdin();
    for line in stdin.lock().lines() {
        if let Ok(line) = line {
            let values: Vec<f64> = line
                .split_whitespace()
                .filter_map(|s| s.parse().ok())
                .collect();
            if values.len() == 9 {
                let v0 = (values[0], values[1], values[2]);
                let v1 = (values[3], values[4], values[5]);
                let v2 = (values[6], values[7], values[8]);
                triangles.push((v0, v1, v2));
            }
        }
    }

    let converted: Vec<[[f64; 3]; 3]> = triangles
        .into_iter()
        .map(|(a, b, c)| [[a.0, a.1, a.2], [b.0, b.1, b.2], [c.0, c.1, c.2]])
        .collect();

    let octree = build_octree(&converted.as_slice(), level, 8.0, [4.0, 4.0, 4.0]);
    serialize_octree(&octree);
}

fn serialize_octree(octree: &OctreeNode) {
    fn go_deeper(c: &[Box<OctreeNode>; 8]) {
        print!("1");
        for i in 0..8 {
            serialize_octree(&c[i]);
        }
    }
    fn stop(octree: &OctreeNode) {
        print!("0");
        if octree.is_inside {
            print!("1");
        } else if octree.is_outside {
            print!("0");
        } else {
            panic!();
        }
    }
    match &octree.children {
        Some(c) => go_deeper(&c),
        None => stop(octree),
    }
}
