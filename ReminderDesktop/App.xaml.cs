using System.Diagnostics;
using System.Windows;

namespace ReminderDesktop;

public partial class App : Application
{
    private Process? webServer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        webServer = new Process();

        webServer.StartInfo.FileName = "dotnet";
        webServer.StartInfo.Arguments =
            "run --project \"D:\\Degree\\Project\\Webreminder\\ReminderWeb\\ReminderWeb.csproj\"";

        webServer.StartInfo.UseShellExecute = false;
        webServer.StartInfo.CreateNoWindow = true;

        webServer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (webServer != null && !webServer.HasExited)
            {
                webServer.Kill(true);
                webServer.Dispose();
            }
        }
        catch
        {
        }

        base.OnExit(e);
    }
}