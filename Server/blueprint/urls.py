from django.urls import path
from . import views


urlpatterns = [
    path('print/', views.add_blueprint),
]