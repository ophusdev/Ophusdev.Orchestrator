# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NuGet.config", "."]
COPY ["Ophusdev.Orchestrator.Api/Ophusdev.Orchestrator.Api.csproj", "Ophusdev.Orchestrator.Api/"]
COPY ["Ophusdev.Orchestrator.Business/Ophusdev.Orchestrator.Business.csproj", "Ophusdev.Orchestrator.Business/"]
COPY ["Ophusdev.Orchestrator.Repository/Ophusdev.Orchestrator.Repository.csproj", "Ophusdev.Orchestrator.Repository/"]
COPY ["Ophusdev.Orchestrator.Shared/Ophusdev.Orchestrator.Shared.csproj", "Ophusdev.Orchestrator.Shared/"]

RUN dotnet restore "./Ophusdev.Orchestrator.Api/Ophusdev.Orchestrator.Api.csproj"

COPY . .
WORKDIR "/src/Ophusdev.Orchestrator.Api"
RUN dotnet build "./Ophusdev.Orchestrator.Api.csproj" -c %BUILD_CONFIGURATION% -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ophusdev.Orchestrator.Api.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ophusdev.Orchestrator.Api.dll"]