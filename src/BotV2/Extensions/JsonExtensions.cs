using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BotV2.Services.JsonConverters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

namespace BotV2.Extensions
{
    public static class JsonExtensions
    {
        public static IServiceCollection AddJsonSerializer(this IServiceCollection services)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton(serviceProvider =>
            {
                var jsonConverters = serviceProvider.GetServices<JsonConverter>();
                var settings = new JsonSerializerSettings();
                foreach (var jsonConverter in jsonConverters)
                {
                    settings.Converters.Add(jsonConverter);
                }

                return settings;
            });

            services.TryAddSingleton(serviceProvider =>
            {
                var settings = serviceProvider.GetRequiredService<JsonSerializerSettings>();
                return JsonSerializer.CreateDefault(settings);
            });

            services.TryAddEnumerable(new[] {ServiceDescriptor.Singleton<JsonConverter, DiscordColorJsonConverter>()});

            return services;
        }

        [return: MaybeNull]
        public static T FromString<T>(this JsonSerializer serializer, string value)
        {
            _ = value ?? throw new ArgumentNullException(nameof(value));
            _ = serializer ?? throw new ArgumentNullException(nameof(serializer));

            using var reader = new StringReader(value);
            using var jsonReader = new JsonTextReader(reader);
            return serializer.Deserialize<T>(jsonReader);
        }

        public static string ToString<T>(this JsonSerializer serializer, [MaybeNull] T value)
        {
            _ = serializer ?? throw new ArgumentNullException(nameof(serializer));

            using var writer = new StringWriter();
            using var jsonWriter = new JsonTextWriter(writer);
            serializer.Serialize(jsonWriter, value);
            return writer.ToString();
        }
    }
}