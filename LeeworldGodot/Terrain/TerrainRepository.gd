extends Node

@export var _http_client: HttpClient

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
	return await _http_client.request("/digg/block/?block=" + str(x) + "_" + str(y) + "_" + str(z))

func mutate_block(x, y, z, level, isInside):
	var X: int = x / 8
	var Y: int = y / 8
	var Z: int = z / 8

	var pos = to8Adic((x % 8 + 8) % 8) + "_" + to8Adic((y % 8 + 8) % 8) + "_" + to8Adic((z % 8 + 8) % 8)

	var player = str(level)
	if isInside:
		player = "1" + str(level)
	return await _http_client.request("/request/?player=" + player + "&block=" + X + "_" + Y + "_" + Z + "&position=" + pos)
