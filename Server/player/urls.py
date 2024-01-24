from django.urls import path, re_path
from . import views


urlpatterns = [
    path("register/", views.register),
    path("", views.home),
    path("profiles/", views.profiles_list),
    re_path(r"profile/(?P<id>\w+)/$", views.profile),
    path("auth/login/", views.login_user),
    path("auth/logout/", views.logout_user),
]
