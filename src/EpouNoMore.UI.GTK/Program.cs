using System;
using System.Diagnostics;
using GLib;
using Application = Gtk.Application;
using Process = System.Diagnostics.Process;

namespace EpouNoMore.UI.GTK
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
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
            Debug.WriteLine("Unhandled exception: {0}", ex);
            Console.WriteLine("Unhandled exception: {0}", ex);
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
