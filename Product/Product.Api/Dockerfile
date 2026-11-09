FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["Product/Product.Api/Product.Api.csproj", "Product/Product.Api/"]
COPY ["Product/Product.Application/Product.Application.csproj", "Product/Product.Application/"]
COPY ["Product/Product.Domain/Product.Domain.csproj", "Product/Product.Domain/"]
COPY ["Product/Product.Infrastructure/Product.Infrastructure.csproj", "Product/Product.Infrastructure/"]
RUN dotnet restore "Product/Product.Api/Product.Api.csproj"
COPY ["Product/", "Product/"]
WORKDIR /src/Product/Product.Api
RUN dotnet publish "Product.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Product.Api.dll"]
