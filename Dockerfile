# Multi-stage Dockerfile for EnterpriseFlow API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for caching restore layer
COPY ["EnterpriseFlow.sln", "./"]
COPY ["src/EnterpriseFlow.Domain/EnterpriseFlow.Domain.csproj", "src/EnterpriseFlow.Domain/"]
COPY ["src/EnterpriseFlow.Application/EnterpriseFlow.Application.csproj", "src/EnterpriseFlow.Application/"]
COPY ["src/EnterpriseFlow.Infrastructure/EnterpriseFlow.Infrastructure.csproj", "src/EnterpriseFlow.Infrastructure/"]
COPY ["src/EnterpriseFlow.Api/EnterpriseFlow.Api.csproj", "src/EnterpriseFlow.Api/"]
COPY ["tests/EnterpriseFlow.Tests/EnterpriseFlow.Tests.csproj", "tests/EnterpriseFlow.Tests/"]

# Restore packages
RUN dotnet restore "src/EnterpriseFlow.Api/EnterpriseFlow.Api.csproj"

# Copy full source tree
COPY . .

# Build and publish release binaries
WORKDIR "/src/src/EnterpriseFlow.Api"
RUN dotnet publish "EnterpriseFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage (lightweight Alpine Linux image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Install curl for health monitoring
RUN apk --no-cache add curl

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EnterpriseFlow.Api.dll"]
