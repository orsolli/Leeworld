from django.http import HttpRequest, HttpResponse
from django.contrib.auth.decorators import login_required
from django.views.decorators.csrf import csrf_exempt

from . import models

from datetime import datetime, timedelta


def get_blueprint(request: HttpRequest):
    block_id = request.GET['block']
    if not block_id.count('_') == 2:
        return HttpResponse("block must be three numbers separated by underscore (x_y_z)")
    block_id = '_'.join([str(int(p)) for p in block_id.split('_')])

    if not models.Block.objects.get(position=block_id):
        return HttpResponse("Block not found")

    blueprints = models.Blueprint.objects.filter(
        block=models.Block.objects.get(position=block_id),
        timestamp__gt=datetime.utcnow() - timedelta(1)
    )
    return HttpResponse(blueprints, status=200)

@login_required
@csrf_exempt
def add_blueprint(request: HttpRequest):
    player_id = request.POST['player']
    if not models.Player.objects.filter(id=player_id, user=request.user.id).exists():
        return HttpResponse("Player not found")

    block_position = request.POST['block']
    if not block_position.count('_') == 2:
        return HttpResponse("block must be three numbers separated by underscore (x_y_z)")
    block_position = ','.join([str(int(p)) for p in block_position.split('_')])

    if not models.Block.objects.filter(position=block_position).exists():
        return HttpResponse("Block not found")

    position = request.POST['position']
    if not position.count('_') == 2:
        return HttpResponse("Position must be three numbers separated by underscore (x_y_z)")
    position = ','.join([str(int(p)) for p in position.split('_')])

    size = request.POST['size']
    if not size.count('_') == 2:
        return HttpResponse("Size must be three numbers separated by underscore (x_y_z)")
    size = [int(s) for s in size.split('_')]
    if (sum(abs(s) for s in size) > 64):
        return HttpResponse("Blueprint is too large")

    models.Blueprint.objects.create(
        player=models.Player.objects.get(id=player_id),
        block=models.Block.objects.get_or_create(defaults={'mesh':models.default_block_mesh if '-' in block_position else ''}, position=block_position)[0],
        position=position,
        size_x=size[0],
        size_y=size[1],
        size_z=size[2],
        timestamp=datetime.utcnow(),
    )
    return HttpResponse()
