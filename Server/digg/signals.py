from time import monotonic
from django.dispatch import Signal
from . import models
from boolean_geometry import intersect
from multiprocessing import Process, Queue
from queue import Empty


DIGG=0
STOP=1
PING=2

terraformer_signal = Signal()

processes: dict[str, tuple[Queue, Process], float] = {}

def terraformer_signal_handler(action: DIGG | STOP | PING, player_id: str, **kwargs) -> list[int]:

    player = models.Player.objects.get(id=player_id)
    block_position, position = kwargs.get('block_position'), kwargs.get('position')
    if block_position:
        block = models.Block.objects.get(position=block_position)
    if action is DIGG:
        if models.Terraformer.objects.filter(player__id=player_id).exists():
            if block.position not in processes or not models.Terraformer.objects.filter(player__id=player_id, block=block, position=position).exists():
                action = STOP

        elif models.TerraformQueue.objects.filter(player__id=player_id).exists():
            if not models.TerraformQueue.objects.filter(player__id=player_id, block=block, position=position).exists():
                action = STOP

        else:
            models.TerraformQueue(player=player, block=block, position=position).save()

    if action is STOP:
        queued, _ = models.TerraformQueue.objects.filter(player=player).delete()
        if not models.Terraformer.objects.filter(player=player).exists():
            if queued:
                print(f"{player_id} gave up the queue")
            return False, [], 5
        deserter = models.Terraformer.objects.get(player=player)
        block = deserter.block

        workers = models.Terraformer.objects.filter(block=block)
        trofast = f"{[p.player.id for p in workers]}"
        for worker in workers:
            if worker.player != player:
                models.TerraformQueue(player=worker.player, block=worker.block, position=worker.position).save()
            else:
                print(f"{player_id} ruined it for {trofast}")
            worker.delete()

        processes[block.position][1].kill()
        processes[block.position][0].close()
        del processes[block.position]

    if block.position not in processes:
        tools = []
        workers = models.TerraformQueue.objects.filter(block=block)
        if not workers.exists():
            return False, [player.id], 5
        print(f"Go! {[p.player.id for p in workers]}")
        for worker in workers:
            tools.append((worker.player.mesh, worker.position, worker.player.builder))
            models.Terraformer(player=worker.player, block=worker.block, position=worker.position).save()
            worker.delete()
        queue = Queue()
        processes[block.position] = (queue, Process(target=intersect, args=(block.position, block.mesh, tools, queue)), monotonic())
        processes[block.position][1].start()

    workers = models.Terraformer.objects.filter(block=block)
    finishers = [p.player.id for p in workers]
    time = processes[block.position][2]
    try:
        block_position, mesh = processes[block.position][0].get(block=False)

        block = models.Block.objects.get(position=block_position)
        block.mesh = mesh
        block.save()
        print(f"{player_id} finished the work for {finishers}")
        workers.delete()
        processes[block.position][1].join()
        processes[block.position][1].close()
        processes[block.position][0].close()
        del processes[block.position]
        return True, finishers, monotonic() - time
    except Empty:
        return False, finishers, monotonic() - time
