using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EpouNoMore.Core;
using Gdk;
using GLib;
using Gtk;
using Mono.Unix.Native;
using Action = System.Action;
using Application = Gtk.Application;
using Thread = System.Threading.Thread;
using Window = Gtk.Window;

namespace EpouNoMore.UI.GTK
{
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    class MainWindow : Window
    {
        [Builder.ObjectAttribute] private Button _btnBackup = null;
        [Builder.ObjectAttribute] private Button _btnClearBackup = null;
        [Builder.ObjectAttribute] private Spinner _spinnerBackup = null;
        [Builder.ObjectAttribute] private Switch _fullBackupSwitch = null;
        [Builder.ObjectAttribute] private FileChooserButton _folderChooser = null;
        [Builder.ObjectAttribute] private TextBuffer _textBufferBackup = null;
        [Builder.ObjectAttribute] private TextView _textViewBackup = null;

        private readonly Logger<MainWindow> _logger;
        private readonly string _lineSeparator;

        /// <summary>
        /// G_SOURCE_REMOVE: Frees the callback from memory after calling it
        /// </summary>
        private const bool GSourceRemove = false;

        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
        }

        private MainWindow(Builder builder) : base(builder.GetObject("_mainWindow").Handle)
        {
            builder.Autoconnect(this);

            _logger = new Logger<MainWindow>();

            Title = "EpouNoMore";

            SetDefaultFolderBackup();

            _btnBackup.Clicked += BtnBackupClicked;
            _btnClearBackup.Clicked += BtnClearBackupClicked;

            DeleteEvent += Window_DeleteEvent;
            _lineSeparator = string.Join(string.Empty, Enumerable.Repeat("-", 50)) +
                             Environment.NewLine;

            _textBufferBackup.InsertText += delegate
            {
                RunOnMainThread(
                    Priority.HighIdle
                    , () =>
                    {
                        _textViewBackup.ScrollToMark(
                            _textBufferBackup.InsertMark
                            , 0
                            , true
                            , 0
                            , 1
                        );
                    });
            };
        }

        private void SetDefaultFolderBackup()
        {
            var defaultBackupFolder = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Backup");

            try
            {
                if (!Directory.Exists(defaultBackupFolder))
                    Directory.CreateDirectory(defaultBackupFolder);

                _folderChooser.SetCurrentFolder(defaultBackupFolder);
            }
            catch (Exception e)
            {
                _logger.Warn("Failed to create a default directory on \"{defaultBackupFolder}\"");
                _logger.Warn(e.Message);
            }
        }

        private void BtnClearBackupClicked(object sender, EventArgs e)
        {
            ResetTextBufferBackup();
        }

        private void ResetTextBufferBackup()
        {
            _logger.Info("Cleaning backup text view...");
            _textBufferBackup.Clear();
            _textBufferBackup.Text = "Waiting for your backup..." + Environment.NewLine;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private async void BtnBackupClicked(object sender, EventArgs a)
        {
            _btnBackup.Sensitive = false;
            ToggleBackupSpinner(_spinnerBackup);

            var (isValid, destPathOrMsg) = GetDestPath();

            if (!isValid)
            {
                _textBufferBackup.InsertAtCursor(_lineSeparator);
                _textBufferBackup.InsertAtCursor(destPathOrMsg);
                _logger.Warn(destPathOrMsg);
                return;
            }

            var msg = await Backup(destPathOrMsg, _fullBackupSwitch.Active);

            _textBufferBackup.InsertAtCursor(_lineSeparator);
            _textBufferBackup.InsertAtCursor(msg);
            _logger.Info(msg);

            _btnBackup.Sensitive = true;
            ToggleBackupSpinner(_spinnerBackup);
        }

        private (bool, string) GetDestPath()
        {
            if (string.IsNullOrWhiteSpace(_folderChooser.CurrentFolder))
                return (false, "Please, select a destination folder.");

            if (!Directory.Exists(_folderChooser.CurrentFolder) ||
                !CheckFolderWritePermission(_folderChooser.CurrentFolder))
                return (false, "Please, select a existent and writable folder.");

            _logger.Info($"Destination folder: {_folderChooser.CurrentFolder}");

            return (true, _folderChooser.CurrentFolder);
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

        private void RunOnMainThread(Priority priority, Action action)
        {
            Threads.AddIdle((int) priority, () =>
            {
                action();
                return GSourceRemove;
            });
        }

        private Task<string> Backup(string destPath, bool fullBackup)
        {
            var tcs = new TaskCompletionSource<string>();

            async void StartBackup()
            {
                var backupManager = new BackupManager(destPath);

                _textBufferBackup.Text = "Starting..." + Environment.NewLine;

                void OutputWriter(string data)
                {
                    RunOnMainThread(Priority.HighIdle, () =>
                    {
                        var iter = _textBufferBackup.EndIter;
                        _textBufferBackup.Insert(ref iter, data);

                        iter = _textBufferBackup.EndIter;
                        _textBufferBackup.Insert(ref iter, Environment.NewLine);
                    });
                }

                var procTask = backupManager.Start(OutputWriter, fullBackup)
                    .ConfigureAwait(false);

                var success = await procTask;

                var msg = success
                    ? $"Backup completed! \"file://{destPath}\""
                    : "Failed!";

                tcs.SetResult(msg);
            }

            var t = new Thread(StartBackup)
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
