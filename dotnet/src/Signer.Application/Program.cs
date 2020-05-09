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
using Akka.Configuration.Hocon;

namespace Signer.Application
{
    public static class Program
    {
        private const int CombineBy = 100;
        private const int InputElements = 1000;
        public static async Task Main() => Bench();
        public static void Bench() => BenchmarkRunner.Run<GlobalBench>();
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
