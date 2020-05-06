using Akka.Actor;
using BenchmarkDotNet.Running;
using Orleans;
using Signer.Application.Impl.Actors.Akka;
using Signer.Application.Impl.Actors.Orleans;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signer.Application
{
    public static class Program
    {
        public const int CombineBy = 10;
        public const int InputElements = 1000;

        public static void Main() => AkkaActors();

        public static void Bench() => BenchmarkRunner.Run<GlobalBench>();
        public static void OrleansActors()
        {
            var host = OrleansPipeline.GetHost();
            host.StartAsync().GetAwaiter().GetResult();
            var client = OrleansPipeline.GetClient();
            client.Connect().GetAwaiter().GetResult();
            var stopWatch = new Stopwatch();
            var md5Grain = client.GetGrain<IMd5SignerGrain>("IMd5SignerGrain");
            var tasks = new Task[InputElements];

            stopWatch.Start();

            static async Task SignerFlow(IMd5SignerGrain md5Grain, IClusterClient clusterClient, string input)
            {
                var singleHashGrain = clusterClient.GetGrain<ISingleHashGrain>(input);
                var singleHashResult = await singleHashGrain.SingleHash(md5Grain, input);
                Console.Out.WriteLine($"SingleHash -> {singleHashResult}");
                var multiHashGrain = clusterClient.GetGrain<IMultiHashGrain>(input);
                var resultMulti = await multiHashGrain.MultiHash(singleHashResult);
                Console.Out.WriteLine($"MultiHash -> {resultMulti}");
                var combineGrain = clusterClient.GetGrain<ICombineResultsGrain>("ICombineResultsGrain");
                var combineResult = await combineGrain.CombineResults(resultMulti);

                if (combineResult != null)
                    Console.Out.WriteLine($"CombineResults -> {combineResult}");
            }

            foreach (var (index, input) in Enumerable.Range(0, InputElements).Select((i, p) => (i, p.ToString())))
                tasks[index] = SignerFlow(md5Grain, client, input);

            Task.WaitAll(tasks);
            stopWatch.Stop();
            Console.Out.WriteLine($"\n\n Elapsed {stopWatch.Elapsed}... \n\n");
        }
        public static void AkkaActors()
        {
            var stopWatch = new Stopwatch();

            using var system = ActorSystem.Create("hashing");
            using var manualResetSlim = new ManualResetEventSlim();
            stopWatch.Start();

            var pipeline = system.ActorOf(PipelineActor.Configure(InputElements, CombineBy, manualResetSlim), "pipeline");

            manualResetSlim.Wait();
            stopWatch.Stop();
            Console.Out.WriteLine($"\n\n Elapsed {stopWatch.Elapsed}... \n\n");

        }
        public static void Channels()
        {
            var stopWatch = new Stopwatch();
            using var manualSlim = new ManualResetEventSlim();
            stopWatch.Start();
            var counter = 0;

            Pipeline.Execute(
                input: Enumerable.Range(0, InputElements).Select(p => p.ToString()),

                new SingleHashChannel(),
                new OnEachChannel((element) =>
                {
                    Console.Out.WriteLine($"SingleHashed -> [{element}]");
                }),
                new MultiHashChannel(6),
                new OnEachChannel((element) =>
                {
                    Console.Out.WriteLine($"MultiHashed (6x) -> [{element[0..10]}...{element[^10..^1]}]");
                }),
                new CombineResultsChannel(CombineBy),

                new OnEachChannel((element) =>
                {
                    counter++;
                    Console.Out.WriteLine($"Combined by ({CombineBy}) of total ({InputElements}) -> [{element[0..10]}...{element[^10..^1]}]");
                    if (counter == InputElements / CombineBy)
                    {
                        stopWatch.Stop();
                        Console.Out.WriteLine($"\n\n Elapsed {stopWatch.Elapsed}... \n\n");
                        manualSlim.Set();
                    }
                })
            );

            manualSlim.Wait();
        }
        public static void Pipes()
        {
            var stopWatch = new Stopwatch();
            using var manualSlim = new ManualResetEventSlim();
            stopWatch.Start();
            var counter = 0;

            Pipeline.Execute(
                input: Enumerable.Range(0, InputElements).Select(p => p.ToString()),

                new SingleHash(),
                new OnEach((element) =>
                {
                    Console.Out.WriteLine($"SingleHashed -> [{element}]");
                }),
                new MultiHash(6),
                new OnEach((element) =>
                {
                    Console.Out.WriteLine($"MultiHashed (6x) -> [{element[0..10]}...{element[^10..^1]}]");
                }),
                new CombineResults(CombineBy),

                new OnEach((element) =>
                {
                    counter++;
                    Console.Out.WriteLine($"Combined by ({CombineBy}) of total ({InputElements}) -> [{element[0..10]}...{element[^10..^1]}]");
                    if (counter == InputElements / CombineBy)
                    {
                        stopWatch.Stop();
                        Console.Out.WriteLine($"\n\n Elapsed {stopWatch.Elapsed}... \n\n");
                        manualSlim.Set();
                    }
                })
            );

            manualSlim.Wait();
        }
    }
}
