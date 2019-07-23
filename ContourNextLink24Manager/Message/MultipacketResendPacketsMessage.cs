using ContourNextLink24Manager.Device.Usb;
using System;

namespace ContourNextLink24Manager.Message
{
    internal class MultipacketResendPacketsMessage
    {
        private MedtronicCnlSession pumpSession;
        private byte[] tupple;

        public MultipacketResendPacketsMessage(MedtronicCnlSession pumpSession, byte[] tupple)
        {
            this.pumpSession = pumpSession;
            this.tupple = tupple;
        }

        internal void send(UsbHidDriver mDevice)
        {
            throw new NotImplementedException();
        }
    }
}