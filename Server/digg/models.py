from django.db import models
from player.models import *

default_block_mesh = """v 0 0 0
v 0 0 8
v 0 8 0
v 0 8 8
v 8 0 0
v 8 0 8
v 8 8 0
v 8 8 8

vn 1 0 0
vn -1 0 0
vn 0 1 0
vn 0 -1 0
vn 0 0 1
vn 0 0 -1

f 1//2 2 3//2
f 2 4//2 3//2
f 2//5 6//5 4//5
f 4//5 6//5 8//5
f 1//4 5//4 2//4
f 2//4 5//4 6//4
f 3 8//3 7//3
f 3 4//3 8//3
f 1//6 3//6 7//6
f 1//6 7//6 5//6
f 5//1 8//1 6//1
f 5//1 7//1 8//1
"""


class Block(models.Model):
    octree = models.TextField(default="00")
    position = models.CharField(primary_key=True, max_length=100)


class TerraformQueue(models.Model):
    player = models.OneToOneField(Player, primary_key=True, on_delete=models.DO_NOTHING)
    block = models.ForeignKey(Block, on_delete=models.DO_NOTHING)
    position = models.CharField(max_length=16)


class Terraformer(models.Model):
    player = models.OneToOneField(Player, primary_key=True, on_delete=models.DO_NOTHING)
    block = models.ForeignKey(Block, on_delete=models.DO_NOTHING)
    position = models.CharField(max_length=16)
