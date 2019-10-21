FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS runtime
EXPOSE 443/tcp
EXPOSE 50000-65535/udp

# Restore NuGet packages
FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS restore
WORKDIR /src
COPY src/Bot.sln Bot.sln
COPY src/BotV2/BotV2.csproj BotV2/BotV2.csproj
RUN dotnet restore

# Build solution
FROM restore AS build
WORKDIR /src
COPY src .
ARG CONFIGURATION=Release
RUN dotnet build -c ${CONFIGURATION}
RUN dotnet test -c ${CONFIGURATION}
RUN dotnet publish -c ${CONFIGURATION} -o publish_output

# Runtime
FROM runtime
WORKDIR /out
COPY --from=build /src/publish_output .
ENTRYPOINT ["dotnet", "BotV2.dll"]
