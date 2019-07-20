using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace EpouNoMore.Core
{
    public class ProcessUtility
    {
        public static ProcessUtility Instance { get; } = new ProcessUtility();

        private readonly ConcurrentQueue<Process> _children = new ConcurrentQueue<Process>();

        private ProcessUtility()
        {
            Console.CancelKeyPress += delegate { KillChildProcesses(); };

            AppDomain.CurrentDomain.ProcessExit += delegate { KillChildProcesses(); };
        }

        public int StartAndRunCommand(string cmd, params string[] args)
        {
            var startInfo = new ProcessStartInfo(cmd, string.Join(" ", args));
            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            _children.Enqueue(process);

            process.WaitForExit();

            return process.ExitCode;
        }

        private void KillChildProcesses()
        {
            Debug.WriteLine("Killing all child processes", nameof(ProcessUtility));
            while (_children.TryDequeue(out var process) || _children.Count != 0)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }

                process.Dispose();
            }
        }
    }
}
