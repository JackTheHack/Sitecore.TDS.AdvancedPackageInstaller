using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace HedgehogDevelopment.PackageInstaller
{
    [HubName("InstallerHub")]
    public class InstallerHub : Hub
    {
        public void Send(string message, string level)
        {
            if (!Program.DisableLogging)
            {
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss}] {message}");
            }
        }
    }
}
