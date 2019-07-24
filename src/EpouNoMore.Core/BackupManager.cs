using System;
using System.IO;
using System.Threading.Tasks;
using EpouNoMore.Core.Internal;

namespace EpouNoMore.Core
{
    public class BackupManager
    {
        private readonly string _destPath;
        private readonly Logger<BackupManager> _logger;

        public BackupManager(string destPath)
        {
            _destPath = destPath;
            _logger = new Logger<BackupManager>();
        }

        public Task<bool> Start(Action<string> outputReader, bool fullBackup = true)
        {
            // uses a tmp dir because its faster on tmpfs
            var tmpPath = IOUtility.GetRandomTempPath();

            try
            {
                _logger.Info($"Creating temp directory on {tmpPath}");
                var d = Directory.CreateDirectory(tmpPath);
                if (!d.Exists)
                {
                    _logger.Error("Failed to create temp dir");
                    d.Create();
                }

                //TODO: Needs pairing
                _logger.Info("Starting backup");
                var exitCode = ProcessUtility.Instance.StartAndRunCommand(
                    outputReader
                    , "idevicebackup2"
                    //TODO: Create a manager for idevice to handle this
                    , "backup"
                    , fullBackup ? "--full" : ""
                    , tmpPath
                );

                //TODO: Check the exit codes
                //TODO: Better error handling
                return exitCode.ContinueWith(t =>
                {
                    _logger.Info("Backup completed");

                    if (t.Status != TaskStatus.RanToCompletion || t.Result != 0)
                    {
                        if (Directory.Exists(tmpPath))
                            Directory.Delete(tmpPath, true);
                        return false;
                    }

                    MoveContentsToDestination(tmpPath, _destPath);

                    return true;
                });
            }
            catch (Exception e)
            {
                _logger.Error($"Error on backup.\n{e.Message}");
                return Task.FromResult(false);
            }
        }

        private void MoveContentsToDestination(string tmpPath, string destPath)
        {
            _logger.Info("Moving contents to destination folder");

            var tmpInfo = new DirectoryInfo(tmpPath);

            foreach (var dir in tmpInfo.EnumerateDirectories())
            {
                dir.MoveTo(Path.Combine(destPath, dir.Name));
            }
        }
    }
}
