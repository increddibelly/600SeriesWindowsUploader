using System.Collections.Generic;
using System.Linq;

namespace ContourNextLink24Manager
{
    public static class Extensions
    {
        public static void Put<T>(this T[] source, IEnumerable<T> data, int readOffset = 0, int itemsToWrite = 0)
        {
            var addr = 0;
            if (itemsToWrite == 0) 
                itemsToWrite = data.Count();

            foreach (var t in data.Skip(readOffset).Take(itemsToWrite))
            {
                source.Put(t, addr++);
            }
        }

        public static void Put<T>(this T[] source, T t, int addr=0)
        {
            source[addr] = t;
        }
    }
}
