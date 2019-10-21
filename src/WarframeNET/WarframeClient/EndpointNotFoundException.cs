using System;

namespace WarframeNET
{
    /// <summary>
    /// Thrown when an endpoint is incorrect.
    /// </summary>
    [Serializable]
    public class EndpointNotFoundException : Exception
    {
        /// <inheritdoc />
        public EndpointNotFoundException() { }

        /// <inheritdoc />
        public EndpointNotFoundException(string message) : base (message) { }

        /// <inheritdoc />
        public EndpointNotFoundException(string message, Exception inner) : base (message, inner) { }
    }
}