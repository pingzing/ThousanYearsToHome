using System;
using System.Runtime.Serialization;

namespace ThousandYearsHome.Extensions.Exceptions
{
    class SignalAwaitCountExceeded : Exception
    {
        public SignalAwaitCountExceeded()
        {
        }

        public SignalAwaitCountExceeded(string message) : base(message)
        {
        }

        public SignalAwaitCountExceeded(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SignalAwaitCountExceeded(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
