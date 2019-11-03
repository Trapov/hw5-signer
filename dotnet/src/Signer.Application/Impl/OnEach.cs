using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signer.Application
{

    public sealed class OnEach : IPipeable<string>
    {
        private readonly BlockingCollection<string> _outPutStream = new BlockingCollection<string>();
        private readonly Action<string> _action;

        public OnEach(Action<string> action)
        {
            _action = action;
        }

        public IEnumerable<string> In(IEnumerable<string> input)
        {
            Task.Run(() =>
            {
                foreach (var @el in input)
                {
                    _action(@el);
                    _outPutStream.Add(el);
                }
            });

            return _outPutStream.GetConsumingEnumerable();
        }
    }
}
