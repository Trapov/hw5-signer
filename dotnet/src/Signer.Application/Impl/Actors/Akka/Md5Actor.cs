using Akka.Actor;
using System.Threading.Tasks;

namespace Signer.Application.Impl.Actors.Akka
{
    public sealed class Md5Actor : ReceiveActor
    {
        public Md5Actor() => ReceiveAsync<string>(HandleAsync);

        private async Task HandleAsync(string input)
        {
            var result = await Signers.DataSignerMd5(input);
            Sender.Tell(new Md5Result(result));
        }

        internal class Md5Result
        {
            public Md5Result(string value) => Value = value;
            public string Value { get; }
        }
    }
}
