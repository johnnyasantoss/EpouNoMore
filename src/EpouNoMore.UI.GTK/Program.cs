using System;
using EpouNoMore.Core;
using GLib;
using Application = Gtk.Application;
using Process = System.Diagnostics.Process;

namespace EpouNoMore.UI.GTK
{
    public class Program
    {
        private static Logger<Program> _logger;

        [STAThread]
        public static void Main(string[] args)
        {
            _logger = new Logger<Program>();

            AppDomain.CurrentDomain.UnhandledException += (_, e) => OnUnhandledException(e);
            ExceptionManager.UnhandledException += OnUnhandledException;

            Application.Init();

            SetupProcess();
            SetupApplication();

            Application.Run();
        }

        private static void OnUnhandledException(UnhandledExceptionEventArgs eventArgs)
        {
            var ex = (Exception) eventArgs.ExceptionObject;

            var msg = $"Unhandled exception: {ex}";

            _logger.Error(msg);
        }

        private static void SetupProcess()
        {
            Process.GetCurrentProcess().PriorityBoostEnabled = true;
        }

        private static void SetupApplication()
        {
            var app = new Application("org.EpouNoMore.EpouNoMore", ApplicationFlags.None);
            app.Register(Cancellable.Current);

            var win = new MainWindow();
            app.AddWindow(win);

            win.Show();
        }
    }
}
