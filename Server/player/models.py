from django.db import models
from django.contrib.auth.models import User



class Player(models.Model):
    id=models.BigAutoField(primary_key=True)
    user=models.ForeignKey(User, on_delete=models.CASCADE)
    mesh=models.CharField(max_length=2**16)
    position=models.CharField(max_length=100)
