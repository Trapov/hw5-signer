using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signer.Application.Impl.Actors.Akka
{
    public class PipelineActor : ReceiveActor
    {
        public readonly IActorRef SingleHash;
        public readonly IActorRef MultiHash;
        public readonly IActorRef CombineResults;

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

            Receive<SingleHashActor.SingleHashResult>(HandleSingleHashResult);
            Receive<MultiHashActor.MultiHashResult>(HandleMultiHashResult);
            Receive<CombineResultsActor.CombineResultsResult>(HandleCombineResultsResult);

            Execute(Enumerable.Range(0, inputElements).Select(i => i.ToString()));
        }

        private void HandleCombineResultsResult(CombineResultsActor.CombineResultsResult obj)
        {
            _counter++;
            Context.System.Log.Info(obj.Value);

            if(_counter == (_inputElements / _combineBy))
            {
                _manualResetEventSlim.Set();
            }
        }

        public void Execute(IEnumerable<string> enumerable)
        {
            Task.Run(() => 
            {
                foreach (var e in enumerable)
                    SingleHash.Tell(e);
            });
        }

        private void HandleSingleHashResult(SingleHashActor.SingleHashResult singleHashResult)
        {
            MultiHash.Tell(singleHashResult.Value);
            Context.System.Log.Info(singleHashResult.Value);
        }

        private void HandleMultiHashResult(MultiHashActor.MultiHashResult singleHashResult)
        {
            CombineResults.Tell(singleHashResult.Value);
            Context.System.Log.Info(singleHashResult.Value);
        }
    }
}
