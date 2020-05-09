using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Signer.Application.Impl.Actors.Akka;
using System.Linq;
using System.Threading;
using Akka.Configuration;

namespace Signer.Application
{
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [HtmlExporter]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn()]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp31)]
    public class GlobalBench
    {
        [Params(10)]
        public int CombineBy;

        [Params(100, 1000, 10_000)]
        public int InputElements;

        public ActorSystem ActorSystem;
        public IActorRef PipelineActorRef;
        public ManualResetEventSlim ManualResetSlimForAkkaActors;

        [IterationSetup(Target = nameof(AkkaActors))]
        public void SetupAkkaActors()
        {
            const string akkaConfig = @"
                akka {
                  loglevel = INFO
                }
            ";
            ActorSystem = ActorSystem.Create("hashing", ConfigurationFactory.ParseString(akkaConfig));
            ManualResetSlimForAkkaActors = new ManualResetEventSlim();
            PipelineActorRef =  ActorSystem.ActorOf(PipelineActor.Configure(InputElements, CombineBy, ManualResetSlimForAkkaActors), "pipeline");
        }

        [IterationCleanup(Target = nameof(AkkaActors))]
        public async void CleanUpAkkactors()
        {
            await ActorSystem.Terminate();
            ActorSystem.Dispose();
            ManualResetSlimForAkkaActors.Dispose();
        }

        [Benchmark]
        [BenchmarkCategory(nameof(AkkaActors))]
        public void AkkaActors()
        {
            PipelineActorRef.Tell(Enumerable.Range(0, InputElements).Select(i => i.ToString()));
            ManualResetSlimForAkkaActors.Wait();
            ManualResetSlimForAkkaActors.Reset();
        }

        [Benchmark]
        public void Channels()
        {
            using var manualSlim = new ManualResetEventSlim();
            var counter = 0;

            Pipeline.Execute(
                input: Enumerable.Range(0, InputElements).Select(p => p.ToString()),

                new SingleHashChannel(),
                new MultiHashChannel(6),
                new CombineResultsChannel(CombineBy),
                new OnEachChannel((element) =>
                {
                    counter++;
                    if (counter == InputElements / CombineBy)
                        manualSlim.Set();
                })
            );

            manualSlim.Wait();
        }

        [Benchmark]
        public void BlockingCollection()
        {
            using var manualSlim = new ManualResetEventSlim();
            var counter = 0;

            Pipeline.Execute(
                input: Enumerable.Range(0, InputElements).Select(p => p.ToString()),

                new SingleHash(),
                new MultiHash(6),
                new CombineResults(CombineBy),
                new OnEach((element) =>
                {
                    counter++;
                    if (counter == InputElements / CombineBy)
                        manualSlim.Set();
                })
            );

            manualSlim.Wait();
        }
    }
}
