using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Orleans;
using Orleans.Hosting;
using Signer.Application.Impl.Actors.Akka;
using Signer.Application.Impl.Actors.Orleans;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signer.Application
{
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(BenchmarkDotNet.Mathematics.NumeralSystem.Stars)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp31)]
    public class GlobalBench
    {
        [Params(100)]
        public int CombineBy;

        [Params(10, 100, 1000, 10_000)]
        public int InputElements;

        public ActorSystem ActorSystem;
        public ISiloHost SiloHost;
        public IClusterClient ClusterClient;
        public IMd5SignerGrain Md5Grain;


        [IterationSetup(Target = "AkkaActors")]
        public void SetupAkkaActors()
        {
            ActorSystem = ActorSystem.Create("hashing");
        }

        [IterationCleanup(Target = "AkkaActors")]
        public void CleanUpAkkaActors()
        {
            ActorSystem.Dispose();
        }

        [GlobalSetup(Target = "OrleansActors")]
        public void SetUpOrleansActors()
        {

            SiloHost = OrleansPipeline.GetHost();
            SiloHost.StartAsync().GetAwaiter().GetResult();
            ClusterClient = OrleansPipeline.GetClient();
            ClusterClient.Connect().GetAwaiter().GetResult();
            Md5Grain = ClusterClient.GetGrain<IMd5SignerGrain>("IMd5SignerGrain");
        }

        [GlobalCleanup(Target = "OrleansActors")]
        public void CleanUpOrleansActors()
        {
            ClusterClient.Dispose();
            SiloHost.Dispose();
        }

        [Benchmark]
        public void OrleansActors()
        {
            var tasks = new Task[InputElements];

            static async Task SignerFlow(IMd5SignerGrain md5Grain, IClusterClient clusterClient, string input)
            {
                var singleHashGrain = clusterClient.GetGrain<ISingleHashGrain>(input);
                var singleHashResult = await singleHashGrain.SingleHash(md5Grain, input);
                var multiHashGrain = clusterClient.GetGrain<IMultiHashGrain>(input);
                var resultMulti = await multiHashGrain.MultiHash(singleHashResult);
                var combineGrain = clusterClient.GetGrain<ICombineResultsGrain>("ICombineResultsGrain");
                var combineResult = await combineGrain.CombineResults(resultMulti);
            }

            foreach (var (index, input) in Enumerable.Range(0, InputElements).Select((i, p) => (i, p.ToString())))
                tasks[index] = SignerFlow(Md5Grain, ClusterClient, input);

            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void AkkaActors()
        {
            var manualResetSlim = new ManualResetEventSlim();
            var _ = ActorSystem.ActorOf(PipelineActor.Configure(InputElements, CombineBy, manualResetSlim), "pipeline");
            manualResetSlim.Wait();
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
