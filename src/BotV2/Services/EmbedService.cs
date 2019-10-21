using DSharpPlus.Entities;

namespace BotV2.Services
{
    public class EmbedService
    {
        public DiscordEmbedBuilder CreateStandardEmbed()
        {
            var builder = new DiscordEmbedBuilder();
            this.AddStandardFooter(builder);
            return builder;
        }

        private void AddStandardFooter(DiscordEmbedBuilder builder)
        {
            // builder.WithFooter($"Timestamp: {DateTimeOffset.UtcNow:O}");
        }
    }
}
