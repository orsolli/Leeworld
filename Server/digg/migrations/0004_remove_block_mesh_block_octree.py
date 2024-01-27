# Generated by Django 4.1.6 on 2024-01-24 20:58

from django.db import migrations, models

import VoxelEngine


def forwards(apps, schema_editor):
    """
    Convert mesh to octree string.
    """
    Block = apps.get_model("digg", "block")
    for block in Block.objects.all():
        block.octree = VoxelEngine.build_octree_string_fast(
            mesh=VoxelEngine.mutate(mesh=block.mesh, players=[])[1],
        )
        block.save()


def backwards(apps, schema_editor):
    """
    Convert octree string to mesh.
    """
    Block = apps.get_model("digg", "block")
    for block in Block.objects.all():
        block.mesh = VoxelEngine.octree_string_to_mesh(octree=block.octree)
        block.save()


class Migration(migrations.Migration):
    dependencies = [
        ("digg", "0003_alter_block_mesh"),
    ]

    operations = [
        migrations.RunSQL(
            f"DELETE FROM digg_block WHERE length(mesh) < 315 OR (length(mesh) = 315 AND position LIKE '%-%');"
        ),
        migrations.AddField(
            model_name="block",
            name="octree",
            field=models.TextField(default="00"),
        ),
        migrations.RunPython(forwards, backwards),
        migrations.RemoveField(
            model_name="block",
            name="mesh",
        ),
    ]
