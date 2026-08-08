FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props Kaan.SecurityPlatform.slnx ./
COPY src/Kaan.SecurityPlatform.Domain/*.csproj src/Kaan.SecurityPlatform.Domain/
COPY src/Kaan.SecurityPlatform.Application/*.csproj src/Kaan.SecurityPlatform.Application/
COPY src/Kaan.SecurityPlatform.Infrastructure/*.csproj src/Kaan.SecurityPlatform.Infrastructure/
COPY src/Kaan.SecurityPlatform.Api/*.csproj src/Kaan.SecurityPlatform.Api/
COPY src/Kaan.SecurityPlatform.ScannerWorker/*.csproj src/Kaan.SecurityPlatform.ScannerWorker/
COPY src/Kaan.SecurityPlatform.LabWorker/*.csproj src/Kaan.SecurityPlatform.LabWorker/
COPY tests/Kaan.SecurityPlatform.UnitTests/*.csproj tests/Kaan.SecurityPlatform.UnitTests/
COPY tests/Kaan.SecurityPlatform.IntegrationTests/*.csproj tests/Kaan.SecurityPlatform.IntegrationTests/

RUN dotnet restore src/Kaan.SecurityPlatform.LabWorker/Kaan.SecurityPlatform.LabWorker.csproj

COPY . .
RUN dotnet publish src/Kaan.SecurityPlatform.LabWorker/Kaan.SecurityPlatform.LabWorker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libicu76 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

ENTRYPOINT ["dotnet", "Kaan.SecurityPlatform.LabWorker.dll"]
