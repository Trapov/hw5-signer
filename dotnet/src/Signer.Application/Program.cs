﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using BenchmarkDotNet.Running;
using Akka.Actor;
using Signer.Application.Impl.Actors.Akka;

namespace Signer.Application
{
    public static class Program
    {
        public const int CombineBy = 1000;
        public const int InputElements = 1000;

        public static void Main() => Channels();

        public static void Actors()
        {
            using var system = ActorSystem.Create("hashing");
            using var manualResetSlim = new ManualResetEventSlim();
            var pipeline = system.ActorOf(PipelineActor.Configure(1000, 10, manualResetSlim), "pipeline");

            manualResetSlim.Wait();
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
