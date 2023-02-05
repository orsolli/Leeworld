from django.contrib import messages
from django.contrib.auth.decorators import login_required
from django.http import HttpResponse, HttpRequest
from django.shortcuts import render, redirect
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

LIMIT = 4

@login_required
def register(request: HttpRequest):
    user = models.User.objects.get(id=request.user.id)
    count = models.Player.objects.filter(user=user).count()
    level = messages.INFO
    if count < LIMIT:
        HttpResponse(models.Player.objects.create(user=user, mesh=player_mesh).id)
    if count == LIMIT-1:
        level = messages.WARNING
    elif count == LIMIT:
        level = messages.ERROR

    messages.add_message(request, level, f'You are limited to {LIMIT} profile{"s" if LIMIT != 1 else ""}')
    return redirect('/')

@login_required
def profile(request: HttpRequest):
    players = models.Player.objects.filter(user=request.user.id)
    return render(request, 'profile.html', {
        'players': players,
        'can_register': players.count() < LIMIT,
    })
