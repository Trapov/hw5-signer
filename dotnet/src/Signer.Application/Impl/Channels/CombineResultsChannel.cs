using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Signer.Application
{
    public sealed class CombineResultsChannel : IChannelPipeable<string>
    {
        private readonly Channel<string> _outPutChannel =
            Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        private readonly int _combineBy = 6;
        public int Counter { get; private set; } = 0;

        public CombineResultsChannel(int combineBy)
        {
            _combineBy = combineBy;
        }

        public ChannelReader<string> In(ChannelReader<string> input)
        {
            Task.Run(async () =>
            {
                var list = new List<string>();

                await foreach (var element in input.ReadAllAsync())
                {
                    Counter++;

                    list.Add(element);

                    if (Counter != this._combineBy)
                        continue;

                    Counter = 0;

                    list.Sort();
                    _outPutChannel.Writer.WriteAsync(string.Join("_", list));
                    list.Clear();
                }
            });

            return _outPutChannel.Reader;
        }
    }
}
