from django.http import HttpResponse, HttpRequest
from . import models



player_mesh = """v 0 0 0
v 0 0 1
v 0 1 0
v 0 1 1
v 1 0 0
v 1 0 1
v 1 1 0
v 1 1 1

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


def register(request: HttpRequest):
    return HttpResponse(models.Player.objects.create(mesh=player_mesh).id)