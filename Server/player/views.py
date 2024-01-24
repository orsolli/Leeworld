from functools import lru_cache
from django.contrib import messages
from django.contrib.auth import authenticate, login, logout
from django.contrib.auth.decorators import login_required
from django.http import HttpResponse, HttpRequest, JsonResponse
from django.shortcuts import render, redirect
from django.views.decorators.csrf import csrf_exempt
from . import models

LIMIT = 4


@login_required
def register(request: HttpRequest):
    user = models.User.objects.get(id=request.user.id)
    count = models.Player.objects.filter(user=user).count()
    level = messages.INFO
    if count < LIMIT:
        HttpResponse(models.Player.objects.create(user=user).id)
    if count == LIMIT - 1:
        level = messages.WARNING
    elif count == LIMIT:
        level = messages.ERROR

    messages.add_message(
        request, level, f'You are limited to {LIMIT} profile{"s" if LIMIT != 1 else ""}'
    )
    return redirect("/")


def home(request: HttpRequest):
    return render(
        request,
        "home.html",
        {
            "can_register": LIMIT,
        },
    )


@login_required
def profiles_list(request: HttpRequest):
    players = models.Player.objects.filter(user=request.user.id)
    return HttpResponse(", ".join([str(p.id) for p in players]))


@login_required
def profile(request: HttpRequest, id: str):
    return get_profile(id)


@lru_cache(50)
def get_profile(id: str):
    player = models.Player.objects.get(id=id)
    return JsonResponse(
        {
            "level": player.level,
            "builder": player.builder,
        }
    )


@csrf_exempt
def login_user(request: HttpRequest):
    if request.user.is_authenticated:
        return HttpResponse("Authorized", status=200)
    if request.method == "GET":
        return HttpResponse("Unauthorized", status=401)
    if request.method == "POST":
        user = authenticate(
            request,
            username=request.POST["username"],
            password=request.POST["password"],
        )
        if user is not None:
            login(request, user)
            return HttpResponse("Authorized", status=200)
        return HttpResponse("Unauthorized", status=401)


@login_required
def logout_user(request: HttpRequest):
    logout(request)
    return HttpResponse()
