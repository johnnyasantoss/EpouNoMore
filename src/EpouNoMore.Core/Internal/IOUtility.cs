using System;
using System.IO;

namespace EpouNoMore.Core.Internal
{
    // ReSharper disable once InconsistentNaming
    internal static class IOUtility
    {
        public static string GetRandomTempPath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }
    }
}
