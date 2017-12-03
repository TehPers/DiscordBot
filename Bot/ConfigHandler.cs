using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;

namespace Bot {
    public class ConfigHandler {
        private static IConfig DefaultCreate<T>(string s) where T : IConfig, new() => new T();
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        private readonly ConcurrentDictionary<string, IConfig> _globalConfigs = new ConcurrentDictionary<string, IConfig>();
        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, IConfig>> _guildConfigs = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, IConfig>>();

        private readonly string _directory;
        private string GlobalDirectory => Path.Combine(this._directory, "global");
        private string GuildDirectory => Path.Combine(this._directory, "servers");
        private readonly object _ioLock = new object();

        public ConfigHandler(string directory) {
            this._directory = directory;
        }

        #region Get
        private static ConfigWrapper<T> InternalGet<T>(string key, IReadOnlyDictionary<string, IConfig> configs, string path, ConfigWrapper<T> parent = null) where T : IConfig {
            if (configs.TryGetValue(key, out IConfig config) && config is T tConfig)
                return new ConfigWrapper<T>(tConfig, Path.Combine(path, $"{key}.json"), parent);
            return null;
        }

        public ConfigWrapper<T> Get<T>(string key) where T : IConfig {
            return ConfigHandler.InternalGet<T>(key, this._globalConfigs, this.GlobalDirectory);
        }

        public ConfigWrapper<T> Get<T>(string key, IGuild guild) where T : IConfig => this.Get<T>(key, guild.Id);
        public ConfigWrapper<T> Get<T>(string key, ulong guild) where T : IConfig {
            return this._guildConfigs.TryGetValue(guild, out ConcurrentDictionary<string, IConfig> configs) ? ConfigHandler.InternalGet(key, configs, Path.Combine(this.GuildDirectory, guild.ToString()), this.Get<T>(key)) : null;
        }
        #endregion

        #region GetOrCreate
        private static ConfigWrapper<T> InternalGetOrCreate<T>(string key, ConcurrentDictionary<string, IConfig> configs, Func<string, IConfig> createFactory, string path, ConfigWrapper<T> parent = null) where T : IConfig {
            // Try to get existing config, or create one if there is none
            if (configs.GetOrAdd(key, createFactory) is T curConfig)
                return new ConfigWrapper<T>(curConfig, Path.Combine(path, $"{key}.json"), parent);

            // Replace existing config
            Bot.Instance.Log($"Overwriting config {key}", LogSeverity.Warning);
            return new ConfigWrapper<T>((T) configs.AddOrUpdate(key, createFactory, (s, config) => createFactory(s)), Path.Combine(path, $"{key}.json"), parent);
        }

        public ConfigWrapper<T> GetOrCreate<T>(string key) where T : IConfig, new() => this.GetOrCreate<T>(key, ConfigHandler.DefaultCreate<T>);
        public ConfigWrapper<T> GetOrCreate<T>(string key, Func<string, IConfig> createFactory) where T : IConfig {
            return ConfigHandler.InternalGetOrCreate<T>(key, this._globalConfigs, createFactory, this.GlobalDirectory);
        }

        public ConfigWrapper<T> GetOrCreate<T>(string key, IGuild guild) where T : IConfig, new() => this.GetOrCreate<T>(key, guild.Id);
        public ConfigWrapper<T> GetOrCreate<T>(string key, ulong guild) where T : IConfig, new() => this.GetOrCreate<T>(key, guild, ConfigHandler.DefaultCreate<T>);
        public ConfigWrapper<T> GetOrCreate<T>(string key, IGuild guild, Func<string, IConfig> createFactory) where T : IConfig => this.GetOrCreate<T>(key, guild.Id, createFactory);
        public ConfigWrapper<T> GetOrCreate<T>(string key, ulong guild, Func<string, IConfig> createFactory) where T : IConfig {
            ConcurrentDictionary<string, IConfig> configs = this._guildConfigs.GetOrAdd(guild, k => new ConcurrentDictionary<string, IConfig>());
            return ConfigHandler.InternalGetOrCreate(key, configs, createFactory, this.GuildDirectory, this.GetOrCreate<T>(key, createFactory));
        }
        #endregion

        #region IO
        [Obsolete]
        public async Task Save() {
            // Save global configs
            foreach (string configName in this._globalConfigs.Keys) {
                ConfigWrapper<IConfig> config = this.Get<IConfig>(configName);
                if (config != null) {
                    await config.Save();
                }
            }

            // Save guild configs
            foreach (KeyValuePair<ulong, ConcurrentDictionary<string, IConfig>> guildConfigs in this._guildConfigs) {
                foreach (string configName in guildConfigs.Value.Keys) {
                    ConfigWrapper<IConfig> config = this.Get<IConfig>(configName, guildConfigs.Key);
                    if (config != null) {
                        await config.Save();
                    }
                }
            }
        }

        public void Load() {
            this._globalConfigs.Clear();
            this._guildConfigs.Clear();

            lock (this._ioLock) {
                string globalDir = this.GlobalDirectory;

                // Try to load the global config
                if (Directory.Exists(globalDir)) {
                    foreach (string configPath in Directory.EnumerateFiles(globalDir)) {
                        string key = Path.GetFileNameWithoutExtension(configPath);
                        using (StreamReader file = File.OpenText(configPath)) {
                            try {
                                IConfig config = JsonConvert.DeserializeObject<IConfig>(file.ReadToEnd(), ConfigHandler.SerializerSettings);
                                this._globalConfigs.AddOrUpdate(key, config, (k, v) => config);
                            } catch {
                                Bot.Instance.Log($"Failed to load global config {key}", LogSeverity.Error);
                            }
                        }
                    }
                }

                // Try to load guild configs
                string guildDirs = this.GuildDirectory;
                if (Directory.Exists(guildDirs)) {
                    foreach (string guildDir in Directory.EnumerateDirectories(guildDirs)) {
                        // Get guild ID
                        if (!ulong.TryParse(Path.GetFileName(guildDir), out ulong guild)) {
                            Bot.Instance.Log($"Invalid guild config directory, expected <guild_id>: {guildDir}", LogSeverity.Error);
                            continue;
                        }

                        // Try to load the guild configs
                        ConcurrentDictionary<string, IConfig> guildConfigs = this._guildConfigs.GetOrAdd(guild, k => new ConcurrentDictionary<string, IConfig>());
                        foreach (string configPath in Directory.EnumerateFiles(guildDir)) {
                            string key = Path.GetFileNameWithoutExtension(configPath);

                            using (StreamReader file = File.OpenText(configPath)) {
                                try {
                                    IConfig config = JsonConvert.DeserializeObject<IConfig>(file.ReadToEnd(), ConfigHandler.SerializerSettings);
                                    guildConfigs.AddOrUpdate(key, config, (k, v) => config);
                                } catch {
                                    Bot.Instance.Log($"Failed to load config {key} for guild {guild}", LogSeverity.Error);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Helpers
        public static bool IsValidKey(string key) => key.IndexOfAny(Path.GetInvalidFileNameChars()) != -1;
        #endregion

        /// <summary>A temporary wrapper for configs. Do not cache objects of this type.</summary>
        /// <typeparam name="TConfig">The type of config this wraps</typeparam>
        public class ConfigWrapper<TConfig> where TConfig : IConfig {
            private readonly TConfig _config;
            private readonly ConfigWrapper<TConfig> _parent;
            private readonly string _path;
            private bool _dirty;

            public ConfigWrapper(TConfig config, string path, ConfigWrapper<TConfig> parent = null) {
                this._config = config;
                this._path = path;
                this._parent = parent;
            }

            #region Getters
            /// <summary>Gets a value from the config without checking parent configs</summary>
            /// <typeparam name="T">The type of value to get</typeparam>
            /// <param name="getter">A function that returns the value from the config</param>
            /// <returns>The value returned from <see cref="getter"/></returns>
            public T GetValueRaw<T>(Func<TConfig, T> getter) {
                lock (this._config) {
                    return getter(this._config);
                }
            }

            /// <summary>Gets a value from the config, or parent config if null</summary>
            /// <typeparam name="T">The type of value to get</typeparam>
            /// <param name="getter">A function that returns the value from the config</param>
            /// <returns>The value returned from <see cref="getter"/></returns>
            public T GetValue<T>(Func<TConfig, T> getter) where T : class {
                lock (this._config) {
                    return getter(this._config) ?? this._parent?.GetValue(getter);
                }
            }

            /// <summary>Gets a value from the config, or parent config if null</summary>
            /// <typeparam name="T">The type of value to get</typeparam>
            /// <param name="getter">A function that returns the value from the config</param>
            /// <returns>The value returned from <see cref="getter"/></returns>
            public T? GetValue<T>(Func<TConfig, T?> getter) where T : struct {
                lock (this._config) {
                    return getter(this._config) ?? this._parent?.GetValue(getter);
                }
            }
            #endregion

            #region Setters
            /// <summary>Sets values in this config</summary>
            /// <param name="setter">A function that sets values or performs operations on the config</param>
            public ConfigWrapper<TConfig> SetValue(Action<TConfig> setter) {
                lock (this._config) {
                    this._dirty = true;
                    setter(this._config);
                }

                return this;
            }
            #endregion

            #region IO
            public async Task Save() {
                // Don't try to save if not necessary
                if (!this._dirty)
                    return;

                //Bot.Instance.Config.Save();
                Directory.CreateDirectory(Path.GetDirectoryName(this._path));
                using (FileStream stream = File.Open(this._path, FileMode.OpenOrCreate, FileAccess.Write)) {
                    string serialized;
                    lock (this._config)
                        serialized = JsonConvert.SerializeObject(this._config, Formatting.Indented, ConfigHandler.SerializerSettings);

                    byte[] data = Encoding.UTF8.GetBytes(serialized);
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
            #endregion
        }
    }
}
