using Gdk;
using GLib;
using Action = System.Action;
using Window = Gtk.Window;

namespace EpouNoMore.UI.GTK.Extensions
{
    public static class GtkWindowExtensions
    {
        /// <summary>
        /// G_SOURCE_REMOVE: Frees the callback from memory after calling it
        /// </summary>
        private const bool GSourceRemove = false;

        public static void RunOnMainThread(this Window _, Priority priority, Action action)
        {
            Threads.AddIdle((int) priority, () =>
            {
                action();
                return GSourceRemove;
            });
        }
    }
}
