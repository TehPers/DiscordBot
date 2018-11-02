using System.Threading.Tasks;

namespace TehBot.Core {
    public interface IBot {
        Task Start();
        Task Stop();
    }
}