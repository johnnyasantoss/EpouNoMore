using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EpouNoMore.Core
{
    public class Logger<T> where T : class
    {
        private string NameOfT
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => typeof(T).Name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warn(string msg)
        {
            msg = Fmt(msg);
            Debug.WriteLine(msg);
            Console.Error.WriteLine(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string msg)
        {
            msg = Fmt(msg);
            Debug.WriteLine(msg);
            Console.Error.WriteLine(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string msg)
        {
            msg = Fmt(msg);
            Console.WriteLine(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string Fmt(string msg, [CallerMemberName] string level = null)
        {
            Debug.Assert(level != null, nameof(level) + " != null");
            return $"{level.ToUpper(),5}: {NameOfT}: {msg}";
        }
    }
}
