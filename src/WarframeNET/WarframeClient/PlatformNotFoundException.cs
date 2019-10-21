using System;

namespace WarframeNET
{
    /// <summary>
    /// Thrown when a platform isn't valid.
    /// </summary>
    [Serializable]
    public class PlatformNotFoundException : Exception
    {
        /// <inheritdoc />
        public PlatformNotFoundException() { }

        /// <inheritdoc />
        public PlatformNotFoundException(string message) : base(message) { }

        /// <inheritdoc />
        public PlatformNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}