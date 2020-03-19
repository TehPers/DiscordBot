FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime

# Restore NuGet packages
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS restore
WORKDIR /build
COPY src/Bot.sln src/Bot.sln
COPY src/BotV2/BotV2.csproj src/BotV2/BotV2.csproj
COPY Warframe.NET/src/Warframe/Warframe.csproj Warframe.NET/src/Warframe/Warframe.csproj
COPY Warframe.NET/src/Warframe.World/Warframe.World.csproj Warframe.NET/src/Warframe.World/Warframe.World.csproj
RUN dotnet restore src/Bot.sln

# Build solution
FROM restore AS build
WORKDIR /build
COPY src src
COPY Warframe.NET/src Warframe.NET/src
ARG CONFIGURATION=Release
RUN dotnet build -c ${CONFIGURATION} src/Bot.sln
RUN dotnet test -c ${CONFIGURATION} src/Bot.sln
RUN dotnet publish -c ${CONFIGURATION} -o publish_output src/BotV2/BotV2.csproj

# Runtime
FROM runtime
WORKDIR /out
COPY --from=build /build/publish_output .
ENTRYPOINT ["dotnet", "BotV2.dll"]
