namespace ContourNextLink24Manager.Encryption
{
    internal class IvParameterSpec
    {
        private readonly byte[] _iv;

        public IvParameterSpec(byte[] iv)
        {
            _iv = iv;
        }
    }
}