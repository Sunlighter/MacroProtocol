using System;

namespace Sunlighter.MacroProtocol
{
#if NETSTANDARD2_0
    [Serializable]
#endif
    public class MacroProtocolException : Exception
    {
        public MacroProtocolException() : base() { }

        public MacroProtocolException(string message) : base(message) { }

        public MacroProtocolException(string message, Exception innerException) : base(message, innerException) { }

#if NETSTANDARD2_0
        protected MacroProtocolException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
