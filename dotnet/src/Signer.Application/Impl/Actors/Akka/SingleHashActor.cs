using Akka.Actor;
using System;
using System.Threading.Tasks;

namespace Signer.Application.Impl.Actors.Akka
{
    public sealed class SingleHashActor : ReceiveActor
    {
        public SingleHashActor()
        {
            ReceiveAsync<string>(HandleAsync);
            Receive<SingleHashResult>(m => Context.Parent.Forward(m));
        }

        public sealed class SingleHashResult
        {
            public SingleHashResult(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public sealed class ForLeftSide
        {
            public ForLeftSide(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public sealed class ForRightSide
        {
            public ForRightSide(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        sealed class Crc32Worker : ReceiveActor
        {
            private string _leftSide = string.Empty;
            private string _rightSide = string.Empty;

            public Crc32Worker()
            {
                Receive<ForLeftSide>(Handle);
                Receive<ForRightSide>(Handle);

                Receive<LeftSideDone>(Handle);
                Receive<RightSideDone>(Handle);

                Receive<SingleHashResult>(m => Context.Parent.Forward(m));
            }

            public sealed class RightSideDone
            {
                public RightSideDone(string value)
                {
                    Value = value;
                }

                public string Value { get; }
            }

            public sealed class LeftSideDone
            {
                public LeftSideDone(string value)
                {
                    Value = value;
                }

                public string Value { get; }
            }

            public void Handle(LeftSideDone leftSideDone)
            {
                _leftSide = leftSideDone.Value;
                if (_rightSide != string.Empty)
                {
                    Context
                        .Parent
                    .Tell(new SingleHashResult(string.Join('~', _leftSide, _rightSide)));
                }
            }
            public void Handle(RightSideDone rightSideDone)
            {
                _rightSide = rightSideDone.Value;
                if (_leftSide != string.Empty)
                {
                    Context
                        .Parent
                    .Tell(new SingleHashResult(string.Join('~', _leftSide, _rightSide)));
                }
            }
            public void Handle(ForLeftSide rawInput)
            {
                Signers.DataSignerCrc32(rawInput.Value)
                    .PipeTo(Self, success: (i) => new LeftSideDone(i));
            }

            public void Handle(ForRightSide rawInput)
            {
                Signers.DataSignerCrc32(rawInput.Value)
                    .PipeTo(Self, success: (i) => new RightSideDone(i));
            }
        }

        public async Task HandleAsync(string input)
        {
            var crc32Worker = Context.ActorOf<Crc32Worker>();
            crc32Worker.Tell(new ForLeftSide(input));

            var md5Result = await Signers.DataSignerMd5(input);
            crc32Worker.Tell(new ForRightSide(md5Result));
        }
    }
}
