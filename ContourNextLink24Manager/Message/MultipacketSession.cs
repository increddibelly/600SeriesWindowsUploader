using System;
using System.Linq;

namespace ContourNextLink24Manager.Message
{
    internal class MultipacketSession
    {
        private readonly int _sessionSize;
        private readonly int _packetSize;
        private readonly int _lastPacketSize;
        private readonly bool[] _segments;
        private readonly byte[] _response;
        internal readonly int PacketsToFetch;
        private Logger Log;

        private static string TAG => System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;

        internal MultipacketSession(byte[] settings)
        {
            _sessionSize = read32BEtoInt(settings, 0x0003);
            _packetSize = read16BEtoUInt(settings, 0x0007);
            _lastPacketSize = read16BEtoUInt(settings, 0x0009);
            PacketsToFetch = read16BEtoUInt(settings, 0x000B);
            _response = new byte[_sessionSize + 1];
            _segments = new bool[PacketsToFetch];
            _response[0] = settings[0]; // comDSequenceNumber
            Log.d(TAG, $"*** Starting a new Multipacket Session. Expecting {_sessionSize} bytes of data from {PacketsToFetch} packets");
        }

        private int LastPacketNumber => PacketsToFetch - 1;

        // The number of segments we've actually fetched.
        private int NrSegmentsFilled => _segments.Count(segment => segment);

        internal bool PayloadComplete => NrSegmentsFilled == PacketsToFetch;

        internal void AddSegment(byte[] data)
        {
            try
            {
                int packetNumber = read16BEtoUInt(data, 0x0003);
                int packetSize = data.Length - 7;
                _segments[packetNumber] = true;

                Log.d(TAG, $"*** Got a Multipacket Segment: {(packetNumber + 1)} of {PacketsToFetch}, count: {NrSegmentsFilled} [packetSize={packetSize} {_packetSize}/{_lastPacketSize}]");

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

        private int read16BEtoUInt(byte[] data, int v)
        {
            throw new NotImplementedException();
        }

        private int read32BEtoInt(byte[] settings, int v)
        {
            throw new NotImplementedException();
        }

        internal byte[] MissingSegments()
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

        internal byte[] Response => _response.ToArray();
    }
}

