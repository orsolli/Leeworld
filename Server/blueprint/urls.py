from django.urls import path
from . import views


urlpatterns = [
    path("", views.get_blueprint),
    path("print/", views.add_blueprint),
]
