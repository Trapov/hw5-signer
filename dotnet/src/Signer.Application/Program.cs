using Akka.Actor;
using BenchmarkDotNet.Running;
using Signer.Application.Impl.Actors.Akka;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace Signer.Application
{
    public static class Program
    {
        private const int CombineBy = 10;
        private const int InputElements = 1000;
        public static async Task Main() => Bench();
        public static void Bench() => BenchmarkRunner.Run<GlobalBench>();

        public static async Task AkkaStreams()
        {
            var stopWatch = new Stopwatch();
            const string akkaConfig = @"
                akka {
                  loggers = [""Akka.Event.DefaultLogger""]
                }
            ";
            using var system = ActorSystem.Create("hashing", ConfigurationFactory.ParseString(akkaConfig));
            using var materializer = system.Materializer();
            using var manualResetSlim = new ManualResetEventSlim();

            stopWatch.Start();
            var counter = 0;

            var singleHash = Flow.FromGraph(GraphDsl.Create(builder =>
            {
                var broadCast = builder.Add(new Broadcast<string>(2));
                var zip = builder.Add(new Zip<string, string>());
                var left = Flow.Create<string>().SelectAsync(InputElements, async i => await Signers.DataSignerCrc32(i));
                var right = Flow.Create<string>()
                    .SelectAsync(1, Signers.DataSignerMd5)
                    .SelectAsync(InputElements, Signers.DataSignerCrc32);

                builder.From(broadCast.Out(0)).Via(left).To(zip.In0);
                builder.From(broadCast.Out(1)).Via(right).To(zip.In1);

                return new FlowShape<string, (string, string)>(broadCast.In, zip.Out);
            }));

            var mergeSingleHash = Flow.Create<(string, string)>()
                    .Select(tuple => string.Join("~", tuple.Item1, tuple.Item2));

            var multiHash = Flow.Create<string>()
                .SelectAsync(InputElements,
                    async input => await Task.WhenAll(Enumerable.Range(0, 6).Select(i => Signers.DataSignerCrc32(i.ToString() + input))))
                .Select(string.Concat);

            var combine =
                Flow.Create<string>()
                .Grouped(CombineBy)
                .Select(i => string.Join("_", i));

            Source.From(Enumerable.Range(0, InputElements))
                .Select(i => i.ToString())
                .Via(singleHash)
                .Via(mergeSingleHash)
                .Via(multiHash)
                .Via(combine)
            .To(Sink.ForEach<string>(i => 
            {
                counter++;

                Console.WriteLine(i);

                if (counter == InputElements / CombineBy)
                    manualResetSlim.Set();
            }))
            .Run(materializer);


            manualResetSlim.Wait();
            stopWatch.Stop();
            Console.Out.WriteLine($"\n\n Elapsed {stopWatch.Elapsed}... \n\n");
        }

        public static void AkkaActors()
        {
            var stopWatch = new Stopwatch();
            const string akkaConfig = @"
akka {
  loggers = [""Akka.Event.DefaultLogger""]
}
";
            using var system = ActorSystem.Create("hashing", ConfigurationFactory.ParseString(akkaConfig));
            using var manualResetSlim = new ManualResetEventSlim();
            stopWatch.Start();

            var pipeline = system.ActorOf(PipelineActor.Configure(InputElements, CombineBy, manualResetSlim), "pipeline");
            pipeline.Tell(Enumerable.Range(0, InputElements).Select(i => i.ToString()));
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
