using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Device.Net;
using Hid.Net.Windows;
using WindowsUploader.ContourNextLink24Manager;

namespace ContourNextLink24Manager
{
    public class CNL24DeviceManager
    {
        private readonly ILogger _logger = new DeviceLogger();

        private readonly FilterDeviceDefinition ContourNextLink24DeviceDefinition = 
            new FilterDeviceDefinition {
                DeviceType = DeviceType.Hid,
                VendorId = CNL24Definition.VendorId,
                ProductId = CNL24Definition.PID,
                Label = CNL24Definition.Label
            };

        public async Task<IDevice> Initialize()
        {
            try {
                WindowsHidDeviceFactory.Register(_logger);

                var devices = await DeviceManager.Current.GetDevicesAsync(new List<FilterDeviceDefinition> { ContourNextLink24DeviceDefinition });

                var selectedDevice = devices?.FirstOrDefault();
                await selectedDevice.InitializeAsync();

                return selectedDevice;
            } catch (Exception ex)
            {
                _logger.Log(ex.Message, "CNL24Manager", ex, LogLevel.Error);
            }
            return null;
        }
    }
}
