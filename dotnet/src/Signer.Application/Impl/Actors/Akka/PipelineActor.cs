using Akka.Actor;
using Akka.Event;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Signer.Application.Impl.Actors.Akka
{

    public class PipelineActor : ReceiveActor
    {
        public readonly IActorRef SingleHash;
        public readonly IActorRef MultiHash;
        public readonly IActorRef CombineResults;
        public readonly IActorRef Md5Actor;

        private readonly ILoggingAdapter _log = new DummyLogger();

        private readonly int _combineBy = 10;
        private readonly int _inputElements = 10;
        private int _counter = 0;
        private readonly ManualResetEventSlim _manualResetEventSlim;

        public static Props Configure(int inputElements, int combineBy, ManualResetEventSlim manualResetSlim)
        {
            return Props.Create<PipelineActor>(inputElements, combineBy, manualResetSlim);
        }

        public PipelineActor(int inputElements, int combineBy, ManualResetEventSlim manualResetEventSlim)
        {
            _inputElements = inputElements;
            _combineBy = combineBy;
            _manualResetEventSlim = manualResetEventSlim;

            SingleHash = Context.ActorOf<SingleHashActor>();
            MultiHash = Context.ActorOf<MultiHashActor>();
            CombineResults = Context.ActorOf<CombineResultsActor>();

            Md5Actor = Context.ActorOf<Md5Actor>(nameof(Md5Actor));

            Receive<SingleHashActor.SingleHashResult>(HandleSingleHashResult);
            Receive<MultiHashActor.MultiHashResult>(HandleMultiHashResult);
            Receive<CombineResultsActor.CombineResultsResult>(HandleCombineResultsResult);
            Receive<IEnumerable<string>>(Execute);

            Self.Tell(Enumerable.Range(0, inputElements).Select(i => i.ToString()), Self);
        }

        private void HandleCombineResultsResult(CombineResultsActor.CombineResultsResult obj)
        {
            _counter++;
            _log.Info($"CombineResultsResult -> {obj.Value}");

            if (_counter == (_inputElements / _combineBy))
                _manualResetEventSlim.Set();
        }

        private void Execute(IEnumerable<string> enumerable)
        {
            foreach (var e in enumerable)
                SingleHash.Tell(e);
        }

        private void HandleSingleHashResult(SingleHashActor.SingleHashResult singleHashResult)
        {
            _log.Info($"SingleHashResult -> {singleHashResult.Value}");
            MultiHash.Tell(singleHashResult.Value);
        }

        private void HandleMultiHashResult(MultiHashActor.MultiHashResult singleHashResult)
        {
            _log.Info($"MultiHashResult -> {singleHashResult.Value}");
            CombineResults.Tell(singleHashResult.Value);
        }
    }
}
