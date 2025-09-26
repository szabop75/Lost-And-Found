# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution files
COPY backend/LostAndFound.Api/LostAndFound.Api.csproj backend/LostAndFound.Api/
COPY backend/LostAndFound.Infrastructure/LostAndFound.Infrastructure.csproj backend/LostAndFound.Infrastructure/
COPY backend/LostAndFound.Domain/LostAndFound.Domain.csproj backend/LostAndFound.Domain/

RUN dotnet restore backend/LostAndFound.Api/LostAndFound.Api.csproj

# Copy the rest of the source
COPY backend/ backend/

# Publish
RUN dotnet publish backend/LostAndFound.Api/LostAndFound.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Set binding to internal port 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Expose container port 8080 (host port set in compose)
EXPOSE 8080

ENTRYPOINT ["dotnet", "LostAndFound.Api.dll"]
