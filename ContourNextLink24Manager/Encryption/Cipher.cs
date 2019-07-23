using System;

namespace ContourNextLink24Manager.Encryption
{
    internal class Cipher
    {
        public static object DECRYPT_MODE { get; internal set; }

        internal static Cipher getInstance(string v)
        {
            throw new NotImplementedException();
        }

        internal void init(object DECRYPT_MODE, SecretKeySpec secretKeySpec, IvParameterSpec ivSpec)
        {
            throw new NotImplementedException();
        }

        internal byte[] doFinal(byte[] encrypted)
        {
            throw new NotImplementedException();
        }
    }
}