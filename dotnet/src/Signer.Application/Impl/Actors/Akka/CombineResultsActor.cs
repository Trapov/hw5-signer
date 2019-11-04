using Akka.Actor;
using System.Collections.Generic;

namespace Signer.Application.Impl.Actors.Akka
{
    public sealed class CombineResultsActor : ReceiveActor
    {
        private readonly List<string> _stored = new List<string>();

        public CombineResultsActor()
        {
            Receive<string>(Handle);
        }

        public sealed class CombineResultsResult
        {
            public CombineResultsResult(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public void Handle(string input)
        {
            _stored.Add(input);

            if(_stored.Count == 10)
            {
                Context.Parent.Tell(new CombineResultsResult(string.Join('_', _stored)));
                _stored.Clear();
            }
        }
    }
}
