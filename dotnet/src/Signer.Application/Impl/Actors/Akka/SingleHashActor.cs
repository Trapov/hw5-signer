using System.Threading.Tasks;
using Akka.Actor;

namespace Signer.Application.Impl.Actors.Akka
{
    public sealed class SingleHashActor : ReceiveActor
    {
        private readonly IActorRef _leftSideActor;
        private readonly IActorRef _rightSideActor;
        private readonly IActorRef _realParent;

        private string _leftSide = null;
        private string _rightSide = null;

        private sealed class LeftSideActor : ReceiveActor
        {
            public LeftSideActor() => Receive<string>(HandleAsync);
            private async void HandleAsync(string input) => Sender.Tell(new Result(await Signers.DataSignerCrc32(input)));
            public sealed class Result
            {
                public Result(string value) => Value = value;
                public string Value { get; }
            }
        }

        private sealed class RightSideActor : ReceiveActor
        {
            private readonly IActorRef _md5Actor;
            private readonly IActorRef _parent;
            public RightSideActor(IActorRef md5Actor, IActorRef parent)
            {
                _md5Actor = md5Actor;
                _parent = parent;
                Receive<string>(Handle);
                Receive<Md5Actor.Md5Result>(HandleAsync);
            }

            private async void HandleAsync(Md5Actor.Md5Result md5Result) =>
                _parent.Tell(new Result(
                    await Signers.DataSignerCrc32(md5Result.Value)
                ));

            private void Handle(string input) => _md5Actor.Tell(input);

            public sealed class Result
            {
                public Result(string value) => Value = value;
                public string Value { get; }
            }
        }

        public SingleHashActor(IActorRef md5Actor, IActorRef realParent)
        {
            _realParent = realParent;
            Receive<string>(Handle);

            Receive<RightSideActor.Result>(Handle);
            Receive<LeftSideActor.Result>(Handle);

            _leftSideActor = Context.ActorOf<LeftSideActor>();
            _rightSideActor = Context.ActorOf(Props.Create<RightSideActor>(md5Actor, Self));
        }

        private void Handle(RightSideActor.Result result)
        {
            _rightSide = result.Value;

            if (_leftSide == null) return;
            _realParent.Tell(new SingleHashResult(string.Concat(_leftSide, "~", _rightSide)));
            _leftSide = null;
            _rightSide = null;
        }

        private void Handle(LeftSideActor.Result result)
        {
            _leftSide = result.Value;

            if (_rightSide == null) return;
            _realParent.Tell(new SingleHashResult(string.Concat(_leftSide, "~", _rightSide)));
            _leftSide = null;
            _rightSide = null;
        }

        private void Handle(string input)
        {
            _leftSideActor.Tell(input);
            _rightSideActor.Tell(input);
        }

        public sealed class SingleHashResult
        {
            public SingleHashResult(string value) => Value = value;
            public string Value { get; }
        }
    }
}
