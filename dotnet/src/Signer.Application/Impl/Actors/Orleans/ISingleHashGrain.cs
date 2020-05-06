using Orleans;
using System.Threading.Tasks;

namespace Signer.Application.Impl.Actors.Orleans
{
    public interface ISingleHashGrain : IGrainWithStringKey
    {
        Task<string> SingleHash(IMd5SignerGrain signerGrain, string input);
    }

    public sealed class SingleHashGrain : Grain, ISingleHashGrain
    {
        public async Task<string> SingleHash(IMd5SignerGrain signerGrain, string input)
        {
            return string.Join("~", await Task.WhenAll(Signers.DataSignerCrc32(input), Signers.DataSignerCrc32(await signerGrain.Sign(input))));
        }
    }

}
