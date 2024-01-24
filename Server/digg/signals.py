import threading
from time import monotonic
from django.dispatch import Signal
from . import models
from boolean_geometry import intersectOctree
from multiprocessing import Process, Queue
from queue import Empty
import signal


DIGG = 0
STOP = 1
PING = 2

terraformer_signal = Signal()
lock = threading.Lock()

processes: dict[str, tuple[Queue, Process, float]] = {}
optimize_processes: dict[str, tuple[Queue, Process, float]] = {}


def shutdown():
    with lock:
        for k, p in {**processes, **optimize_processes}.items():
            try:
                p[0].close()
                p[1].terminate()
                p[1].join(5)
                if p[1].is_alive():
                    print(
                        f"Killing {k} from "
                        + ("processes" if k in processes else "optimize_processes")
                    )
                    p[1].kill()
                else:
                    print(
                        f"Terminated {k} from "
                        + ("processes" if k in processes else "optimize_processes")
                    )
                p[1].close()
            except AttributeError:
                pass


def shutdown_handler(s, frame):
    print(f"Received signal {s}. Terminating digg processes {frame}")
    shutdown()
    signal.default_int_handler(s, frame)


def terraformer_signal_handler(
    action: DIGG | STOP | PING, player_id: str, **kwargs
) -> list[int]:
    player = models.Player.objects.get(id=player_id)
    block_position, position = kwargs.get("block_position"), kwargs.get("position")
    if block_position:
        block, _ = models.Block.objects.get_or_create(
            defaults={"octree": "01" if "-" in block_position else "00"},
            position=block_position,
        )
    if action is DIGG:
        if models.Terraformer.objects.filter(player__id=player_id).exists():
            if (
                block.position not in processes
                or not models.Terraformer.objects.filter(
                    player__id=player_id, block=block, position=position
                ).exists()
            ):
                action = STOP

        elif models.TerraformQueue.objects.filter(player__id=player_id).exists():
            if not models.TerraformQueue.objects.filter(
                player__id=player_id, block=block, position=position
            ).exists():
                action = STOP

        else:
            models.TerraformQueue(player=player, block=block, position=position).save()

    with lock:
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
                    models.TerraformQueue(
                        player=worker.player,
                        block=worker.block,
                        position=worker.position,
                    ).save()
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
                tools.append(
                    (worker.player.level, worker.position, worker.player.builder)
                )
                models.Terraformer(
                    player=worker.player, block=worker.block, position=worker.position
                ).save()
                worker.delete()
            queue = Queue()

            processes[block.position] = (
                queue,
                Process(
                    target=intersectOctree,
                    name=f"block_{block.position}",
                    args=(block.position, block.octree, tools, queue),
                    daemon=True,
                ),
                monotonic(),
            )
            processes[block.position][1].start()

        workers = models.Terraformer.objects.filter(block=block)
        finishers = [p.player.id for p in workers]
        time = processes[block.position][2]
        try:
            process_id, octree = processes[block.position][0].get(block=False)
            if process_id == block.position:
                block.octree = octree
                block.save()
                print(f"{player_id} finished the work for {finishers}")
            else:
                print(
                    f"Wrong process_id. Expected {block.position} but got {process_id}"
                )
            try:
                workers.delete()
                processes[block.position][0].close()
                if processes[block.position][1].is_alive():
                    print("Its alive! Lets terminate")
                    processes[block.position][1].terminate()
                processes[block.position][1].join(1)
                if processes[block.position][1].is_alive():
                    print("Its alive! Lets kill it")
                    processes[block.position][1].kill()
                processes[block.position][1].join()
                print("Lets close")
                processes[block.position][1].close()
                del processes[block.position]
            except Exception as e:
                print(e)
            return True, finishers, monotonic() - time
        except Empty:
            return False, finishers, monotonic() - time
