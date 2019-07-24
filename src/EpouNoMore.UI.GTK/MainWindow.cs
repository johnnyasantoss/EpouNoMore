using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EpouNoMore.Core;
using EpouNoMore.UI.GTK.Extensions;
using EpouNoMore.UI.GTK.IO;
using GLib;
using Gtk;
using Application = Gtk.Application;
using Thread = System.Threading.Thread;

namespace EpouNoMore.UI.GTK
{
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    public class MainWindow : Window
    {
        [Builder.ObjectAttribute] private Button _btnBackup = null;
        [Builder.ObjectAttribute] private Button _btnClearBackup = null;
        [Builder.ObjectAttribute] private Spinner _spinnerBackup = null;
        [Builder.ObjectAttribute] private Switch _fullBackupSwitch = null;
        [Builder.ObjectAttribute] private FileChooserButton _folderChooser = null;
        [Builder.ObjectAttribute] private TextBuffer _textBufferBackup = null;
        [Builder.ObjectAttribute] private TextView _textViewBackup = null;

        private readonly Logger<MainWindow> _logger;

        private readonly string _lineSeparator = string.Join(string.Empty, Enumerable.Repeat("-", 50)) +
                                                 Environment.NewLine;

        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
        }

        private MainWindow(Builder builder) : base(builder.GetObject("_mainWindow").Handle)
        {
            builder.Autoconnect(this);

            _logger = new Logger<MainWindow>();

            Title = "EpouNoMore";

            SetDefaultFolderBackup();

            SubscribeToEvents();
        }

        private void BtnClearBackupClicked(object sender, EventArgs e)
        {
            ResetTextBufferBackup();
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private async void BtnBackupClicked(object sender, EventArgs a)
        {
            _btnBackup.Sensitive = false;
            ToggleBackupSpinner(_spinnerBackup);

            var (isValid, destPathOrMsg) = GetDestPathFromFolderChooser();

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

        private void OnTextBufferBackupOnInsertText(object o, InsertTextArgs args)
        {
            ScrollToEnd(_textViewBackup, _textBufferBackup);
        }

        private void SubscribeToEvents()
        {
            _btnBackup.Clicked += BtnBackupClicked;
            _btnClearBackup.Clicked += BtnClearBackupClicked;

            DeleteEvent += Window_DeleteEvent;

            _textBufferBackup.InsertText += OnTextBufferBackupOnInsertText;
        }

        private void ScrollToEnd(TextView textView, TextBuffer textBuffer)
        {
            this.RunOnMainThread(
                Priority.HighIdle
                , () =>
                {
                    textView.ScrollToMark(
                        textBuffer.InsertMark
                        , 0
                        , true
                        , 0
                        , 1
                    );
                });
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

        private void ResetTextBufferBackup()
        {
            _logger.Info("Cleaning backup text view...");
            _textBufferBackup.Clear();
            _textBufferBackup.Text = "Waiting for your backup..." + Environment.NewLine;
        }

        private (bool, string) GetDestPathFromFolderChooser()
        {
            if (string.IsNullOrWhiteSpace(_folderChooser.CurrentFolder))
                return (false, "Please, select a destination folder.");

            var dirPerm = new DirectoryPermissionManager(_folderChooser.CurrentFolder);
            if (!Directory.Exists(_folderChooser.CurrentFolder) || !dirPerm.HasWritePermission())
                return (false, "Please, select a existent and writable folder.");

            _logger.Info($"Destination folder: {_folderChooser.CurrentFolder}");

            return (true, _folderChooser.CurrentFolder);
        }

        private Task<string> Backup(string destPath, bool fullBackup)
        {
            var tcs = new TaskCompletionSource<string>();

            async void StartBackup()
            {
                var backupManager = new BackupManager(destPath);

                _textBufferBackup.Text = "Starting..." + Environment.NewLine;

                var success = await backupManager.Start(AppendToTextBuffer, fullBackup)
                    .ConfigureAwait(false);

                var msg = success
                    ? $"Backup completed! \"file://{destPath}\""
                    : "Failed!";

                tcs.SetResult(msg + Environment.NewLine);
            }

            var t = new Thread(StartBackup)
            {
                IsBackground = true,
                Name = "backup-manager"
            };
            t.Start();

            return tcs.Task;
        }

        private void AppendToTextBuffer(string data)
        {
            this.RunOnMainThread(Priority.HighIdle, () =>
            {
                var iter = _textBufferBackup.EndIter;
                _textBufferBackup.Insert(ref iter, data);

                iter = _textBufferBackup.EndIter;
                _textBufferBackup.Insert(ref iter, Environment.NewLine);
            });
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
