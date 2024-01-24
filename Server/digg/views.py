from time import sleep
from django.http import HttpResponse, HttpRequest
from django.views.decorators.csrf import csrf_exempt
from . import models, signals


def get_block(request: HttpRequest):
    block_id = request.GET["block"]
    if not block_id.count("_") == 2:
        return HttpResponse(
            "block must be three numbers separated by underscore (x_y_z)"
        )
    block_id = "_".join([str(int(p)) for p in block_id.split("_")])
    block, created = models.Block.objects.get_or_create(
        defaults={"mesh": models.default_block_mesh if "-" in block_id else ""},
        position=block_id.replace("_", ","),
    )
    return HttpResponse(block.mesh, status=201 if created else 200)


@csrf_exempt
def request_digg_block(request: HttpRequest):
    player_id = request.GET["player"]
    if not models.Player.objects.filter(id=player_id).exists():
        return HttpResponse("Player not found")

    if request.method == "DELETE":
        results = signals.terraformer_signal.send(
            sender=None,
            action=signals.STOP,
            player_id=player_id,
        )
        for receiver, response in results:
            if receiver == signals.terraformer_signal_handler:
                return HttpResponse(response, status=200)

    block_position = request.GET["block"]
    if not block_position.count("_") == 2:
        return HttpResponse(
            "block must be three numbers separated by underscore (x_y_z)"
        )
    block_position = ",".join([str(int(p)) for p in block_position.split("_")])

    if not models.Block.objects.filter(position=block_position).exists():
        return HttpResponse("Block not found")

    position = request.GET["position"]
    if not position.count("_") == 2:
        return HttpResponse(
            "Position must be three numbers separated by underscore (x_y_z)"
        )
    position = ",".join([str(int(p)) for p in position.split("_")])

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
            # status = {"Waiting": 202, "Done": 201}.get(response, 400)
            if response == "Waiting":
                sleep(1)
            return HttpResponse(response)
