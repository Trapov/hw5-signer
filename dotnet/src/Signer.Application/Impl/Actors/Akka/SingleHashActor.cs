using Akka.Actor;

namespace Signer.Application.Impl.Actors.Akka
{
    public sealed class SingleHashActor : ReceiveActor
    {
        private readonly IActorRef _leftSideActor;
        private readonly IActorRef _rightSideActor;

        private string _leftSide = null;
        private string _rightSide = null;

        internal sealed class LeftSideActor : ReceiveActor
        {
            public LeftSideActor() => Receive<string>(HandleAsync);
            private async void HandleAsync(string input) => Sender.Tell(new Result(await Signers.DataSignerCrc32(input)));
            public sealed class Result
            {
                public Result(string value)
                {
                    Value = value;
                }

                public string Value { get; }
            }
        }
        internal sealed class RightSideActor : ReceiveActor
        {
            public RightSideActor()
            {
                Receive<string>(Handle);
                Receive<Md5Actor.Md5Result>(HandleAsync);
            }

            private async void HandleAsync(Md5Actor.Md5Result result) => Context.Parent.Tell(new Result(await Signers.DataSignerCrc32(result.Value)));
            private async void Handle(string input) => Context.ActorSelection($"../../{nameof(Md5Actor)}").Tell(input);

            public sealed class Result
            {
                public Result(string value)
                {
                    Value = value;
                }

                public string Value { get; }
            }
        }

        public SingleHashActor()
        {
            Receive<string>(Handle);

            Receive<RightSideActor.Result>(Handle);
            Receive<LeftSideActor.Result>(Handle);

            _leftSideActor = Context.ActorOf<LeftSideActor>();
            _rightSideActor = Context.ActorOf<RightSideActor>();
        }

        private void Handle(RightSideActor.Result result)
        {
            _rightSide = result.Value;

            if (_leftSide != null)
                Context.Parent.Tell(new SingleHashResult(string.Concat(_leftSide, "~", _rightSide)));
        }

        private void Handle(LeftSideActor.Result result)
        {
            _leftSide = result.Value;

            if (_rightSide != null)
                Context.Parent.Tell(new SingleHashResult(string.Concat(_leftSide, "~", _rightSide)));
        }

        public async void Handle(string input)
        {
            _leftSideActor.Tell(input);
            _rightSideActor.Tell(input);
        }

        public sealed class SingleHashResult
        {
            public SingleHashResult(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }
}
