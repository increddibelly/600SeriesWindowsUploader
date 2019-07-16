using ContourNextLink24Manager;
using System;
using System.Threading.Tasks;

namespace Testbed
{
    class Program
    {
        static async Task Main(string[] args)
        {
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
    }
}
