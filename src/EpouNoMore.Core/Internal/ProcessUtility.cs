using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EpouNoMore.Core.Internal
{
    internal class ProcessUtility
    {
        public static ProcessUtility Instance { get; } = new ProcessUtility();

        private readonly ConcurrentQueue<Process> _children = new ConcurrentQueue<Process>();
        private readonly Logger<ProcessUtility> _logger;

        private ProcessUtility()
        {
            _logger = new Logger<ProcessUtility>();

            Console.CancelKeyPress += delegate { KillChildProcesses(); };

            AppDomain.CurrentDomain.ProcessExit += delegate { KillChildProcesses(); };
        }

        public Task<int> StartAndRunCommand(Action<string> output, string cmd, params string[] args)
        {
            var startInfo = new ProcessStartInfo(cmd, string.Join(" ", args))
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = new Process
            {
                StartInfo = startInfo
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null)
                    return;
                output(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null)
                    return;
                output(e.Data);
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _children.Enqueue(process);

            return WaitProcess(process);
        }

        private static Task<int> WaitProcess(Process process)
        {
            var tcs = new TaskCompletionSource<int>();

            var processHandler = new Thread(_ =>
            {
                try
                {
                    process.WaitForExit();
                    tcs.SetResult(process.ExitCode);
                    process.Dispose();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            })
            {
                IsBackground = true,
                Name = $"process-{process.Id}-handler"
            };

            processHandler.Start();

            return tcs.Task;
        }

        private void KillChildProcesses()
        {
            _logger.Info("Killing all child processes");
            while (_children.TryDequeue(out var process) || _children.Count != 0)
            {
                try
                {
                    if (process.HasExited)
                        continue;
                    process.Kill();
                    process.WaitForExit();
                }
                catch
                {
                    continue;
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
    }
}
