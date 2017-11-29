using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, IConfig>> _serverConfigs = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, IConfig>>();

        private readonly string _directory;
        private string GlobalDirectory => Path.Combine(this._directory, "global");
        private string ServerDirectory => Path.Combine(this._directory, "servers");
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

        public ConfigWrapper<T> Get<T>(string key, IGuild server) where T : IConfig => this.Get<T>(key, server.Id);
        public ConfigWrapper<T> Get<T>(string key, ulong server) where T : IConfig {
            return this._serverConfigs.TryGetValue(server, out ConcurrentDictionary<string, IConfig> configs) ? this.InternalGet(key, configs, this.Get<T>(key)) : null;
        }
        #endregion

        #region GetOrCreate
        private ConfigWrapper<T> InternalGetOrCreate<T>(string key, ConcurrentDictionary<string, IConfig> configs, Func<string, IConfig> createFactory, ConfigWrapper<T> parent = null) where T : IConfig {
            if (configs.GetOrAdd(key, createFactory) is T tConfig)
                return new ConfigWrapper<T>(tConfig, this, parent);
            return null;
        }

        public ConfigWrapper<T> GetOrCreate<T>(string key) where T : IConfig, new() => this.GetOrCreate<T>(key, ConfigHandler.DefaultCreate<T>);
        public ConfigWrapper<T> GetOrCreate<T>(string key, Func<string, IConfig> createFactory) where T : IConfig {
            return this.InternalGetOrCreate<T>(key, this._globalConfigs, createFactory);
        }

        public ConfigWrapper<T> GetOrCreate<T>(string key, IGuild server) where T : IConfig, new() => this.GetOrCreate<T>(key, server.Id);
        public ConfigWrapper<T> GetOrCreate<T>(string key, ulong server) where T : IConfig, new() => this.GetOrCreate<T>(key, server, ConfigHandler.DefaultCreate<T>);
        public ConfigWrapper<T> GetOrCreate<T>(string key, IGuild server, Func<string, IConfig> createFactory) where T : IConfig => this.GetOrCreate<T>(key, server.Id, createFactory);
        public ConfigWrapper<T> GetOrCreate<T>(string key, ulong server, Func<string, IConfig> createFactory) where T : IConfig {
            ConcurrentDictionary<string, IConfig> configs = this._serverConfigs.GetOrAdd(server, k => new ConcurrentDictionary<string, IConfig>());
            return this.InternalGetOrCreate(key, configs, createFactory, this.GetOrCreate<T>(key, createFactory));
        }
        #endregion

        #region Set

        #endregion

        #region IO
        public void Load() {
            this._dirty.Clear();
            this._globalConfigs.Clear();
            this._serverConfigs.Clear();

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

                // Try to load server configs
                string serversDir = this.ServerDirectory;
                if (Directory.Exists(serversDir)) {
                    foreach (string serverDir in Directory.EnumerateDirectories(serversDir)) {
                        // Get server ID
                        if (!ulong.TryParse(Path.GetFileName(serverDir), out ulong server)) {
                            Bot.Instance.Log($"Invalid server config directory, expected <server_id>: {serverDir}", LogSeverity.Error);
                            continue;
                        }

                        // Try to load the server configs
                        ConcurrentDictionary<string, IConfig> serverConfigs = this._serverConfigs.GetOrAdd(server, k => new ConcurrentDictionary<string, IConfig>());
                        foreach (string configPath in Directory.EnumerateFiles(serverDir)) {
                            string key = Path.GetFileNameWithoutExtension(configPath);

                            using (StreamReader file = File.OpenText(configPath)) {
                                try {
                                    IConfig config = JsonConvert.DeserializeObject<IConfig>(file.ReadToEnd(), ConfigHandler.SerializerSettings);
                                    serverConfigs.AddOrUpdate(key, config, (k, v) => config);
                                } catch {
                                    Bot.Instance.Log($"Failed to load config {key} for server {server}", LogSeverity.Error);
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

                // Write server configs
                foreach (KeyValuePair<ulong, ConcurrentDictionary<string, IConfig>> configsKV in this._serverConfigs) {
                    // Make sure directory exists
                    string dir = Path.Combine(this.ServerDirectory, configsKV.Key.ToString());
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
                File.WriteAllText(path, serialized);
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
            /// <param name="getValue">A function that returns the value from the config</param>
            /// <returns>The value returned from <see cref="getValue"/></returns>
            public T GetValueRaw<T>(Func<TConfig, T> getValue) {
                lock (this._config) {
                    return getValue(this._config);
                }
            }

            /// <summary>Gets a value from the config, or parent config if null</summary>
            /// <typeparam name="T">The type of value to get</typeparam>
            /// <param name="getValue">A function that returns the value from the config</param>
            /// <returns>The value returned from <see cref="getValue"/></returns>
            public T GetValue<T>(Func<TConfig, T> getValue) where T : class {
                lock (this._config) {
                    return getValue(this._config) ?? this._parent?.GetValue(getValue);
                }
            }

            /// <summary>Gets a value from the config, or parent config if null</summary>
            /// <typeparam name="T">The type of value to get</typeparam>
            /// <param name="getValue">A function that returns the value from the config</param>
            /// <returns>The value returned from <see cref="getValue"/></returns>
            public T? GetValue<T>(Func<TConfig, T?> getValue) where T : struct {
                lock (this._config) {
                    return getValue(this._config) ?? this._parent?.GetValue(getValue);
                }
            }
            #endregion

            #region Setters
            /// <summary>Sets values in this config</summary>
            /// <param name="setValue">A function that sets values or performs operations on the config</param>
            public void SetValue(Action<TConfig> setValue) {
                lock (this._config) {
                    this._handler._dirty.Add(this._config);
                    setValue(this._config);
                }
            }
            #endregion
        }
    }
}
