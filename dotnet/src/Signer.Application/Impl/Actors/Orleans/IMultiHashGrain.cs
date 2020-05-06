using Orleans;
using System.Linq;
using System.Threading.Tasks;

namespace Signer.Application.Impl.Actors.Orleans
{
    public interface IMultiHashGrain : IGrainWithStringKey
    {
        Task<string> MultiHash(string input);
    }

    public sealed class MultiHashGrain : Grain, IMultiHashGrain
    {
        public async Task<string> MultiHash(string input)
        {
            return string.Concat(await Task.WhenAll(
                Enumerable
                .Range(0, 6)
                .Select(i => Signers.DataSignerCrc32(i.ToString() + input))
            ));
        }
    }

}
