using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Signer.Application
{
    public static class Pipeline
    {
        public static IPipeable<TValue> Execute<TValue>(IEnumerable<TValue> input, params IPipeable<TValue>[] pipeables)
        {
            var firstPipe = pipeables.First();
            var @out = firstPipe.In(input);

            foreach (var pipe in pipeables.Skip(1))
                @out = pipe.In(@out);

            return pipeables.Last();
        }

        public static IChannelPipeable<TValue> Execute<TValue>(IEnumerable<TValue> input, params IChannelPipeable<TValue>[] pipeables)
        {
            var inputChannel = Channel.CreateUnbounded<TValue>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            Task.Run(async () =>
            {
                foreach (var @el in input)
                    await inputChannel.Writer.WriteAsync(@el);
            });

            var @out = inputChannel.Reader;

            foreach (var pipe in pipeables)
                @out = pipe.In(@out);

            return pipeables.Last();
        }
    }
}
