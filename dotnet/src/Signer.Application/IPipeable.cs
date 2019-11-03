using System.Collections.Generic;
using System.Threading.Channels;

namespace Signer.Application
{

    public interface IPipeable<TValue>
    {
        IEnumerable<TValue> In(IEnumerable<TValue> input);
    }

    public interface IChannelPipeable<TValue>
    {
        ChannelReader<TValue> In(ChannelReader<TValue> input);
    }
}
