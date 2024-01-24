from time import sleep
from django.test import TestCase, Client
from . import models


class DiggTestCase(TestCase):
    def setUp(self):
        models.User.objects.create_user("Adam", "adam@example.com", "eve")

    def test_can_register(self):
        c = Client()
        c.login(username="Adam", password="eve")
        n_player_before = models.Player.objects.count()
        res = c.post("/register/")
        self.assertNotEqual(models.Player.objects.count(), n_player_before, res.content)
