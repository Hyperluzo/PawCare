# Multi-stage Dockerfile for PawCare.Api (.NET 10)
# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for optimal docker layer caching
COPY ["PawCare.Api/PawCare.Api.csproj", "PawCare.Api/"]
RUN dotnet restore "PawCare.Api/PawCare.Api.csproj"

# Copy remaining source code and publish
COPY . .
WORKDIR "/src/PawCare.Api"
RUN dotnet publish "PawCare.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run as non-root user for cloud security
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PawCare.Api.dll"]
