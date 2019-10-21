using System;
using Discord;
using Newtonsoft.Json;

namespace Bot.Helpers {
    public class TimedMessageInfo : MessageInfo {
        public bool Expired { get; set; }
        public DateTimeOffset? ExpireTime { get; set; }
        public DateTimeOffset? DeleteTime { get; set; }

        [JsonIgnore]
        public virtual bool ShouldExpire => !this.Expired && this.ExpireTime != null && DateTime.Now >= this.ExpireTime;

        [JsonIgnore]
        public virtual bool ShouldDelete => this.DeleteTime != null && DateTime.Now >= this.DeleteTime;
    }
}