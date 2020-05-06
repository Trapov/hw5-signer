using System.Threading.Channels;
using System.Threading.Tasks;

namespace Signer.Application
{
    public sealed class SingleHashChannel : IChannelPipeable<string>
    {
        private readonly Channel<string> _outPutChannel =
            Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        public ChannelReader<string> In(ChannelReader<string> input)
        {
            Task.Run(async () =>
            {
                await foreach (var element in input.ReadAllAsync())
                {
                    var rightSideNotCompleted = await Signers.DataSignerMd5(element);
                    var _ = Task.Run(async () =>
                    {
                        await _outPutChannel.Writer.WriteAsync(
                            string.Join("~",
                                await Task.WhenAll(
                                    Signers.DataSignerCrc32(element),
                                    Signers.DataSignerCrc32(rightSideNotCompleted)
                                )
                            )
                        );
                    });
                }
            });

            return _outPutChannel.Reader;
        }
    }
}
