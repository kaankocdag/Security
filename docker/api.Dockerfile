FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props Kaan.SecurityPlatform.slnx ./
COPY src/Kaan.SecurityPlatform.Domain/*.csproj src/Kaan.SecurityPlatform.Domain/
COPY src/Kaan.SecurityPlatform.Application/*.csproj src/Kaan.SecurityPlatform.Application/
COPY src/Kaan.SecurityPlatform.Infrastructure/*.csproj src/Kaan.SecurityPlatform.Infrastructure/
COPY src/Kaan.SecurityPlatform.Api/*.csproj src/Kaan.SecurityPlatform.Api/
COPY src/Kaan.SecurityPlatform.ScannerWorker/*.csproj src/Kaan.SecurityPlatform.ScannerWorker/
COPY tests/Kaan.SecurityPlatform.UnitTests/*.csproj tests/Kaan.SecurityPlatform.UnitTests/
COPY tests/Kaan.SecurityPlatform.IntegrationTests/*.csproj tests/Kaan.SecurityPlatform.IntegrationTests/

RUN dotnet restore src/Kaan.SecurityPlatform.Api/Kaan.SecurityPlatform.Api.csproj

COPY . .
RUN dotnet publish src/Kaan.SecurityPlatform.Api/Kaan.SecurityPlatform.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    KESTREL_TRANSPORT=Sockets

RUN apt-get update && apt-get install -y --no-install-recommends libicu76 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./
RUN mkdir -p /app/wwwroot/uploads/knowledge

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --retries=5 CMD curl -fsS http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "Kaan.SecurityPlatform.Api.dll"]
