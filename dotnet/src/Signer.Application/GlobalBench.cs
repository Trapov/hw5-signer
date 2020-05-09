using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Signer.Application.Impl.Actors.Akka;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Implementation;

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
        public IMaterializer Materializer;
        public IActorRef PipelineActorRef;
        public ManualResetEventSlim ManualResetSlimForAkka;

        [GlobalSetup(Targets = new [] {nameof(AkkaActors), nameof(AkkaStreams) })]
        public void SetupAkkaActors()
        {
            const string akkaConfig = @"
                akka {
                  loglevel = WARNING
                }
            ";
            ActorSystem = ActorSystem.Create("hashing", ConfigurationFactory.ParseString(akkaConfig));
            Materializer = ActorSystem.Materializer();
            ManualResetSlimForAkka = new ManualResetEventSlim();
            PipelineActorRef =  ActorSystem.ActorOf(PipelineActor.Configure(InputElements, CombineBy, ManualResetSlimForAkka), "pipeline");
        }

        [GlobalCleanup(Targets = new [] {nameof(AkkaActors), nameof(AkkaStreams) })]
        public async void CleanUpAkkactors()
        {
            await ActorSystem.Terminate();
            ActorSystem.Dispose();
            ManualResetSlimForAkka.Dispose();
        }

        [Benchmark]
        [BenchmarkCategory(nameof(AkkaStreams))]
        public void AkkaStreams()
        {
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

                if (counter == InputElements / CombineBy)
                    ManualResetSlimForAkka.Set();
            }))
            .Run(Materializer);
            ManualResetSlimForAkka.Wait();
            ManualResetSlimForAkka.Reset();
        }
        
        [Benchmark]
        [BenchmarkCategory(nameof(AkkaActors))]
        public void AkkaActors()
        {
            PipelineActorRef.Tell(Enumerable.Range(0, InputElements).Select(i => i.ToString()));
            ManualResetSlimForAkka.Wait();
            ManualResetSlimForAkka.Reset();
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
