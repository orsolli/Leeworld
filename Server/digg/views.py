from time import sleep
from django.http import HttpResponse, HttpRequest
from django.views.decorators.csrf import csrf_exempt
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
    block_id = request.GET['block']
    if not block_id.count('_') == 2:
        return HttpResponse("block must be three numbers separated by underscore (x_y_z)")
    block, created = models.Block.objects.get_or_create(
        defaults={'mesh':cube if '-' in block_id else ''},
        position=block_id.replace('_', ',')
    )
    return HttpResponse(block.mesh, status=201 if created else 200)

@csrf_exempt
def request_digg_block(request: HttpRequest):

    player_id = request.GET['player']
    if not models.Player.objects.filter(id=player_id).exists():
        return HttpResponse("Player not found")

    if request.method == 'DELETE':
        results = signals.terraformer_signal.send(
            sender=None,
            action=signals.STOP,
            player_id=player_id,
        )
        for receiver, response in results:
            if receiver == signals.terraformer_signal_handler:
                return HttpResponse(response, status=200)

    block_position = request.GET['block']
    if not block_position.count('_') == 2:
        return HttpResponse("block must be three numbers separated by underscore (x_y_z)")
    block_position = block_position.replace('_', ',')

    if not models.Block.objects.filter(position=block_position).exists():
        return HttpResponse("Block not found")

    position = request.GET['position']
    if not position.count('_') == 2:
        return HttpResponse("Position must be three numbers separated by underscore (x_y_z)")
    position = position.replace('_', ',')

    # Start digging process
    results = signals.terraformer_signal.send(
        sender=None,
        action=signals.DIGG,
        player_id=player_id,
        block_position=block_position,
        position=position,
    )
    for receiver, response in results:
        if receiver == signals.terraformer_signal_handler:
            #status = {"Waiting": 202, "Done": 201}.get(response, 400)
            if response == "Waiting":
                sleep(1)
            return HttpResponse(response)
