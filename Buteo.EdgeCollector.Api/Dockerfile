# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore packages first (cached until the .csproj changes)
COPY Buteo.EdgeCollector.Api.csproj .
RUN dotnet restore

# Copy the code and publish a Release build
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---------- Stage 2: run ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
# Development so the OpenAPI document is available at /openapi/v1.json
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080

ENTRYPOINT ["dotnet", "Buteo.EdgeCollector.Api.dll"]
