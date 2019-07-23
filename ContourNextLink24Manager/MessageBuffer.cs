using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("WindowsUploader.Tests")]
namespace ContourNextLink24Manager
{
    public abstract class MessageBuffer
    {
        protected byte[] _buffer;
        private int _writeIndex = 0;
        protected Encoding _encoding = Encoding.Default;
        public int Length => _buffer.Length;

        public byte[] ToArray() => _buffer.ToArray();

        protected MessageBuffer() { }

        // implicit conversion to byte array
        public static implicit operator byte[](MessageBuffer mBuf)
        {
            return mBuf._buffer.ToArray();
        }

        // allow direct access to the array
        protected  byte this[int index]{
            get { return _buffer[index]; }
            set { _buffer[index] = value; }
        }

        public override string ToString()
        {
            return _encoding.GetString(_buffer);
        }

        public void Put(byte value)
        {
            _buffer[_writeIndex++] = value;
        }

        public void Put(byte[] source, int sourceOffset=0, int itemCount=0)
        {
            var itemsToWrite = source.Skip(sourceOffset);
            
            // take all items by default
            if (itemCount > 0)
                itemsToWrite = itemsToWrite.Take(itemCount);

            foreach (var item in itemsToWrite)
            {
                Put(item);
            }
        }

        public bool StartsWith(byte[] other)
        {
            // take the first N bytes from this buffer, and verify their sequential values are equal
            return Length >= other.Length && 
                  _buffer.Take(other.Length).SequenceEqual(other);
        }
    }

    public class MedtronicMessageBuffer : MessageBuffer
    {
        protected new Encoding _encoding = Encoding.ASCII;

        public MedtronicMessageBuffer(int size)
        {
            _buffer = new byte[size];
        }

        public MedtronicMessageBuffer(byte[] source)
        {
            _buffer = source.ToArray();
        }

        public MedtronicMessageBuffer(string source)
        {
            _buffer = _encoding.GetBytes(source);
        }

        // bytes 0..3 = ABC
        // byte  4    = message body size (max 60)
        // bytes 5..n = message body (n = message body size, the value in byte 4)
        public int MessageLength => _buffer[3];
    }
}
