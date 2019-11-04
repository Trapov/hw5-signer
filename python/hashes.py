import hashlib, zlib
import asyncio

from asyncio import Future

from helpers import AtomicInteger

md5_counter : AtomicInteger = AtomicInteger(0)

async def overheating_lock():
    while md5_counter.value == 1:
        print("Over-heated for 1 second... locked")
        await asyncio.sleep(1)
    else:
        md5_counter.inc()

async def overheating_unlock():
    while md5_counter.value == 0:
        print("Over-heated for 1 second...unlocked")
        await asyncio.sleep(1)
    else:
        md5_counter.dec()

async def md5(value : str) -> 'Future[str]':
    await overheating_lock()
    hash_result = hashlib.md5(value.encode('utf-8')).hexdigest()
    await asyncio.sleep(1/100)
    
    await overheating_unlock()
    return hash_result

async def crc32(value : str) -> 'Future[str]':
    await asyncio.sleep(1)
    return zlib.crc32(value.encode('utf-8'))

if __name__ == "__main__":
    async def main():    
        print(
            await md5("aa"),
            await crc32("aa")
        )
    async def concurent_call_to_md5(): 
        await asyncio.gather(md5("aa"), md5("55"), md5("59"))

    asyncio.run(concurent_call_to_md5())