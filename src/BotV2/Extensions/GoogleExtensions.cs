using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Newtonsoft.Json;

namespace BotV2.Extensions
{
    public static class GoogleExtensions
    {
        public static IServiceCollection AddGoogleSheets(this IServiceCollection services, IConfiguration config)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.AddSingleton(_ =>
            {
                var initializer = new BaseClientService.Initializer();

                var credentialConfig = config.GetSection("Credential");
                var credentialDict = credentialConfig.GetChildren().ToDictionary(section => section.Key, section => section.Value);
                var serialized = JsonConvert.SerializeObject(credentialDict);
                var credential = GoogleCredential.FromJson(serialized).CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

                config.Bind(initializer);
                initializer.HttpClientInitializer = credential;
                return initializer;
            });
            services.AddSingleton(serviceProvider => new SheetsService(serviceProvider.GetRequiredService<BaseClientService.Initializer>()));
            
            return services;
        }
    }
}
