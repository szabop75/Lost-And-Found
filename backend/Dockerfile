# Backend Dockerfile - ASP.NET Core 8
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy solution and projects
COPY backend/LostAndFound.Domain/*.csproj backend/LostAndFound.Domain/
COPY backend/LostAndFound.Infrastructure/*.csproj backend/LostAndFound.Infrastructure/
COPY backend/LostAndFound.Api/*.csproj backend/LostAndFound.Api/

# restore
RUN dotnet restore backend/LostAndFound.Api/LostAndFound.Api.csproj

# copy source
COPY backend/ ./backend/

# publish
RUN dotnet publish backend/LostAndFound.Api/LostAndFound.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .

# Optionally set environment to Production in container
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LostAndFound.Api.dll"]
