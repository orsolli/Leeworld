from django.dispatch import Signal
from . import models
from boolean_geometry import intersect
from multiprocessing import Process, Queue
from queue import Empty


DIGG=0
STOP=1

terraformer_signal = Signal()

processes: dict[str, tuple[Queue, Process]] = {}

def terraformer_signal_handler(action: DIGG | STOP, player_id: str, **kwargs):

    player = models.Player.objects.get(id=player_id)
    if action is DIGG:
        block_position, position = kwargs.get('block_position'), kwargs.get('position')
        block = models.Block.objects.get(position=block_position)
        if models.Terraformer.objects.filter(player__id=player_id).exists():
            if block.position not in processes or not models.Terraformer.objects.filter(player__id=player_id, block=block, position=position).exists():
                action = STOP

        elif models.TerraformQueue.objects.filter(player__id=player_id).exists():
            if not models.TerraformQueue.objects.filter(player__id=player_id, block=block, position=position).exists():
                action = STOP

        else:
            models.TerraformQueue(player=player, block=block, position=position).save()

    if action is STOP:
        models.TerraformQueue.objects.filter(player=player).delete()
        if not models.Terraformer.objects.filter(player=player).exists():
            return "Your loss"
        deserter = models.Terraformer.objects.get(player=player)
        block = deserter.block

        for worker in models.Terraformer.objects.filter(block=block):
            if worker.player != player:
                models.TerraformQueue(player=worker.player, block=worker.block, position=worker.position).save()
            worker.delete()

        processes[block.position][1].kill()
        processes[block.position][0].close()
        del processes[block.position]

    if block.position not in processes:
        tools = []
        workers = models.TerraformQueue.objects.filter(block=block)
        if not workers.exists():
            return "Go home"
        for worker in workers:
            tools.append((worker.player.mesh, worker.position))
            models.Terraformer(player=worker.player, block=worker.block, position=worker.position).save()
            worker.delete()
        queue = Queue()
        processes[block.position] = (queue, Process(target=intersect, args=(block.position, block.mesh, tools, queue)))
        processes[block.position][1].start()

    try:
        block_position, mesh = processes[block.position][0].get(block=False)

        block = models.Block.objects.get(position=block_position)
        block.mesh = mesh
        block.save()
        models.Terraformer.objects.filter(block=block).delete()
        processes[block.position][1].join()
        processes[block.position][1].close()
        processes[block.position][0].close()
        del processes[block.position]
        return "Done"
    except Empty:
        return "Waiting"
