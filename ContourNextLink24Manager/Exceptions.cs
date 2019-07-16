using System;

namespace ContourNextLink24Manager
{
    public class TimeoutException : Exception {
        public TimeoutException (string message) : base(message) { }
    }

    public class UnexpectedMessageException : Exception {
        public UnexpectedMessageException(string message) : base(message) { }
    }

    public class EncryptionException : Exception
    {
        public EncryptionException(string message) : base(message) { }
        public EncryptionException(string message, Exception e) : base(message, e) { }
    }
}
