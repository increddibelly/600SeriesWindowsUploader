using Device.Net;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContourNextLink24Manager.Device.Usb
{
    /**
     * USB HID Driver implementation.
     *
     * @author mike wakerly (opensource@hoho.com), Lennart Goedhart (lennart@omnibase.com.au)
     * @see <a
     * href="http://www.usb.org/developers/devclass_docs/usbcdc11.pdf">Universal
     * Serial Bus Class Definitions for Communication Devices, v1.1</a>
     */
    public class UsbHidDriver  
    {
        private const int DEFAULT_READ_BUFFER_SIZE = 16 * 1024;
        private const int DEFAULT_WRITE_BUFFER_SIZE = 16 * 1024;

        protected byte[] _readBuffer;
        protected byte[] _writeBuffer;

        private static string TAG => System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private object ReadBufferLock;
        private object WriteBufferLock;

        private readonly IDevice _device;
        public bool IsConnectionOpen {get; protected set; }

        protected UsbHidDriver(IDevice device)
        {
            _device = device;
            _readBuffer = new byte[DEFAULT_READ_BUFFER_SIZE];
            _writeBuffer = new byte[DEFAULT_WRITE_BUFFER_SIZE];
        }

        public static async Task<UsbHidDriver> Acquire(IDeviceManager usbManager)
        {
            if (usbManager != null)
            {
                try 
                { 
                    var device = await usbManager.Initialize();
                    return new UsbHidDriver(device);
                } catch (Exception ex)
                {
                    _log.Error("could not initialize CNL24", ex);
                }
            }

            return null;
        }

        public async Task Open()
        {
            if (_device.IsInitialized)
                return;
            
            _log.Debug($"{TAG} Claiming HID interface.");

            try
            {
                await _device.InitializeAsync();
            } catch(Exception ex)
            {
                IsConnectionOpen = false;
                throw new IOException("Could not claim data interface.", ex);
            }

            IsConnectionOpen = true;
        }

        public void Close()
        {
           _device?.Close();
            IsConnectionOpen = false;
        }

        public async Task<byte[]> Read(int timeoutMillis)
        {
            byte[] destinationBuffer = null;
            var buffer = await _device.ReadAsync().TimeoutAfter(timeoutMillis);
            var numBytesRead = buffer?.Length ?? 0;
            if (numBytesRead > 0)
            {
               destinationBuffer = buffer.ToArray();
            }

            return destinationBuffer;
        }

        public async Task<int> Write(byte[] src, int timeoutMillis)
        {
            int offset = 0;
            int writeLength;

            while (offset < src.Length) {

                writeLength = Math.Min(src.Length - offset, _writeBuffer.Length);
                if (offset == 0)
                {
                    _writeBuffer = src;
                }
                else
                {
                    _writeBuffer = src.Partial(offset, writeLength);
                }

                try { 
                    await _device.WriteAsync(_writeBuffer).TimeoutAfter(timeoutMillis);
                } catch(Exception ex)
                {
                    throw new IOException($"Error writing {writeLength} bytes at offset {offset} length={src.Length}", ex);
                }

                _log.Debug($"{TAG} Wrote amt={writeLength}");
                offset += writeLength;
            }
            return offset;
        }
    }
}
