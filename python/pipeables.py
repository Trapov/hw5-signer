from hashes import md5, crc32
from typing import Callable
import asyncio, asyncio.queues

from concurrent.futures.thread import ThreadPoolExecutor
from asyncio import AbstractEventLoop
import time

COMBINE_BY = 1000
INPUT_ELEMENTS = 1000 
global counter
counter = 0

async def __single_hash_worker_continuation(el :str, md5_result : str, out_stream : asyncio.queues.Queue):
        start_time = time.time()
        md5_and_crc32_result, crc32_result = await asyncio.gather(
            crc32(md5_result),
            crc32(el)
        )
        result = str(crc32_result) + '~' + str(md5_and_crc32_result)
        await out_stream.put(result)
        print(f"SingleHashed => [{result}]. Elapsed [{time.time()-start_time}]")

async def __single_hash_worker(in_stream : asyncio.queues.Queue, out_stream : asyncio.queues.Queue):
    while True:
        el = await in_stream.get()
        md5_result = await md5(el)

        asyncio.create_task(__single_hash_worker_continuation(el, md5_result, out_stream))

async def __multi_hash_continuatuin(el : str, out_stream : asyncio.queues.Queue):
        start_time = time.time()
        results = await asyncio.gather(
            crc32(str(0)+str(el)),
            crc32(str(1)+str(el)),
            crc32(str(2)+str(el)),
            crc32(str(3)+str(el)),
            crc32(str(4)+str(el)),
            crc32(str(5)+str(el)),
        )
        result = ''.join(map(str, results))
        await out_stream.put(result)
        print(f"MultiHashed => [{result[:10]+'...'+result[-10:]}] . Elapsed [{time.time() - start_time}] seconds")


async def __multi_hash_worker(in_stream : asyncio.queues.Queue, out_stream : asyncio.queues.Queue):
    while True:
        el = await in_stream.get()
        asyncio.create_task(__multi_hash_continuatuin(el, out_stream))

async def __combine_results_worker(in_stream : asyncio.queues.Queue, out_stream : asyncio.queues.Queue):
    global counter
    stored = []
    while True:
        el = await in_stream.get()

        stored.append(el)

        if len(stored) == COMBINE_BY:
            result = '_'.join(map(str, sorted(stored)))
            out_stream.put_nowait(result)
            counter = counter + 1
            print(f"CombineResults by ({COMBINE_BY}) of ({INPUT_ELEMENTS}) => [{result[:10]+'...'+result[-10:]}]")
            stored = []     
        
async def __on_each_worker(callback : Callable[[str], None], in_stream : asyncio.queues.Queue, out_stream : asyncio.queues.Queue):
    while True:
        el = await in_stream.get()
        await out_stream.put(el)
        callback(el)     

def SingleHash(in_stream : asyncio.queues.Queue) -> asyncio.queues.Queue:
    out_stream = asyncio.queues.Queue()
    asyncio.create_task(__single_hash_worker(in_stream, out_stream))
    return out_stream

def MultiHash(in_stream : asyncio.queues.Queue) -> asyncio.queues.Queue:
    out_stream = asyncio.queues.Queue()
    asyncio.create_task(__multi_hash_worker(in_stream, out_stream))
    return out_stream


def CombineResults(in_stream : asyncio.queues.Queue) -> asyncio.queues.Queue:
    out_stream = asyncio.queues.Queue()
    asyncio.create_task(__combine_results_worker(in_stream, out_stream))
    return out_stream

def OnEach(callback : Callable[[str], None], in_stream : asyncio.queues.Queue) -> asyncio.queues.Queue:
    out_stream = asyncio.queues.Queue()
    asyncio.create_task(__on_each_worker(callback, in_stream, out_stream))
    return out_stream

if __name__ == "__main__":

    async def main():

        start_time = time.time()
        queue = asyncio.queues.Queue()
        loop : AbstractEventLoop = asyncio.get_running_loop()

        CombineResults(
            MultiHash(
                SingleHash(queue)
            )
        )

        await asyncio.gather(*[queue.put(str(i)) for i in range(INPUT_ELEMENTS)])

        while counter != INPUT_ELEMENTS/COMBINE_BY:
            await asyncio.sleep(0.001)

        print(f'Done hashing [{INPUT_ELEMENTS}] elements. Elapsed {time.time()-start_time} seconds.')

    asyncio.run(main())    