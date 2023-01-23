from time import sleep
from django.test import TestCase, Client
from . import models



class DiggTestCase(TestCase):

    def test_can_register(self):
        c = Client()
        res = c.post('/player/register/')
        self.assertTrue(models.Player.objects.filter(id=res.content).exists())
