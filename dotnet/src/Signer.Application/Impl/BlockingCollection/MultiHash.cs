using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Signer.Application
{
    public sealed class MultiHash : IPipeable<string>
    {
        private readonly BlockingCollection<string> _outPutStream = new BlockingCollection<string>();
        private readonly int _range = 6;

        public MultiHash(int range)
        {
            _range = range;
        }

        public IEnumerable<string> In(IEnumerable<string> input)
        {
            Task.Run(() =>
            {
                foreach (var element in input)
                {
                    Task.Run(async () =>
                    {
                        var results = await Task.WhenAll(
                            Enumerable
                            .Range(0, _range)
                            .Select(i => Signers.DataSignerCrc32(i.ToString() + element))
                        );
                        _outPutStream.Add(string.Concat(results));
                    });
                }
            });

            return _outPutStream.GetConsumingEnumerable();
        }
    }
}
