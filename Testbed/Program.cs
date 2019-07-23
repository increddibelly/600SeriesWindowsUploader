using ContourNextLink24Manager.Device;
using NightscoutClient;
using NightscoutClient.CodeGeneration;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Testbed
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await ClientGenerator.Run(Secrets.NightscoutSiteURI, GetTargetPath());

            Console.WriteLine("Initializing Contour Next Link...");
            var mgr = new CNL24DeviceManager();
            var device = await mgr.Initialize();
            if (device == null)
            {
                Console.WriteLine($"Device not found. Is it connected?");
                return;
            }
            Console.WriteLine($"DeviceId = {device.DeviceId}");
            
            Console.ReadKey();
        }

        private static string GetTargetPath()
        {
            var appLocation = Assembly.GetExecutingAssembly().CodeBase; // "file:///C:/Repos/600SeriesWindowsUploader/Testbed/bin/Debug/netcoreapp3.0/Testbed.dll
            var rootFolder = appLocation.Replace("Testbed/bin/Debug/netcoreapp3.0/Testbed.dll", "");
            var result = Path.Combine(rootFolder, @"NightscoutClient\Api\Client.cs");
            return result;
        }
    }
}
