using Akka.Actor;
using Akka.Event;
using System.Collections.Generic;
using System.Threading;
using Akka.Routing;

namespace Signer.Application.Impl.Actors.Akka
{
    public sealed class PipelineActor : ReceiveActor
    {
        private readonly IActorRef _singleHashActor;
        private readonly IActorRef _multiHashActor;
        private readonly IActorRef _combineResultsActor;
        public readonly IActorRef Md5Actor;

        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly int _combineBy;
        private readonly int _inputElements;
        private int _counter = 0;
        private readonly ManualResetEventSlim _manualResetEventSlim;

        public static Props Configure(int inputElements, int combineBy, ManualResetEventSlim manualResetSlim) => 
            Props.Create<PipelineActor>(inputElements, combineBy, manualResetSlim);

        public PipelineActor(int inputElements, int combineBy, ManualResetEventSlim manualResetEventSlim)
        {
            _inputElements = inputElements;
            _combineBy = combineBy;
            _manualResetEventSlim = manualResetEventSlim;
            Md5Actor = Context.ActorOf<Md5Actor>(nameof(Md5Actor));

            _singleHashActor = Context.ActorOf(
                    Props.Create<SingleHashActor>(Md5Actor, Self)
                        .WithRouter(new RoundRobinPool(inputElements))
                );
            _multiHashActor = Context.ActorOf<MultiHashActor>();
            _combineResultsActor = Context.ActorOf<CombineResultsActor>();

            Receive<SingleHashActor.SingleHashResult>(HandleSingleHashResult);
            Receive<MultiHashActor.MultiHashResult>(HandleMultiHashResult);
            Receive<CombineResultsActor.CombineResultsResult>(HandleCombineResultsResult);
            Receive<IEnumerable<string>>(Execute);
        }

        private void HandleCombineResultsResult(CombineResultsActor.CombineResultsResult obj)
        {
            _counter++;
            _log.Info($"CombineResultsResult -> {obj.Value}");

            if (_counter != (_inputElements / _combineBy)) return;
            
            _counter = 0;
            _manualResetEventSlim.Set();
        }

        private void Execute(IEnumerable<string> enumerable)
        {
            foreach (var e in enumerable)
                _singleHashActor.Tell(e);
        }

        private void HandleSingleHashResult(SingleHashActor.SingleHashResult singleHashResult)
        {
            _log.Info($"SingleHashResult -> {singleHashResult.Value}");
            _multiHashActor.Tell(singleHashResult.Value);
        }

        private void HandleMultiHashResult(MultiHashActor.MultiHashResult singleHashResult)
        {
            _log.Info($"MultiHashResult -> {singleHashResult.Value}");
            _combineResultsActor.Tell(singleHashResult.Value);
        }
    }
}
