# syntax=docker/dockerfile:1
# Multi-stage chiseled build (distroless, non-root). Pattern from:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/aspnetapp/Dockerfile.chiseled
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG TARGETARCH
WORKDIR /source

# Restore as a distinct, cacheable layer (project file only)
COPY --link src/MicroserviceExample/*.csproj src/MicroserviceExample/
RUN dotnet restore src/MicroserviceExample/MicroserviceExample.csproj -a $TARGETARCH

# Copy the rest of the source and publish
COPY --link src/MicroserviceExample/. src/MicroserviceExample/
RUN dotnet publish src/MicroserviceExample/MicroserviceExample.csproj -a $TARGETARCH --no-restore -o /app

# Runtime stage: Ubuntu chiseled — minimal packages, no shell, non-root 'app' user,
# ASPNETCORE_HTTP_PORTS=8080 baked in.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
EXPOSE 8080
COPY --link --from=build /app .
ENTRYPOINT ["dotnet", "MicroserviceExample.dll"]
