namespace ContourNextLink24Manager.Encryption
{
    internal class SecretKeySpec
    {
        private readonly byte[] _key;
        private readonly string _v;

        public SecretKeySpec(byte[] key, string v)
        {
            _key = key;
            _v = v;
        }
    }
}