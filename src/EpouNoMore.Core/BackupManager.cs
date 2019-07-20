using System;
using System.Diagnostics;
using System.IO;
using EpouNoMore.Core.Internal;

namespace EpouNoMore.Core
{
    public class BackupManager
    {
        private readonly string _destPath;

        public BackupManager(string destPath)
        {
            _destPath = destPath;
        }

        public bool Start(bool fullBackup = true)
        {
            // uses a tmp dir because its faster on tmpfs
            var tmpPath = IOUtility.GetRandomTempPath();

            try
            {
                Debug.WriteLine($"Creating temp directory on {tmpPath}", nameof(BackupManager));
                Directory.CreateDirectory(tmpPath);

                //TODO: Needs pairing
                Debug.WriteLine("Starting backup", nameof(BackupManager));
                var exitCode = ProcessUtility.Instance.StartAndRunCommand(
                    "idevicebackup2",
                    "backup",
                    //TODO: Create a manager for idevice to handle this
                    fullBackup ? "--full" : "",
                    tmpPath
                );
                Debug.WriteLine("Backup completed", nameof(BackupManager));

                //TODO: Check the exit codes
                if (exitCode == 0)
                {
                    MoveContents(tmpPath, _destPath);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error on backup.\n{e.Message}", nameof(BackupManager));
            }
            finally
            {
                if (Directory.Exists(tmpPath))
                    Directory.Delete(tmpPath, true);
            }

            //TODO: Better error handling
            return false;
        }

        private void MoveContents(string tmpPath, string destPath)
        {
            Debug.WriteLine("Moving contents", nameof(BackupManager));

            var tmpInfo = new DirectoryInfo(tmpPath);

            foreach (var dir in tmpInfo.EnumerateDirectories())
            {
                dir.MoveTo(Path.Combine(destPath, dir.Name));
            }
        }
    }
}
