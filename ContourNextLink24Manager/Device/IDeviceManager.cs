using System.Threading.Tasks;
using Device.Net;

namespace ContourNextLink24Manager.Device
{
    public interface IDeviceManager
    {
        Task<IDevice> Initialize();
    }
}
