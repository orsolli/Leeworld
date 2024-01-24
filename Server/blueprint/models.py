from django.db import models

from digg.models import Block, default_block_mesh
from player.models import Player


class Blueprint(models.Model):
    player = models.ForeignKey(Player, on_delete=models.CASCADE)
    block = models.ForeignKey(Block, on_delete=models.DO_NOTHING)
    position = models.CharField(max_length=16)
    size_x = models.BigIntegerField()
    size_y = models.BigIntegerField()
    size_z = models.BigIntegerField()
    timestamp = models.DateTimeField()
