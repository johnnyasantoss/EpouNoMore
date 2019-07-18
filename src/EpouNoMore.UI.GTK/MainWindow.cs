using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace epounomore
{
    class MainWindow : Window
    {
        [UI] private Label _lblMain = null;
        [UI] private Button _btnBackup = null;

        private int _counter;

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            _btnBackup.Clicked += Button1_Clicked;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void Button1_Clicked(object sender, EventArgs a)
        {
            _counter++;
            _lblMain.Text = "Hello World! This button has been clicked " + _counter + " time(s).";
        }
    }
}
