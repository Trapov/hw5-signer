using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signer.Application
{
    public sealed class CombineResults : IPipeable<string>
    {
        private readonly BlockingCollection<string> _outPutStream = new BlockingCollection<string>();

        private readonly int _combineBy = 1;

        public CombineResults(int combineBy)
        {
            _combineBy = combineBy;
        }
        public IEnumerable<string> In(IEnumerable<string> input)
        {
            Task.Run(() =>
            {
                var list = new List<string>();
                foreach (var element in input)
                {
                    list.Add(element);

                    if (list.Count != _combineBy)
                        continue;

                    list.Sort();
                    _outPutStream.Add(string.Join("_", list));
                    list.Clear();
                }
            });

            return _outPutStream.GetConsumingEnumerable();
        }
    }
}
