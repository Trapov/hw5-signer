﻿using System.Threading.Tasks;
using System.Linq;
using System.Threading.Channels;

namespace Signer.Application
{
    public sealed class MultiHashChannel : IChannelPipeable<string>
    {
        private readonly Channel<string> _outPutChannel =
            Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        private readonly int _range = 6;

        public MultiHashChannel(int range)
        {
            _range = range;
        }

        public ChannelReader<string> In(ChannelReader<string> input)
        {
            Task.Run(async () =>
            {
                await foreach (var element in input.ReadAllAsync())
                {
                    var rightSideNotCompleted = await Signers.DataSignerMd5(element);
                    var _ = Task.Run(async () =>
                    {
                        var results = await Task.WhenAll(
                            Enumerable
                            .Range(0, _range)
                            .Select(i => Signers.DataSignerCrc32(i.ToString() + element))
                        );
                        _outPutChannel.Writer.WriteAsync(string.Concat(results));
                    });
                }
            });

            return _outPutChannel.Reader;
        }
    }
}