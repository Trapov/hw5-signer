using Akka.Actor;
using System.Linq;
using System.Threading.Tasks;

namespace Signer.Application.Impl.Actors.Akka
{
    public sealed class MultiHashActor : ReceiveActor
    {
        public sealed class MultiHashResult
        {
            public MultiHashResult(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public sealed class MultiHashDone
        {
            public MultiHashDone(string[] value)
            {
                Value = value;
            }

            public string[] Value { get; }
        }

        public MultiHashActor()
        {
            Receive<string>(Handle);
            Receive<MultiHashDone>(Handle);
        }

        public void Handle(string input)
        {
            Task.WhenAll(
                Enumerable
                .Range(0, 6)
                .Select(i => Signers.DataSignerCrc32(i.ToString() + input))
            ).PipeTo(Self, success: r => new MultiHashDone(r));
        }

        public void Handle(MultiHashDone done)
        {
            Context.Parent.Tell(new MultiHashResult(string.Concat(done.Value)));
        }
    }
}
