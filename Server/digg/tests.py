from time import sleep
from django.test import TestCase, Client
from . import models


player_mesh = """v 0 0 0
v 0 0 1
v 0 1 0
v 0 1 1
v 1 0 0
v 1 0 1
v 1 1 0
v 1 1 1

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


class DiggTestCase(TestCase):
    def setUp(self) -> None:
        self.user = models.User.objects.create()
        self.player = models.Player.objects.create(user=self.user, mesh=player_mesh)

    def test_can_get_block(self):
        c = Client()
        self.assertEqual(models.Block.objects.count(), 0)
        c.get("/digg/block/?block=0_0_0")
        self.assertEqual(models.Block.objects.count(), 1)
        c.get("/digg/block/?block=1_0_0")
        self.assertEqual(models.Block.objects.count(), 2)
        c.get("/digg/block/?block=1_0_0")
        self.assertEqual(models.Block.objects.count(), 2)

    def test_can_digg_block(self):
        c = Client()
        c.get("/digg/block/?block=-1_0_0")
        mesh = models.Block.objects.get(position="-1,0,0").mesh
        c.put(f"/digg/request/?block=-1_0_0&position=0_0_0&player={self.player.id}")
        sleep(2)
        c.put(f"/digg/request/?block=-1_0_0&position=0_0_0&player={self.player.id}")
        self.assertNotEqual(mesh, models.Block.objects.get(position="-1,0,0").mesh)

    def test_can_stop_digg(self):
        c = Client()
        c.get("/digg/block/?block=-2_0_0")
        mesh = models.Block.objects.get(position="-2,0,0").mesh
        c.put(f"/digg/request/?block=-2_0_0&position=0_0_0&player={self.player.id}")
        c.delete(f"/digg/request/?player={self.player.id}")
        sleep(2)
        self.assertEqual(mesh, models.Block.objects.get(position="-2,0,0").mesh)
