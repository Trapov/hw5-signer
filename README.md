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

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.778 (1909/November2018Update/19H2)
Intel Core i5-8500 CPU 3.00GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=3.1.201
  [Host]        : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT
  .NET Core 3.1 : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Job=.NET Core 3.1  Runtime=.NET Core 3.1  

```
|             Method | CombineBy | InputElements |      Mean |    Error |   StdDev | Rank |      Gen 0 |      Gen 1 |     Gen 2 |    Allocated | Completed Work Items | Lock Contentions |
|------------------- |---------- |-------------- |----------:|---------:|---------:|-----:|-----------:|-----------:|----------:|-------------:|---------------------:|-----------------:|
| BlockingCollection |        10 |           100 |   3.628 s | 0.0470 s | 0.0392 s |    1 |          - |          - |         - |    652.99 KB |            1126.0000 |          20.0000 |
|        AkkaStreams |        10 |           100 |   3.628 s | 0.0327 s | 0.0306 s |    1 |          - |          - |         - |    905.11 KB |            1112.0000 |           3.0000 |
|         AkkaActors |        10 |           100 |   3.629 s | 0.0314 s | 0.0294 s |    1 |          - |          - |         - |   2549.38 KB |            2043.0000 |           2.0000 |
|           Channels |        10 |           100 |   3.632 s | 0.0222 s | 0.0208 s |    1 |          - |          - |         - |    652.51 KB |            1306.0000 |                - |
|        AkkaStreams |        10 |          1000 |  18.660 s | 0.1267 s | 0.1185 s |    2 |  1000.0000 |          - |         - |   7276.14 KB |           10441.0000 |          23.0000 |
|           Channels |        10 |          1000 |  18.724 s | 0.0835 s | 0.0781 s |    2 |  1000.0000 |          - |         - |   6422.95 KB |           13025.0000 |          27.0000 |
|         AkkaActors |        10 |          1000 |  18.734 s | 0.0945 s | 0.0884 s |    2 |  5000.0000 |  2000.0000 |         - |  25934.71 KB |           20716.0000 |          17.0000 |
| BlockingCollection |        10 |          1000 |  19.468 s | 0.3881 s | 0.9373 s |    2 |  1000.0000 |          - |         - |    6380.8 KB |           11262.0000 |         240.0000 |
|        AkkaStreams |        10 |         10000 | 168.493 s | 0.4258 s | 0.3982 s |    3 | 15000.0000 |  1000.0000 |         - |  71006.49 KB |          103266.0000 |         107.0000 |
|           Channels |        10 |         10000 | 168.584 s | 0.4002 s | 0.3548 s |    3 | 14000.0000 |  2000.0000 | 1000.0000 |  64122.73 KB |          130712.0000 |          59.0000 |
|         AkkaActors |        10 |         10000 | 169.872 s | 0.2802 s | 0.2621 s |    3 | 48000.0000 | 10000.0000 |         - | 263577.29 KB |          206095.0000 |         239.0000 |
| BlockingCollection |        10 |         10000 | 174.376 s | 2.8485 s | 2.5252 s |    4 | 14000.0000 |  2000.0000 |         - |  63698.44 KB |          112114.0000 |        1984.0000 |
