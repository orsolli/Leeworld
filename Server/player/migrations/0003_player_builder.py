# Generated by Django 4.1.6 on 2023-02-11 14:05

from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ('player', '0002_player_user'),
    ]

    operations = [
        migrations.AddField(
            model_name='player',
            name='builder',
            field=models.BooleanField(default=False),
        ),
    ]
