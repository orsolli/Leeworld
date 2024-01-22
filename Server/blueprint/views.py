from django.http import HttpRequest, HttpResponse, JsonResponse
from django.contrib.auth.decorators import login_required
from django.views.decorators.csrf import csrf_exempt

from VoxelEngine.int8 import to8Adic
from VoxelEngine.main import mesh_to_triangles

from . import models

from datetime import datetime


def get_blueprint(request: HttpRequest):
    block_id = request.GET["block"]
    if not block_id.count("_") == 2:
        return HttpResponse(
            "block must be three numbers separated by underscore (x_y_z)"
        )
    block_id = ",".join([str(int(p)) for p in block_id.split("_")])

    if not models.Block.objects.filter(position=block_id).exists():
        return JsonResponse([], safe=False)

    blueprints = models.Blueprint.objects.filter(
        block=models.Block.objects.get(position=block_id),
        # timestamp__gt=datetime.utcnow() - timedelta(1),
    )
    return JsonResponse(
        [
            {
                "player": blueprint.player.id,
                "block": blueprint.block.position,
                "position": blueprint.position,
                "size_x": blueprint.size_x,
                "size_y": blueprint.size_y,
                "size_z": blueprint.size_z,
            }
            for blueprint in blueprints
        ],
        safe=False,
    )  # safe is False because data is list instead of dict


@login_required
@csrf_exempt
def add_blueprint(request: HttpRequest):
    player_id = request.POST["player"]
    if not models.Player.objects.filter(id=player_id, user=request.user.id).exists():
        return HttpResponse("Player not found")

    block_position = request.POST["block"]
    if not block_position.count("_") == 2:
        return HttpResponse(
            "block must be three numbers separated by underscore (x_y_z)"
        )
    block_position = ",".join([str(int(p)) for p in block_position.split("_")])

    if not models.Block.objects.filter(position=block_position).exists():
        return HttpResponse("Block not found")

    position = request.POST["position"]
    if not position.count("_") == 2:
        return HttpResponse(
            "Position must be three eightadic numbers separated by underscore (x_y_z)"
        )
    position = [int(p, base=8) for p in position.split("_")]

    size = request.POST["size"]
    if not size.count("_") == 2:
        return HttpResponse(
            "Size must be three numbers separated by underscore (x_y_z)"
        )
    size = [int(s) for s in size.split("_")]
    if sum(abs(s) for s in size) > 64:
        return HttpResponse("Blueprint is too large")

    player = models.Player.objects.get(id=player_id)
    triangles = mesh_to_triangles(player.mesh)

    max_vertex = max(
        (
            max(
                vertices,
                key=lambda vertex: vertex[0] ** 2 + vertex[1] ** 2 + vertex[2] ** 2,
            )
            for vertices in triangles
        ),
        key=lambda vertex: vertex[0] ** 2 + vertex[1] ** 2 + vertex[2] ** 2,
    )
    min_vertex = min(
        (
            min(
                vertices,
                key=lambda vertex: vertex[0] ** 2 + vertex[1] ** 2 + vertex[2] ** 2,
            )
            for vertices in triangles
        ),
        key=lambda vertex: vertex[0] ** 2 + vertex[1] ** 2 + vertex[2] ** 2,
    )
    scale = (
        max_vertex[0] - min_vertex[0],
        max_vertex[1] - min_vertex[1],
        max_vertex[2] - min_vertex[2],
    )

    s = [
        int(size[0] / abs(size[0])) if size[0] != 0 else 1,
        int(size[1] / abs(size[1])) if size[1] != 0 else 1,
        int(size[2] / abs(size[2])) if size[2] != 0 else 1,
    ]

    for x in range(0, size[0] + s[0], s[0]):
        for y in range(0, size[1] + s[1], s[1]):
            for z in range(0, size[2] + s[2], s[2]):
                pos = f"""{
                        position[0]+int(to8Adic(x*scale[0]/8), base=8):o},{
                        position[1]+int(to8Adic(y*scale[1]/8), base=8):o},{
                        position[2]+int(to8Adic(z*scale[2]/8), base=8):o}"""
                if "-" in pos:
                    break
                models.Blueprint.objects.create(
                    player=player,
                    block=models.Block.objects.get_or_create(
                        defaults={
                            "mesh": models.default_block_mesh
                            if "-" in block_position
                            else ""
                        },
                        position=block_position,
                    )[0],
                    position=pos,
                    size_x=0,
                    size_y=0,
                    size_z=0,
                    timestamp=datetime.utcnow(),
                )
    return HttpResponse()
