extends Node

@export var _http_client: HttpClient

var chunks = {}
var queue_in = []
var queue_out = []
var routines: Semaphore
var queue_mutex: Mutex

func _ready():
	queue_mutex = Mutex.new()
	routines = Semaphore.new()
	routines.post()
	routines.post()
	routines.post()
	routines.post()
	routines.post()

func _process(_delta: float):
	if len(queue_in) > 0 and routines.try_wait():
		
		queue_mutex.lock()
		var pos = queue_in.pop_front()
		if pos in queue_out:
			queue_mutex.unlock()
			routines.post()
			return
		queue_out.push_back(pos)
		queue_mutex.unlock()
		
		chunks[pos] = await _http_client.request("/digg/block/" + pos + "/")

		queue_mutex.lock()
		queue_out.remove_at(queue_out.find(pos))
		queue_mutex.unlock()
		routines.post()

func to8Adic(num: float):
	"""
	Convert to 8-adic number,

	a big-endian number. Least significant number first. '671' means 6/128 + 7/64 + 1/8

	Args:
		num (float): A number less than 1

	>>> [to8Adic(n) for n in [0.0, 0.125, 0.25, 0.375, 0.5, 0.625]]
	['000', '001', '002', '003', '004', '005']
	>>> to8Adic(0.046875)
	'030'
	>>> to8Adic(0.546875)
	'034'
	>>> to8Adic(1)
	Traceback (most recent call last):
		...
	ValueError: |num| must be < 1
	"""

	var res = ""
	if num == 0:
		return "0"
	if num >= 1 or num < 0:
		return ERR_INVALID_PARAMETER
	var s = sign(num)
	num = abs(num)
	num *= 8
	while num >= int(num) and len(res) < 3:
		if num < 1.0 / 512:
			break
		var i = int(num)
		res = str(i) + str(res)
		num -= i
		num *= 8

	if len(res) == 0:
		return "0"
	return ("-" if s < 0 else "") + res

func get_octree_block(x, y, z):
	var pos = str(x) + "/" + str(y) + "/" + str(z)
	if pos not in chunks and pos not in queue_in and pos not in queue_out:
		queue_mutex.lock()
		queue_in.append(pos)
		queue_mutex.unlock()
	if pos in chunks:
		return chunks[pos]
	return "01"


func posToPath(x: float, y: float, z: float, level: int):
	
	# The order of this array must be according to specifications.
	var direction = PackedVector3Array([
		Vector3(-1,	-1,	-1),
		Vector3(1,	-1,	-1),
		Vector3(-1,	1,	-1),
		Vector3(1,	1,	-1),
		Vector3(-1,	-1,	1),
		Vector3(1,	-1,	1),
		Vector3(-1,	1,	1),
		Vector3(1,	1,	1) # This order is "increment x first"
	])

	var X: int = floori(x / 8.0)
	var Y: int = floori(y / 8.0)
	var Z: int = floori(z / 8.0)

	var pos = [x / 8.0 - X, y / 8.0 - Y, z / 8.0 - Z]
	var path = []
	for i in range(level):
		var index = 0
		if fposmod(pos[0], 1.0) >= 0.5:
			index += 1
			pos[0] -= 1
		if fposmod(pos[1], 1.0) >= 0.5:
			index += 2
			pos[1] -= 1
		if fposmod(pos[2], 1.0) >= 0.5:
			index += 4
			pos[2] -= 1
		path.push_back(index)
		pos[0] = pos[0] * 2 - direction[index][0]
		pos[1] = pos[1] * 2 - direction[index][1]
		pos[2] = pos[2] * 2 - direction[index][2]

	return path

func MutateBlock(x, y, z, level, isInside):
	var X: int = floori(x / 8.0)
	var Y: int = floori(y / 8.0)
	var Z: int = floori(z / 8.0)

	var pos = str(X) + "/" + str(Y) + "/" + str(Z)

	var path = posToPath(x, y, z, level)
	var value = "1" if isInside else "0"

	chunks[pos] = await _http_client.put("/digg/block/" + pos + "/" + value, str(path))
