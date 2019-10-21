using System;

namespace BotV2.Models
{
    public class CommandModuleRegistration
    {
        public Type CommandModuleType { get; }

        public CommandModuleRegistration(Type commandModuleType)
        {
            this.CommandModuleType = commandModuleType;
        }
    }
}