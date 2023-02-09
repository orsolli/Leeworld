from django.urls import path, include
from . import views


urlpatterns = [
    path('register/', views.register),
    path('', views.profile),
    path('profiles/', views.profiles_list),
    path('auth/', include('django.contrib.auth.urls')),
]