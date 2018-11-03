using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Botv2.Interfaces.Client {
    public interface IDiscordClientHandler {
        Task Start();
        Task Stop();
    }
}
