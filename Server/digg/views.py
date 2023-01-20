from django.http import HttpResponse, HttpRequest
from . import models, signals



cube = """v 0 0 0
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
"""


def get_block(request: HttpRequest):
    block, created = models.Block.objects.get_or_create(
        defaults={'mesh':cube},
        position=request.GET['block']
    )
    return HttpResponse(block.mesh, status=201 if created else 200)


def request_digg_block(request: HttpRequest):
    block_position = request.GET['block']
    if not models.Block.objects.filter(position=block_position).exists():
        return HttpResponse("Block not found")

    player_id = request.GET['player']
    if not models.Player.objects.filter(id=player_id).exists():
        return HttpResponse("Player not found")

    position = request.GET['position']
    if not position.count(',') == 2:
        return HttpResponse("Position must be three numbers separated by comma (x,y,z)")

    # Start digging process
    signals.terraformer_signal.send(
        sender=None,
        action=signals.DIGG,
        player_id=player_id,
        block_position=block_position,
        position=position,
    )
    return HttpResponse("")
