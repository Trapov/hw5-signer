using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Signer.Application
{
    public sealed class SingleHash : IPipeable<string>
    {
        private readonly BlockingCollection<string> _outPutStream = new BlockingCollection<string>();

        public IEnumerable<string> In(IEnumerable<string> input)
        {
            Task.Run(async () =>
            {
                foreach (var element in input)
                {
                    var rightSideNotCompleted = await Signers.DataSignerMd5(element);
                    var _ = Task.Run(async () =>
                    {
                        _outPutStream.Add(
                            string.Join("~",
                                await Task.WhenAll(
                                    Signers.DataSignerCrc32(element),
                                    Signers.DataSignerCrc32(rightSideNotCompleted)
                                )
                            )
                        );
                    });
                }
            });

            return _outPutStream.GetConsumingEnumerable();
        }
    }
}
