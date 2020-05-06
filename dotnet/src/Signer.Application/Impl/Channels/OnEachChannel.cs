using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Signer.Application
{
    public sealed class OnEachChannel : IChannelPipeable<string>
    {
        private readonly Action<string> _action;
        private readonly Channel<string> _outPutChannel =
            Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        public OnEachChannel(Action<string> action)
        {
            _action = action;
        }


        public ChannelReader<string> In(ChannelReader<string> input)
        {
            Task.Run(async () =>
            {
                await foreach (var @el in input.ReadAllAsync())
                {
                    _action(@el);
                    await _outPutChannel.Writer.WriteAsync(el);
                }
            });

            return _outPutChannel.Reader;
        }
    }
}
