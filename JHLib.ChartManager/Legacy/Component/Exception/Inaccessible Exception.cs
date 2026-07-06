namespace Legacy.ECM_Core.Component
{
    public class InaccessibleException : Exception
    {
        public InaccessibleException() : base() { }

        public InaccessibleException(string message) : base(message) { }

        public InaccessibleException(string message, Exception innerException) : base(message, innerException) { }
    }
}