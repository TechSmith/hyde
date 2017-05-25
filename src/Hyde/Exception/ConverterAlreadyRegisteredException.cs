using System;

namespace Hyde
{
    [Serializable]
    public class ConverterAlreadyRegisteredException : Exception
    {
        public ConverterAlreadyRegisteredException()
        {
        }

        public ConverterAlreadyRegisteredException( string message )
          : base( message )
        {
        }

        public ConverterAlreadyRegisteredException( string message, Exception innerException )
          : base( message, innerException )
        {
        }
    }
}