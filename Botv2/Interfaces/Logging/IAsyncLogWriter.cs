using System.Threading.Tasks;
using Discord;

namespace Botv2.Interfaces.Logging {
    public interface IAsyncLogWriter {
        /// <summary>Writes a message to the log.</summary>
        /// <param name="message"></param>
        Task WriteMessage(ILogMessage message);
    }
}