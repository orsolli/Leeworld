from django.db import models



class Player(models.Model):
    id=models.BigAutoField(primary_key=True)
    mesh=models.CharField(max_length=2**16)
    position=models.CharField(max_length=100)


class Block(models.Model):
    mesh=models.CharField(max_length=2**24)
    position=models.CharField(primary_key=True, max_length=100)


class TerraformQueue(models.Model):
    player=models.OneToOneField(Player, primary_key=True, on_delete=models.DO_NOTHING)
    block=models.ForeignKey(Block, on_delete=models.DO_NOTHING)
    position=models.CharField(max_length=16)


class Terraformer(models.Model):
    player=models.OneToOneField(Player, primary_key=True, on_delete=models.DO_NOTHING)
    block=models.ForeignKey(Block, on_delete=models.DO_NOTHING)
    position=models.CharField(max_length=16)
