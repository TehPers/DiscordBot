using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace CoreTest.Commands {
    public class MockGuild : IGuild {
        public Task DeleteAsync(RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public ulong Id { get; } = 0;
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
        public Task ModifyAsync(Action<GuildProperties> func, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task ModifyEmbedAsync(Action<GuildEmbedProperties> func, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task ReorderChannelsAsync(IEnumerable<ReorderChannelProperties> args, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task ReorderRolesAsync(IEnumerable<ReorderRoleProperties> args, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task LeaveAsync(RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IBan>> GetBansAsync(RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task AddBanAsync(IUser user, int pruneDays = 0, string reason = null, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task AddBanAsync(ulong userId, int pruneDays = 0, string reason = null, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task RemoveBanAsync(IUser user, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task RemoveBanAsync(ulong userId, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IGuildChannel>> GetChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IGuildChannel> GetChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<ITextChannel>> GetTextChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<ITextChannel> GetTextChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IVoiceChannel>> GetVoiceChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IVoiceChannel> GetVoiceChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IVoiceChannel> GetAFKChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<ITextChannel> GetDefaultChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IGuildChannel> GetEmbedChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<ITextChannel> CreateTextChannelAsync(string name, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IVoiceChannel> CreateVoiceChannelAsync(string name, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IGuildIntegration>> GetIntegrationsAsync(RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IGuildIntegration> CreateIntegrationAsync(ulong id, string type, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync(RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public IRole GetRole(ulong id) {
            throw new NotImplementedException();
        }

        public Task<IRole> CreateRoleAsync(string name, GuildPermissions? permissions = null, Color? color = null, bool isHoisted = false, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IGuildUser>> GetUsersAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IGuildUser> GetUserAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IGuildUser> GetCurrentUserAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task<IGuildUser> GetOwnerAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public Task DownloadUsersAsync() {
            throw new NotImplementedException();
        }

        public Task<int> PruneUsersAsync(int days = 30, bool simulate = false, RequestOptions options = null) {
            throw new NotImplementedException();
        }

        public string Name { get; } = "Mock";
        public int AFKTimeout { get; } = 100;
        public bool IsEmbeddable { get; } = true;
        public DefaultMessageNotifications DefaultMessageNotifications { get; } = DefaultMessageNotifications.MentionsOnly;
        public MfaLevel MfaLevel { get; } = MfaLevel.Disabled;
        public VerificationLevel VerificationLevel { get; } = VerificationLevel.None;
        public string IconId { get; } = null;
        public string IconUrl { get; } = null;
        public string SplashId { get; } = null;
        public string SplashUrl { get; } = null;
        public bool Available { get; } = true;
        public ulong? AFKChannelId { get; } = null;
        public ulong DefaultChannelId { get; } = 0;
        public ulong? EmbedChannelId { get; } = null;
        public ulong OwnerId { get; } = 0;
        public string VoiceRegionId { get; } = null;
        public IAudioClient AudioClient { get; } = null;
        public IRole EveryoneRole { get; } = null;
        public IReadOnlyCollection<GuildEmote> Emotes { get; } = new GuildEmote[0];
        public IReadOnlyCollection<string> Features { get; } = new string[0];
        public IReadOnlyCollection<IRole> Roles { get; } = new IRole[0];
    }
}
