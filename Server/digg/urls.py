from django.urls import path
from . import views


urlpatterns = [
    path('request/', views.request_digg_block),
    path('block/', views.get_block),
]