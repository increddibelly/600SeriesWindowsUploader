
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ContourNextLink24Manager.Message
{
    public enum CommandAction : byte
    {
        NO_TYPE = 0x0,
        CHANNEL_NEGOTIATE = 0x03,
        PUMP_REQUEST = 0x05,
        PUMP_RESPONSE = 0x55
    };

    public enum CommandType : byte
    {
        NO_TYPE = 0x0,
        OPEN_CONNECTION = 0x10,
        CLOSE_CONNECTION = 0x11,
        SEND_MESSAGE = 0x12,
        READ_INFO = 0x14,
        REQUEST_LINK_KEY = 0x16,
        SEND_LINK_KEY = 0x17,
        RECEIVE_MESSAGE = 0x80,
        SEND_MESSAGE_RESPONSE = 0x81,
        REQUEST_LINK_KEY_RESPONSE = 0x86
    }

    public enum ASCII
    {
        STX = 0x02,
        EOT = 0x04,
        ENQ = 0x05,
        ACK = 0x06,
        NAK = 0x15
    }

    public abstract class ContourNextLinkMessage
    {
        private static string TAG => System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;

        public const int CLEAR_TIMEOUT_MS = 1000; // note: 2000ms was used for v5.1
        public const int ERROR_CLEAR_TIMEOUT_MS = 2000;
        public const int PRESEND_CLEAR_TIMEOUT_MS = 100;

        public const int READ_TIMEOUT_MS = 10000;
        public const int WRITE_TIMEOUT_MS = 200;
        public const int CNL_READ_TIMEOUT_MS = 2000;
        public const int PUMP_READ_TIMEOUT_MS = 10000;

        private const int MULTIPACKET_TIMEOUT_MS = 1000;
        private const int SEGMENT_RETRY = 10;

        private const int USB_MESSAGE_SIZE = 60;
        private const int USB_BLOCKSIZE = 64;
        private const string USB_HEADER = "ABC";

        protected byte[] mPayload;

        protected ContourNextLinkMessage(byte[] bytes)
        {
            SetPayload(bytes);
        }

        public byte[] Encode()
        {
            return mPayload.ToArray(); 
        }

        // FIXME - get rid of this - make a Builder instead
        protected void SetPayload(byte[] payload)
        {
            if (payload != null)
            {
                mPayload = payload.ToArray();
            }
        }

        protected void SendMessage(UsbHidDriver mDevice)
        {
            int pos = 0;
            byte[] message = Encode();

            while (message.Length > pos)
            {
                byte[] outputBuffer = new byte[USB_BLOCKSIZE];
                int sendLength = (pos + USB_MESSAGE_SIZE > message.Length) 
                    ? message.Length - pos 
                    : USB_MESSAGE_SIZE;
                outputBuffer.Put(Encoding.ASCII.GetBytes(USB_HEADER));
                outputBuffer.Put((byte)sendLength);
                outputBuffer.Put(message, pos, sendLength);

                mDevice.write(outputBuffer, WRITE_TIMEOUT_MS);
                pos += sendLength;

                string outputstring = HexDump.DumpHexstring(outputBuffer);
                Log.d(TAG, "WRITE: " + outputstring);
            }
        }

        protected byte[] ReadMessage(UsbHidDriver mDevice, int timeout = READ_TIMEOUT_MS)
        {
            ByteArrayOutputStream responseMessage = new ByteArrayOutputStream();

            byte[] responseBuffer = new byte[USB_BLOCKSIZE];
            int bytesRead;
            int messageSize = 0;

            do
            {
                bytesRead = mDevice.read(responseBuffer, timeout);

                if (bytesRead == -1)
                {
                    throw new TimeoutException("Timeout waiting for response from pump");
                }
                else if (bytesRead > 0)
                {
                    // Validate the header
                    byte[] header = new byte[USB_HEADER.Length];
                    header.put(responseBuffer, 0, USB_HEADER.Length);
                    string headerstring = new string(header.array());
                    if (!headerstring.equals(USB_HEADER))
                    {
                        throw new IOException("Unexpected header received");
                    }
                    messageSize = responseBuffer[3];
                    responseMessage.write(responseBuffer, 4, messageSize);
                }
                else
                {
                    Log.w(TAG, "readMessage: got a zero-sized response.");
                }
            } while (bytesRead > 0 && messageSize == USB_MESSAGE_SIZE);

            string responsestring = HexDump.dumpHexstring(responseMessage.toByteArray());
            Log.d(TAG, "READ: " + responsestring);

            return responseMessage.toByteArray();
        }

        // safety check to make sure an expected 0x81 response is received before next expected 0x80 response
        // very infrequent as clearMessage catches most issues but very important to save a CNL error situation

        protected byte[] ReadMessage_0x81(UsbHidDriver mDevice)
        {
            return ReadMessage_0x81(mDevice, READ_TIMEOUT_MS);
        }

        protected byte[] ReadMessage_0x81(UsbHidDriver mDevice, int timeout)
        {
            byte[] responseBytes;
            bool doRetry;
            do
            {
                responseBytes = ReadMessage(mDevice, timeout);
                if (responseBytes[18] != (byte)0x81)
                {
                    doRetry = true;
                    Log.d(TAG, "readMessage0x81: did not get 0x81 response, got " + HexDump.ToHexstring(responseBytes[18]));
                    MedtronicCnlService.cnl0x81++;
                }
                else
                {
                    doRetry = false;
                }

            } while (doRetry);

            return responseBytes;
        }

        // intercept unexpected messages from the CNL
        // these usually come from pump requests as it can occasionally resend message responses several times (possibly due to a missed CNL ACK during CNL-PUMP comms?)
        // mostly noted on the higher radio channels, channel 26 shows this the most
        // if these messages are not cleared the CNL will likely error needing to be unplugged to reset as it expects them to be read before any further commands are sent

        protected int ClearMessage(UsbHidDriver mDevice)
        {
            return ClearMessage(mDevice, CLEAR_TIMEOUT_MS);
        }

        protected int ClearMessage(UsbHidDriver mDevice, int timeout)
        {
            int count = 0;
            bool cleared = false;

            do
            {
                try
                {
                    ReadMessage(mDevice, timeout);
                    count++;
                    MedtronicCnlService.cnlClear++;
                }
                catch (TimeoutException e)
                {
                    cleared = true;
                }
            } while (!cleared);

            if (count > 0)
            {
                Log.d(TAG, "clearMessage: message stream cleared " + count + " messages.");
            }

            return count;
        }

        protected byte[] SendToPump(UsbHidDriver mDevice, MedtronicCnlSession pumpSession, string tag)
        {
            tag = " (" + tag + ")";
            byte[] payload;
            byte medtronicSequenceNumber = pumpSession.getMedtronicSequenceNumber();

            // extra safety check and delay, CNL is not happy when we miss incoming messages
            // the short delay may also help with state readiness
            ClearMessage(mDevice, PRESEND_CLEAR_TIMEOUT_MS);

            SendMessage(mDevice);

            try
            {
                payload = ReadMessage_0x81(mDevice);
            }
            catch (TimeoutException e)
            {
                // ugh... there should always be a CNL 0x81 response and if we don't get one it usually ends with a E86 / E81 error on the CNL needing a unplug/plug cycle
                Log.e(TAG, "Timeout waiting for 0x81 response." + tag);
                ClearMessage(mDevice, ERROR_CLEAR_TIMEOUT_MS);
                throw new TimeoutException("Timeout waiting for 0x81 response" + tag);
            }

            Log.d(TAG, "0x81 response: payload.Length=" + payload.Length + (payload.Length >= 0x30 ? " payload[0x21]=" + payload[0x21] + " payload[0x2C]=" + payload[0x2C] + " medtronicSequenceNumber=" + medtronicSequenceNumber + " payload[0x2D]=" + payload[0x2D] : "") + tag);

            // following errors usually have no further response from the CNL but occasionally they do
            // and these need to be read and cleared asap or yep E86 me baby and a unresponsive CNL
            // the extra delay from the clearMessage timeout may be helping here too by holding back any further downstream sends etc - investigate
            if (payload.Length <= 0x21)
            {
                ClearMessage(mDevice, ERROR_CLEAR_TIMEOUT_MS);
                throw new UnexpectedMessageException("0x81 response was empty" + tag);  // *bad* CNL death soon after this, may want to end comms immediately
            }
            else if (payload.Length != 0x30 && payload[0x21] != 0x55)
            {
                ClearMessage(mDevice, ERROR_CLEAR_TIMEOUT_MS);
                throw new UnexpectedMessageException("0x81 response was not a 0x55 message" + tag);
            }
            else if (payload[0x2C] != medtronicSequenceNumber)
            {
                ClearMessage(mDevice, ERROR_CLEAR_TIMEOUT_MS);
                throw new UnexpectedMessageException("0x81 sequence number does not match" + tag);
            }
            else if (payload[0x2D] == 0x04)
            {
                ClearMessage(mDevice, ERROR_CLEAR_TIMEOUT_MS);
                throw new UnexpectedMessageException("0x81 connection busy" + tag);
            }
            else if (payload[0x2D] != 0x02)
            {
                ClearMessage(mDevice, ERROR_CLEAR_TIMEOUT_MS);
                throw new UnexpectedMessageException("0x81 connection lost" + tag);
            }

            return payload;
        }

        protected byte[] ReadFromPump(UsbHidDriver mDevice, MedtronicCnlSession pumpSession, string tag)
        {
            tag = " (" + tag + ")";

            MultipacketSession multipacketSession = null;
            byte[] tupple;
            byte[] payload = null;
            byte[] decrypted = null;

            bool fetchMoreData = true;
            int retry = 0;
            int expectedSegments = 0;

            int cmd;

            while (fetchMoreData)
            {
                if (multipacketSession != null)
                {
                    do
                    {
                        if (expectedSegments < 1)
                        {
                            tupple = multipacketSession.missingSegments();
                            new MultipacketResendPacketsMessage(pumpSession, tupple).send(mDevice);
                            expectedSegments = read16BEtoUInt(tupple, 0x02);
                        }
                        try
                        {
                            payload = ReadMessage(mDevice, MULTIPACKET_TIMEOUT_MS);
                            retry = 0;
                        }
                        catch (TimeoutException e)
                        {
                            if (++retry >= SEGMENT_RETRY)
                                throw new TimeoutException("Timeout waiting for response from pump (multipacket)" + tag);
                            Log.d(TAG, "*** Multisession timeout, expecting:" + expectedSegments + " retry: " + retry);
                            expectedSegments = 0;
                        }
                    } while (retry > 0);
                }
                else
                {
                    try
                    {
                        payload = ReadMessage(mDevice, READ_TIMEOUT_MS);
                    }
                    catch (TimeoutException e)
                    {
                        throw new TimeoutException("Timeout waiting for response from pump" + tag);
                    }
                }

                if (payload.Length < 0x0039)
                {
                    Log.d(TAG, "*** bad response" + HexDump.dumpHexstring(payload, 0x12, payload.Length - 0x14));
                    fetchMoreData = true;

                }
                else
                {
                    decrypted = Decode(pumpSession, payload);

                    cmd = read16BEtoUInt(decrypted, RESPONSE_COMMAND);
                    Log.d(TAG, "CMD: " + HexDump.toHexstring(cmd));

                    if (MedtronicSendMessageRequestMessage.MessageType.EHSM_SESSION.response(cmd))
                    { // EHSM_SESSION(0)
                        Log.d(TAG, "*** EHSM response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = true;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.NAK_COMMAND.response(cmd))
                    {
                        Log.d(TAG, "*** NAK response" + HexDump.DumpHexstring(decrypted));
                        ClearMessage(mDevice, ERROR_CLEAR_TIMEOUT_MS); // if multipacket was in progress we may need to clear 2 EHSM_SESSION(1) messages from pump
                        short nakcmd = read16BEtoShort(decrypted, 3);
                        byte nakcode = decrypted[5];
                        throw new UnexpectedMessageException("Pump sent a NAK(" + string.format("%02X", nakcmd) + ":" + string.format("%02X", nakcode) + ") response" + tag);
                        //fetchMoreData = false;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.INITIATE_MULTIPACKET_TRANSFER.response(cmd))
                    {
                        multipacketSession = new MultipacketSession(decrypted);
                        new AckMessage(pumpSession, MedtronicSendMessageRequestMessage.MessageType.INITIATE_MULTIPACKET_TRANSFER.response()).send(mDevice);
                        expectedSegments = multipacketSession._packetsToFetch;
                        fetchMoreData = true;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.MULTIPACKET_SEGMENT_TRANSMISSION.response(cmd))
                    {
                        if (multipacketSession == null) throw new UnexpectedMessageException("multipacketSession not initiated before segment received" + tag);
                        multipacketSession.AddSegment(decrypted);
                        expectedSegments--;

                        if (multipacketSession.PayloadComplete())
                        {
                            Log.d(TAG, "*** Multisession Complete");
                            new AckMessage(pumpSession, MedtronicSendMessageRequestMessage.MessageType.MULTIPACKET_SEGMENT_TRANSMISSION.response()).send(mDevice);

                            // read 0412 = EHSM_SESSION(1)
                            payload = ReadMessage(mDevice, READ_TIMEOUT_MS);
                            decrypted = Decode(pumpSession, payload);
                            Log.d(TAG, "*** response" + HexDump.DumpHexstring(decrypted));

                            return multipacketSession._response;
                            //fetchMoreData = false;
                        }
                        else
                            fetchMoreData = true;

                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.END_HISTORY_TRANSMISSION.response(cmd))
                    {
                        Log.d(TAG, "*** END_HISTORY_TRANSMISSION response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = false;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.READ_PUMP_TIME.response(cmd))
                    {
                        Log.d(TAG, "*** READ_PUMP_TIME response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = false;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.READ_PUMP_STATUS.response(cmd))
                    {
                        Log.d(TAG, "*** READ_PUMP_STATUS response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = false;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.READ_HISTORY_INFO.response(cmd))
                    {
                        Log.d(TAG, "*** READ_HISTORY_INFO response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = false;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.READ_BASAL_PATTERN.response(cmd))
                    {
                        Log.d(TAG, "*** READ_BASAL_PATTERN response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = false;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.READ_BOLUS_WIZARD_CARB_RATIOS.response(cmd))
                    {
                        Log.d(TAG, "*** READ_BOLUS_WIZARD_CARB_RATIOS response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = false;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.READ_BOLUS_WIZARD_SENSITIVITY_FACTORS.response(cmd))
                    {
                        Log.d(TAG, "*** READ_BOLUS_WIZARD_SENSITIVITY_FACTORS response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = false;
                    }
                    else if (MedtronicSendMessageRequestMessage.MessageType.READ_BOLUS_WIZARD_BG_TARGETS.response(cmd))
                    {
                        Log.d(TAG, "*** READ_BOLUS_WIZARD_BG_TARGETS response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = false;
                    }
                    else
                    {
                        Log.d(TAG, "*** ??? response" + HexDump.DumpHexstring(decrypted));
                        fetchMoreData = true;
                    }
                }
            }

            // when returning non-multipacket decrypted data we need to trim the 2 byte checksum
            return (decrypted != null ? Arrays.copyOfRange(decrypted, 0, decrypted.Length - 2) : payload);
        }

        // refactor in progress to use constants for payload offsets
        public const int MM_HEADER = 0x0000; // UInt8
        public const int MM_DEVICETYPE = 0x0001; // UInt8
        public const int MM_PUMPSERIAL = 0x0002; // string 6 bytes
        public const int MM_COMMAND = 0x0012; // UInt8
        public const int MM_SEQUENCE = 0x0013; // UInt32LE
        public const int MM_PAYLOAD_SIZE = 0x001C; // UInt16LE
        public const int MM_CRC = 0x0020; // UInt8
        public const int MM_PAYLOAD = 0x0021; // data

        // {MM_PAYLOAD}
        public const int NGP_COMMAND = 0x0000; // UInt8
        public const int NGP_SIZE = 0x0001; // UInt8
        public const int NGP_PAYLOAD = 0x0002; // data
        public const int NGP_CRC = -0x0002; // UInt16LE

        // {NGP_PAYLOAD} when NGP_COMMAND=0x55
        public const int NGP55_00 = 0x0000; // UInt8
        public const int NGP55_06 = 0x0001; // UInt8 (maybe response flag??? seen 02 04 for 0x81 and 06 for 0x80 messages)
        public const int NGP55_PUMP_MAC = 0x0002; // UInt64LE
        public const int NGP55_LINK_MAC = 0x000A; // UInt64LE
        public const int NGP55_SEQUENCE = 0x0012; // UInt8
        public const int NGP55_U00 = 0x0013; // UInt8
        public const int NGP55_U01 = 0x0014; // UInt8
        public const int NGP55_ENCRYPTED_SIZE = 0x0015; // UInt8
        public const int NGP55_PAYLOAD = 0x0016; // data

        // {NGP55_PAYLOAD}
        public const int RESPONSE_SEQUENCE = 0x0000; // UInt8
        public const int RESPONSE_COMMAND = 0x0001; // UInt16BE
        public const int RESPONSE_PAYLOAD = 0x0004; // data
        public const int RESPONSE_CRC = -0x0002; // UInt16BE

        // returns the dycrypted response payload only
        protected byte[] Decode(MedtronicCnlSession pumpSession, byte[] payload)
        {
            if (payload.Length < MM_PAYLOAD + NGP_PAYLOAD + NGP55_PAYLOAD + RESPONSE_PAYLOAD ||
                payload[MM_COMMAND] == (byte)CommandType.READ_INFO ||
                payload[MM_COMMAND] == (byte)CommandType.REQUEST_LINK_KEY_RESPONSE)
            {
                throw new EncryptionException("Message received for decryption wrong type/size");
            }

            byte encryptedPayloadSize = payload[MM_PAYLOAD + NGP_PAYLOAD + NGP55_ENCRYPTED_SIZE];

            if (encryptedPayloadSize == 0)
            {
                throw new EncryptionException("Could not decrypt Medtronic Message (encryptedPayloadSize == 0)");
            }

            byte[] encryptedPayload = new byte[encryptedPayloadSize];
            encryptedPayload.Put(payload, MM_PAYLOAD + NGP_PAYLOAD + NGP55_PAYLOAD, encryptedPayloadSize);
            byte[] decryptedPayload = decrypt(pumpSession.getKey(), pumpSession.getIV(), encryptedPayload);

            if (decryptedPayload == null)
            {
                throw new EncryptionException("Could not decrypt Medtronic Message (decryptedPayload == null)");
            }

#if DEBUG
            string outputstring = HexDump.DumpHexstring(decryptedPayload);
            Log.d(TAG, "DECRYPTED: " + outputstring);
#endif

            return decryptedPayload;
        }

        protected byte[] Decrypt(byte[] key, byte[] iv, byte[] encrypted)
        {
            SecretKeySpec secretKeySpec = new SecretKeySpec(key, "AES");
            IvParameterSpec ivSpec = new IvParameterSpec(iv);
            byte[] decrypted;

            try
            {
                Cipher cipher = Cipher.getInstance("AES/CFB/NoPadding");
                cipher.init(Cipher.DECRYPT_MODE, secretKeySpec, ivSpec);
                decrypted = cipher.doFinal(encrypted);
            }
            catch (Exception e)
            {
                throw new EncryptionException("Could not decrypt Medtronic Message");
            }
            return decrypted;
        }
    }

    internal class MultipacketSession
    {
        private readonly int _sessionSize;
        private readonly int _packetSize;
        private readonly int _lastPacketSize;
        private readonly int _packetsToFetch;
        private readonly bool[] _segments;
        private readonly byte[] _response;

        private MultipacketSession(byte[] settings)
        {
            _sessionSize = read32BEtoInt(settings, 0x0003);
            _packetSize = read16BEtoUInt(settings, 0x0007);
            _lastPacketSize = read16BEtoUInt(settings, 0x0009);
            _packetsToFetch = read16BEtoUInt(settings, 0x000B);
            _response = new byte[_sessionSize + 1];
            _segments = new bool[_packetsToFetch];
            _response[0] = settings[0]; // comDSequenceNumber
            Log.d(TAG, $"*** Starting a new Multipacket Session. Expecting {_sessionSize} bytes of data from {_packetsToFetch} packets");
        }

        private int LastPacketNumber => _packetsToFetch - 1;

        // The number of segments we've actually fetched.
        private int NrSegmentsFilled => _segments.Count(segment => segment);

        private bool PayloadComplete => NrSegmentsFilled == _packetsToFetch;

        private void AddSegment(byte[] data)
        {
            try
            {
                int packetNumber = read16BEtoUInt(data, 0x0003);
                int packetSize = data.Length - 7;
                _segments[packetNumber] = true;

                Log.d(TAG, $"*** Got a Multipacket Segment: {(packetNumber + 1)} of {_packetsToFetch}, count: {NrSegmentsFilled} [packetSize={packetSize} {_packetSize}/{_lastPacketSize}]");

                if (packetNumber == LastPacketNumber &&
                        packetSize != _lastPacketSize)
                {
                    throw new UnexpectedMessageException("Multipacket Transfer last packet size mismatch");
                }
                else if (packetNumber != LastPacketNumber &&
                      packetSize != _packetSize)
                {
                    throw new UnexpectedMessageException("Multipacket Transfer packet size mismatch");
                }

                int to = (packetNumber * _packetSize) + 1;
                int from = 5;
                while (from < packetSize + 5) _response[to++] = data[from++];
            }
            catch (Exception e)
            {
                throw new UnexpectedMessageException("Multipacket Transfer bad segment data received", e);
            }
        }

        private byte[] MissingSegments()
        {
            int packetNumber = 0;
            int missing = 0;
            foreach (var segment in _segments)
            {
                if (segment)
                {
                    if (missing > 0) break;
                    packetNumber++;
                }
                else missing++;
            }

            Log.d(TAG, "*** Request Missing Multipacket Segments, position: " + (packetNumber + 1) + ", missing: " + missing);

            return new byte[] { (byte)(packetNumber >> 8), (byte)packetNumber, (byte)(missing >> 8), (byte)missing };
        }
    }
}

