using ContourNextLink24Manager.Device.Usb;
using System;

namespace ContourNextLink24Manager.Message
{
    internal class AckMessage
    {
        private MedtronicCnlSession pumpSession;
        private object p;

        public AckMessage(MedtronicCnlSession pumpSession, object p)
        {
            this.pumpSession = pumpSession;
            this.p = p;
        }

        internal void send(UsbHidDriver mDevice)
        {
            throw new NotImplementedException();
        }
    }
}