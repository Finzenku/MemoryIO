using System.Runtime.InteropServices;
using System.Text;

namespace MemoryIO
{
    public abstract class BaseMemoryIO : IMemoryIO
    {
        public abstract byte[] ReadData(IntPtr address, int dataLength);
        public T Read<T>(IntPtr address) where T : unmanaged => MemoryMarshal.Cast<byte, T>(ReadData(address, Marshal.SizeOf<T>()))[0];
        public T[] ReadArray<T>(IntPtr address, int arrayLength) where T : unmanaged => MemoryMarshal.Cast<byte, T>(ReadData(address, Marshal.SizeOf<T>() * arrayLength)).ToArray();
        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            byte[] buffer = ReadData(address, maxLength);
            ReadOnlySpan<byte> bytes = buffer.AsSpan();

            int nullCharPos = bytes.IndexOf((byte)'\0');
            if (nullCharPos != -1)
                bytes = bytes.Slice(0, nullCharPos);

            return encoding.GetString(bytes);
        }
        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512)
        {
            string encoded = encoding.GetString(ReadData(address, maxLength));
            string[] split = encoded.Split('\0');
            int emptyIndex = Array.IndexOf(split, string.Empty);
            return emptyIndex >= 0 ? split[..emptyIndex] : split;
        }

        public abstract void WriteData(IntPtr address, byte[] data);
        public void Write<T>(IntPtr address, T value) where T : unmanaged => WriteData(address, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref value, 1)).ToArray());
        public void WriteArray<T>(IntPtr address, T[] value) where T : unmanaged => WriteData(address, MemoryMarshal.Cast<T, byte>(value.AsSpan()).ToArray());
        public void WriteString(IntPtr address, string text, Encoding encoding) => WriteData(address, encoding.GetBytes(text + '\0'));
        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding)
        {
            int totalLength = 0;
            foreach (string s in text)
                totalLength += encoding.GetByteCount(s) + 1;

            byte[] buffer = new byte[totalLength];
            int bufferOffset = 0;

            foreach (string s in text)
            {
                int byteCount = encoding.GetBytes(s, buffer.AsSpan(bufferOffset));
                bufferOffset += byteCount + 1;
            }

            WriteData(address, buffer);
        }
    }
}
