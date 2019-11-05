using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Akka.Actor;
using Signer.Application.Impl.Actors.Akka;

namespace Signer.Application
{
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [CoreJob]
    public class GlobalBench
    {
        [Params(10)]
        public int CombineBy;

        [Params(10, 100, 1000)]
        public int InputElements;

        [Benchmark]
        public void Actors()
        {
            
            var system = ActorSystem.Create("hashing");
            var manualResetSlim = new ManualResetEventSlim();
            var pipeline = system.ActorOf(PipelineActor.Configure(InputElements, CombineBy, manualResetSlim), "pipeline");

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
