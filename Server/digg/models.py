from django.db import models
from player.models import Player

class Block(models.Model):
    mesh=models.TextField()
    position=models.CharField(primary_key=True, max_length=100)


class TerraformQueue(models.Model):
    player=models.OneToOneField(Player, primary_key=True, on_delete=models.DO_NOTHING)
    block=models.ForeignKey(Block, on_delete=models.DO_NOTHING)
    position=models.CharField(max_length=16)


class Terraformer(models.Model):
    player=models.OneToOneField(Player, primary_key=True, on_delete=models.DO_NOTHING)
    block=models.ForeignKey(Block, on_delete=models.DO_NOTHING)
    position=models.CharField(max_length=16)
