using System;
using System.Linq;
using System.Text;

namespace ContourNextLink24Manager
{
    internal class HexDump
    {
        internal static string DumpHexstring(byte[] outputBuffer)
        {
            var sb = new StringBuilder();
            foreach(var x in outputBuffer){
                sb.Append(x.ToString("X2"));
            }
            return sb.ToString();
        }

        internal static string ToHexstring(byte v)
        {
            return DumpHexstring(new[] { v });
        }

        internal static string ToHexstring(int v)
        {
            return v.ToString("X4");
        }

        internal static string DumpHexstring(byte[] payload, int v1, int v2)
        {
            throw new NotImplementedException();
        }
    }
}
