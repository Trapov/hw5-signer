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
  Job-WRECFI : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT

Runtime=.NET Core 3.0  

```
|             Method | CombineBy | InputElements |     Mean |    Error |   StdDev |   Median | Completed Work Items | Lock Contentions |      Gen 0 |     Gen 1 |     Gen 2 |   Allocated |
|------------------- |---------- |-------------- |---------:|---------:|---------:|---------:|---------------------:|-----------------:|-----------:|----------:|----------:|------------:|
|             **Actors** |        **10** |            **10** |  **2.176 s** | **0.0088 s** | **0.0082 s** |  **2.176 s** |             **235.0000** |                **-** |  **1000.0000** |         **-** |         **-** |  **2187.69 KB** |
|           Channels |        10 |            10 |  2.177 s | 0.0120 s | 0.0112 s |  2.184 s |             137.0000 |                - |          - |         - |         - |    73.42 KB |
| BlockingCollection |        10 |            10 |  4.350 s | 0.3944 s | 1.1504 s |  4.989 s |             120.0000 |           6.0000 |          - |         - |         - |    92.32 KB |
|             **Actors** |        **10** |           **100** |  **3.620 s** | **0.0254 s** | **0.0225 s** |  **3.622 s** |            **1858.0000** |          **13.0000** |  **1000.0000** |         **-** |         **-** |  **3714.11 KB** |
|           Channels |        10 |           100 |  3.636 s | 0.0228 s | 0.0213 s |  3.638 s |            1292.0000 |           5.0000 |          - |         - |         - |   649.41 KB |
| BlockingCollection |        10 |           100 |  3.732 s | 0.0859 s | 0.2351 s |  3.634 s |            1113.0000 |          57.0000 |          - |         - |         - |   645.06 KB |
|             **Actors** |        **10** |          **1000** | **18.690 s** | **0.1178 s** | **0.1102 s** | **18.687 s** |           **14770.0000** |          **19.0000** | **10000.0000** | **3000.0000** | **1000.0000** | **19057.41 KB** |
|           Channels |        10 |          1000 | 18.737 s | 0.1529 s | 0.1430 s | 18.689 s |           12880.0000 |          68.0000 |  4000.0000 | 2000.0000 |         - |  6391.68 KB |
| BlockingCollection |        10 |          1000 | 19.259 s | 0.3847 s | 0.7944 s | 18.851 s |           11213.0000 |         581.0000 |  4000.0000 | 2000.0000 |         - |  6383.93 KB |
