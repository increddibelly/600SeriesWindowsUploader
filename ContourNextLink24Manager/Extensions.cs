using System.Linq;
using System.Threading.Tasks;

namespace ContourNextLink24Manager
{
    public static class Extensions
    {
        public static byte[] Partial(this byte[] buffer, int startIndex, int length)
        {
            return buffer.Skip(startIndex).Take(length).ToArray();
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int millisecondsTimeout)
        {
            // if the task completes BEFORE the delay task completes, timeout has not happened.
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)))
                return await task;
            else
                throw new TimeoutException();
        }
        public static async Task TimeoutAfter(this Task task, int millisecondsTimeout)
        {
            // if the task completes BEFORE the delay task completes, timeout has not happened.
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)))
                await task;
            else
                throw new TimeoutException();
        }
    }
}
