using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EpouNoMore.Core;
using Gtk;
using Mono.Unix.Native;
using Switch = Gtk.Switch;

namespace EpouNoMore.UI.GTK
{
    class MainWindow : Window
    {
        [Builder.ObjectAttribute] private Label lblBackupMain = null;
        [Builder.ObjectAttribute] private Button btnBackup = null;
        [Builder.ObjectAttribute] private Spinner spinnerBackup = null;
        [Builder.ObjectAttribute] private Switch fullBackupSwitch = null;
        [Builder.ObjectAttribute] private FileChooserButton folderChooser = null;

        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
        }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            var defaultBackupFolder = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Backup");
            folderChooser.SetCurrentFolder(defaultBackupFolder);

            DeleteEvent += Window_DeleteEvent;
            btnBackup.Clicked += BtnBackupClicked;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private async void BtnBackupClicked(object sender, EventArgs a)
        {
            btnBackup.Sensitive = false;
            ToggleBackupSpinner(spinnerBackup);

            var (isValid, destPathOrMsg) = GetDestPath();

            if (!isValid)
            {
                lblBackupMain.Text = destPathOrMsg;
                return;
            }

            var msg = await Backup(destPathOrMsg, fullBackupSwitch.Active);

            lblBackupMain.Text = msg;

            btnBackup.Sensitive = true;
            ToggleBackupSpinner(spinnerBackup);
        }

        private (bool, string) GetDestPath()
        {
            if (string.IsNullOrWhiteSpace(folderChooser.CurrentFolder))
                return (false, "Please, select a destination folder.");

            if (!Directory.Exists(folderChooser.CurrentFolder) ||
                !CheckFolderWritePermission(folderChooser.CurrentFolder))
                return (false, "Please, select a existent and writable folder.");

            Debug.WriteLine($"Destination folder: {folderChooser.CurrentFolder}", nameof(MainWindow));

            return (true, folderChooser.CurrentFolder);
        }

        private bool CheckFolderWritePermission(string folderPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //TODO: Change to actually verify the permissions
                try
                {
                    var testFile = System.IO.Path.Combine(folderPath, "testfile");
                    File.OpenWrite(testFile).Dispose();
                    File.Delete(testFile);
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }

            if (Syscall.stat(folderPath, out var stat) < 0)
            {
                return false;
            }

            var hasWrite = stat.st_mode & (FilePermissions.S_IWUSR | FilePermissions.S_IWGRP);

            return hasWrite != 0;
        }

        private Task<string> Backup(string destPath, bool fullBackup)
        {
            var tcs = new TaskCompletionSource<string>();

            var t = new Thread(() =>
            {
                if (tcs.Task.Status == TaskStatus.RanToCompletion)
                    return;

                var backupManager = new BackupManager(destPath);

                var success = backupManager.Start(fullBackup);

                var msg = success
                    ? $"Backup completed! \"file://{destPath}\""
                    : "Failed!";

                tcs.SetResult(msg);
            })
            {
                IsBackground = true,
                Name = "backup-manager"
            };
            t.Start();

            return tcs.Task;
        }

        private void ToggleBackupSpinner(Spinner spinner)
        {
            if (spinner.Active == false)
            {
                spinner.Active = true;
                spinner.Start();
            }
            else
            {
                spinner.Active = false;
                spinner.Stop();
            }
        }
    }
}
