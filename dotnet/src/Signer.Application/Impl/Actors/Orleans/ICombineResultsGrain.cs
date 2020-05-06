using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signer.Application.Impl.Actors.Orleans
{
    public interface ICombineResultsGrain : IGrainWithStringKey
    {
        Task<string> CombineResults(string input);
    }

    public sealed class CombineResultsGrain : Grain, ICombineResultsGrain
    {
        private readonly List<string> _stored = new List<string>();

        public async Task<string> CombineResults(string input)
        {
            _stored.Add(input);

            if (_stored.Count == 10)
            {
                var @return = string.Join('_', _stored);
                _stored.Clear();
                return @return;
            }
            return null;
        }
    }

}
