using Eto.Forms;

namespace VisualTrace;
internal static class App 
{
    public static Application app;
}

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        //UserSettings.DeleteSettings();
        UserSettings.LoadSettings();
        App.app = new Application(Eto.Platform.Detect);
        App.app.Run(new MainForm());
    }
}