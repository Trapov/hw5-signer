using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    }

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

    public interface IPipeable<TValue>
    {
        IEnumerable<TValue> In(IEnumerable<TValue> input);
    }
}
