using System;

namespace hhnl.HomeAssistantNet.Shared.Utils
{
    public class StateInvalidException : Exception
    {
        public StateInvalidException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }
}
