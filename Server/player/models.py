from django.db import models
from django.contrib.auth.models import User



class Player(models.Model):
    id=models.BigAutoField(primary_key=True)
    user=models.ForeignKey(User, on_delete=models.CASCADE)
    mesh=models.TextField()
    builder=models.BooleanField(default=False)
    position=models.CharField(max_length=100)
