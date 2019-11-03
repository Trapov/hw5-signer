# Concurrently hash-out inputs in a pipelining manner.

## Simple combine these three functions in a pipelining manner and push inputs into the first one.

```python

SingleHash(input) => crc32(input) + '~' + crc32(md5(input))

MultiHash(singleHashResult) => 
	concat(for i in 6 do crc32(i + singleHashResult))

CombineResults(multiHashResult) =>
	if stored.count == 6 then join(sort(stored)) else store(multiHashResult)

CombineResults(MultiHash(SingleHash(1,2,3,4,5)))
```

# Important

## You can't call `md5` concurently, if you do, then it will be overheated for **1** second.

# .NET Core 3.0
``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i5-7200U CPU 2.50GHz (Kaby Lake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  Job-WRJOXS : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT

Runtime=.NET Core 3.0  

```
|   Method | CombineBy | InputElements |     Mean |    Error |   StdDev |   Median | Completed Work Items | Lock Contentions |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
|--------- |---------- |-------------- |---------:|---------:|---------:|---------:|---------------------:|-----------------:|----------:|----------:|------:|-----------:|
| **Channels** |        **10** |            **10** |  **2.177 s** | **0.0130 s** | **0.0122 s** |  **2.184 s** |             **140.0000** |                **-** |         **-** |         **-** |     **-** |   **73.56 KB** |
|    Pipes |        10 |            10 |  4.421 s | 0.3772 s | 1.1123 s |  4.987 s |             115.0000 |                - |         - |         - |     - |   80.93 KB |
| **Channels** |        **10** |           **100** |  **3.613 s** | **0.0254 s** | **0.0237 s** |  **3.606 s** |            **1276.0000** |           **1.0000** |         **-** |         **-** |     **-** |  **649.49 KB** |
|    Pipes |        10 |           100 |  3.675 s | 0.0816 s | 0.1774 s |  3.624 s |            1140.0000 |          30.0000 |         - |         - |     - |  650.13 KB |
| **Channels** |        **10** |          **1000** | **18.755 s** | **0.0984 s** | **0.0920 s** | **18.747 s** |           **12975.0000** |          **87.0000** | **4000.0000** | **2000.0000** |     **-** | **6392.23 KB** |
|    Pipes |        10 |          1000 | 19.243 s | 0.3757 s | 0.5388 s | 19.034 s |           11142.0000 |         401.0000 | 4000.0000 | 2000.0000 |     - | 6382.59 KB |
