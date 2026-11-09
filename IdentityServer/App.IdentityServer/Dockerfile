FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["IdentityServer/App.IdentityServer/App.IdentityServer.csproj", "IdentityServer/App.IdentityServer/"]
RUN dotnet restore "IdentityServer/App.IdentityServer/App.IdentityServer.csproj"
COPY ["IdentityServer/App.IdentityServer/", "IdentityServer/App.IdentityServer/"]
WORKDIR /src/IdentityServer/App.IdentityServer
RUN dotnet publish "App.IdentityServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
RUN mkdir -p /home/app/keys
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "App.IdentityServer.dll"]
