using System.IO;
using System.Threading.Tasks;

namespace EpouNoMore.Core
{
    /// <summary>
    /// Output of two or more streams. read-only
    /// </summary>
    public class AsyncMultiStreamReader
    {
        private readonly StreamReader[] _streams;

        public AsyncMultiStreamReader(StreamReader[] streams)
        {
            _streams = streams;
        }

        public async Task<string[]> ReadLineAsync()
        {
            var reads = new Task<string>[_streams.Length];

            for (var i = 0; i < _streams.Length; i++)
            {
                reads[i] = _streams[i].ReadLineAsync();
            }

            var lines = await Task.WhenAll(reads)
                .ConfigureAwait(false);

            return lines;
        }
    }
}
