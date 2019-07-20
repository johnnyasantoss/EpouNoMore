using System;
using System.IO;

namespace EpouNoMore.Core
{
    // ReSharper disable once InconsistentNaming
    public static class IOUtility
    {
        public static string GetRandomTempPath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }
    }
}