using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private readonly ConcurrentSet<IConfig> _dirty = new ConcurrentSet<IConfig>();

        public ConfigHandler(string directory) {
            this._directory = directory;
        }

        #region Get
        private ConfigWrapper<T> InternalGet<T>(string key, IReadOnlyDictionary<string, IConfig> configs, ConfigWrapper<T> parent) where T : IConfig {
            if (configs.TryGetValue(key, out IConfig config) && config is T tConfig)
                return new ConfigWrapper<T>(tConfig, this, parent);
            return null;
        }

        public ConfigWrapper<T> Get<T>(string key) where T : IConfig {
            return this.InternalGet<T>(key, this._globalConfigs, null);
        }

        public ConfigWrapper<T> Get<T>(string key, IGuild guild) where T : IConfig => this.Get<T>(key, guild.Id);
        public ConfigWrapper<T> Get<T>(string key, ulong guild) where T : IConfig {
            return this._guildConfigs.TryGetValue(guild, out ConcurrentDictionary<string, IConfig> configs) ? this.InternalGet(key, configs, this.Get<T>(key)) : null;
        }
        #endregion

        #region GetOrCreate
        private ConfigWrapper<T> InternalGetOrCreate<T>(string key, ConcurrentDictionary<string, IConfig> configs, Func<string, IConfig> createFactory, ConfigWrapper<T> parent = null) where T : IConfig {
            // Try to get existing config, or create one if there is none
            if (configs.GetOrAdd(key, createFactory) is T curConfig)
                return new ConfigWrapper<T>(curConfig, this, parent);

            // Replace existing config
            Bot.Instance.Log($"Overwriting config {key}");
            return new ConfigWrapper<T>((T) configs.AddOrUpdate(key, createFactory, (s, config) => createFactory(s)), this, parent);
        }

        public ConfigWrapper<T> GetOrCreate<T>(string key) where T : IConfig, new() => this.GetOrCreate<T>(key, ConfigHandler.DefaultCreate<T>);
        public ConfigWrapper<T> GetOrCreate<T>(string key, Func<string, IConfig> createFactory) where T : IConfig {
            return this.InternalGetOrCreate<T>(key, this._globalConfigs, createFactory);
        }

        public ConfigWrapper<T> GetOrCreate<T>(string key, IGuild guild) where T : IConfig, new() => this.GetOrCreate<T>(key, guild.Id);
        public ConfigWrapper<T> GetOrCreate<T>(string key, ulong guild) where T : IConfig, new() => this.GetOrCreate<T>(key, guild, ConfigHandler.DefaultCreate<T>);
        public ConfigWrapper<T> GetOrCreate<T>(string key, IGuild guild, Func<string, IConfig> createFactory) where T : IConfig => this.GetOrCreate<T>(key, guild.Id, createFactory);
        public ConfigWrapper<T> GetOrCreate<T>(string key, ulong guild, Func<string, IConfig> createFactory) where T : IConfig {
            ConcurrentDictionary<string, IConfig> configs = this._guildConfigs.GetOrAdd(guild, k => new ConcurrentDictionary<string, IConfig>());
            return this.InternalGetOrCreate(key, configs, createFactory, this.GetOrCreate<T>(key, createFactory));
        }
        #endregion

        #region Set

        #endregion

        #region IO
        public void Load() {
            this._dirty.Clear();
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

        public void Save() {
            lock (this._ioLock) {
                // Make sure directories exist
                Directory.CreateDirectory(this.GlobalDirectory);

                // Write global config
                foreach (KeyValuePair<string, IConfig> configKV in this._globalConfigs) {
                    ConfigHandler.SaveConfig(Path.Combine(this.GlobalDirectory, $"{configKV.Key}.json"), configKV.Value);
                }

                // Write guild configs
                foreach (KeyValuePair<ulong, ConcurrentDictionary<string, IConfig>> configsKV in this._guildConfigs) {
                    // Make sure directory exists
                    string dir = Path.Combine(this.GuildDirectory, configsKV.Key.ToString());
                    Directory.CreateDirectory(dir);

                    foreach (KeyValuePair<string, IConfig> configKV in configsKV.Value) {
                        ConfigHandler.SaveConfig(Path.Combine(dir, $"{configKV.Key}.json"), configKV.Value);
                    }
                }
            }
        }

        private static void SaveConfig(string path, IConfig config) {
            lock (config) {
                string serialized = JsonConvert.SerializeObject(config, Formatting.Indented, ConfigHandler.SerializerSettings);

                bool success = false;
                int tries = 0;
                while (!success && tries++ < 10) {
                    try {
                        File.WriteAllText(path, serialized);
                        success = true;
                    } catch (IOException) {
                        if (tries >= 10)
                            throw;

                        Thread.Sleep(100);
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
            private readonly ConfigHandler _handler;

            public ConfigWrapper(TConfig config, ConfigHandler handler, ConfigWrapper<TConfig> parent = null) {
                this._config = config;
                this._handler = handler;
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
            public void SetValue(Action<TConfig> setter) {
                lock (this._config) {
                    this._handler._dirty.Add(this._config);
                    setter(this._config);
                }
            }
            #endregion

            #region IO
            public void Save() {
                // TODO: Make the configs able to be saved individually and load them when needed (cache?)
                Bot.Instance.Config.Save();
            }
            #endregion
        }
    }
}
