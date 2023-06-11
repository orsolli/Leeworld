from time import monotonic
from django.dispatch import Signal
from . import models
from boolean_geometry import intersect
from multiprocessing import Process, Queue
from queue import Empty
import signal


DIGG=0
STOP=1
PING=2

terraformer_signal = Signal()

processes: dict[str, tuple[Queue, Process, float]] = {}
optimize_processes: dict[str, tuple[Queue, Process, float]] = {}

def shutdown(s, frame):
    print(f"Received signal {s}. Terminating digg processes {frame}")
    for k, p in {**processes, **optimize_processes}.items():
        p[0].close()
        p[1].terminate()
        p[1].join()
        if p[1].is_alive():
            print(f"Killing {k} from " + ('processes' if k in processes else 'optimize_processes'))
            p[1].kill()
        else:
            print(f"Terminated {k} from " + ('processes' if k in processes else 'optimize_processes'))
        p[1].close()
    signal.default_int_handler(s, frame)

def terraformer_signal_handler(action: DIGG | STOP | PING, player_id: str, **kwargs) -> list[int]:

    player = models.Player.objects.get(id=player_id)
    block_position, position = kwargs.get('block_position'), kwargs.get('position')
    if block_position:
        block, _ = models.Block.objects.get_or_create(defaults={'mesh':models.default_block_mesh if '-' in block_position else ''}, position=block_position)
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

        if block.position in processes:
            processes[block.position][1].terminate()
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

        if block.position in optimize_processes:
            try:
                process_id, mesh = optimize_processes[block.position][0].get(block=False)
                optimize_processes[block.position][1].join()
            except Empty:
                process_id = None
            optimize_processes[block.position][0].close()

            if process_id == f'optimize({block.position})':
                if (len(block.mesh) > len(mesh)):
                    print(f"{block.position} compressed {len(block.mesh)=} > {len(mesh)=}")
                    block.mesh = mesh
                    block.save()
                else:
                    print(f"{len(block.mesh)=} < {len(mesh)=}")
            elif process_id is not None:
                print(f"Wrong process_id. Expected optimized({block.position}) but got {process_id}")
            if optimize_processes[block.position][1].is_alive():
                print("Its alive! Lets terminate")
                optimize_processes[block.position][1].terminate()
            print("Lets join")
            optimize_processes[block.position][1].join()
            if optimize_processes[block.position][1].is_alive():
                print("Its alive! Lets kill it")
                optimize_processes[block.position][1].kill()
            print("Lets close")
            optimize_processes[block.position][1].close()
            del optimize_processes[block.position]

        processes[block.position] = (queue, Process(target=intersect, args=(block.position, block.mesh, tools, queue)), monotonic())
        processes[block.position][1].start()

    workers = models.Terraformer.objects.filter(block=block)
    finishers = [p.player.id for p in workers]
    time = processes[block.position][2]
    try:
        process_id, mesh = processes[block.position][0].get(block=False)
        if process_id == block.position:
            block.mesh = mesh
            block.save()
            print(f"{player_id} finished the work for {finishers}")
        else:
            print(f"Wrong process_id. Expected {block.position} but got {process_id}")
        workers.delete()
        optimize_processes[block.position] = processes[block.position]
        del processes[block.position]
        return True, finishers, monotonic() - time
    except Empty:
        return False, finishers, monotonic() - time
