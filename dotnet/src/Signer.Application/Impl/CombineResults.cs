using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace Signer.Application
{

    public sealed class CombineResults : IPipeable<string>
    {
        private readonly BlockingCollection<string> _outPutStream = new BlockingCollection<string>();

        private readonly int _combineBy = 1;
        public int Counter { get; private set; } = 0;

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
                    Counter++;

                    list.Add(element);

                    if (Counter != this._combineBy)
                        continue;

                    Counter = 0;

                    list.Sort();
                    _outPutStream.Add(string.Join("_", list));
                    list.Clear();
                }
            });

            return _outPutStream.GetConsumingEnumerable();
        }
    }
}
