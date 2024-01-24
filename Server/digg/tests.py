from time import sleep
from django.test import TestCase, Client
from . import models


class DiggTestCase(TestCase):
    def setUp(self) -> None:
        self.user = models.User.objects.create()
        self.player = models.Player.objects.create(user=self.user, level=3)

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
        octree = models.Block.objects.get(position="-1,0,0").octree
        c.put(f"/digg/request/?block=-1_0_0&position=0_0_0&player={self.player.id}")
        sleep(2)
        c.put(f"/digg/request/?block=-1_0_0&position=0_0_0&player={self.player.id}")
        self.assertNotEqual(octree, models.Block.objects.get(position="-1,0,0").octree)

    def test_can_stop_digg(self):
        c = Client()
        c.get("/digg/block/?block=-2_0_0")
        octree = models.Block.objects.get(position="-2,0,0").octree
        c.put(f"/digg/request/?block=-2_0_0&position=0_0_0&player={self.player.id}")
        c.delete(f"/digg/request/?player={self.player.id}")
        sleep(2)
        self.assertEqual(octree, models.Block.objects.get(position="-2,0,0").octree)
