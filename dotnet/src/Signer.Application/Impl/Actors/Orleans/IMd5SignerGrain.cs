using Orleans;
using System.Threading.Tasks;

namespace Signer.Application.Impl.Actors.Orleans
{
    public interface IMd5SignerGrain : IGrainWithStringKey
    {
        Task<string> Sign(string input);
    }

    public sealed class Md5SignerGrain : Grain, IMd5SignerGrain
    {
        public async Task<string> Sign(string input)
        {
            return await Signers.DataSignerMd5(input);
        }
    }
}
