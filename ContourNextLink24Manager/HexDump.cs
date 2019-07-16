using System.Text;

namespace ContourNextLink24Manager
{
    internal class HexDump
    {
        internal static string DumpHexstring(byte[] outputBuffer)
        {
            return Encoding.ASCII.GetString(outputBuffer);
        }

        internal static string ToHexstring(byte v)
        {
           return DumpHexstring(new [] { v });
        }
    }
}
