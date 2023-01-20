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
        self.player = models.Player.objects.create(mesh=player_mesh)

    def test_can_get_block(self):
        c = Client()
        self.assertEqual(models.Block.objects.count(), 0)
        c.get('/digg/block/?block=0,0,0')
        self.assertEqual(models.Block.objects.count(), 1)
        c.get('/digg/block/?block=1,0,0')
        self.assertEqual(models.Block.objects.count(), 2)
        #c.get('/digg/block/?block=127,0,0')
        #self.assertEqual(models.Block.objects.count(), 2, "Player is not that fast")

    def test_can_digg_block(self):
        c = Client()
        c.get('/digg/block/?block=0,0,0')
        mesh = models.Block.objects.get(position='0,0,0').mesh
        c.put(f'/digg/request/?block=0,0,0&position=0,0,0&player={self.player.id}')
        sleep(2)
        c.put(f'/digg/request/?block=0,0,0&position=0,0,0&player={self.player.id}')
        self.assertNotEqual(mesh, models.Block.objects.get(position='0,0,0').mesh)
