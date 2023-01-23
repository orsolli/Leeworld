from django.db import models



class Player(models.Model):
    id=models.BigAutoField(primary_key=True)
    mesh=models.CharField(max_length=2**16)
    position=models.CharField(max_length=100)
