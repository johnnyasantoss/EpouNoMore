using System;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix.Native;

namespace EpouNoMore.UI.GTK.IO
{
    public class DirectoryPermissionManager
    {
        private readonly string _path;

        public DirectoryPermissionManager(string path)
        {
            _path = path;
        }

        public bool HasWritePermission()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? HasWritePermissionWin()
                : HasWritePermissionUnix();
        }

        private bool HasWritePermissionUnix()
        {
            if (Syscall.stat(_path, out var stat) < 0)
            {
                return false;
            }

            var hasWrite = stat.st_mode & (FilePermissions.S_IWUSR | FilePermissions.S_IWGRP);

            return hasWrite != 0;
        }

        private bool HasWritePermissionWin()
        {
//TODO: Change to actually verify the permissions
            try
            {
                var testFile = Path.Combine(_path, "testfile");
                File.OpenWrite(testFile).Dispose();
                File.Delete(testFile);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
