# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=9.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS builder
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY --link Pidar.csproj ./
# Copy any Directory.Build.props or Directory.Packages.props if present (not in this project, but good practice)
# COPY --link Directory.*.props ./

# Restore dependencies using cache mounts for nuget and msbuild
RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=cache,target=/root/.cache/msbuild \
    dotnet restore "Pidar.csproj"

# Copy the rest of the source code
COPY --link . .

# Publish the application to the /app/publish directory
RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=cache,target=/root/.cache/msbuild \
    dotnet publish "Pidar.csproj" -c Release -o /app/publish --no-restore

# --- Final image ---
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

# Create a non-root user and group
RUN addgroup --system --gid 10001 appgroup && \
    adduser --system --uid 10001 --ingroup appgroup appuser

# Copy published output from builder
COPY --from=builder /app/publish .

# Set permissions for the non-root user
RUN chown -R appuser:appgroup /app
USER appuser

# Expose the default ASP.NET Core port
EXPOSE 8080

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_URLS="http://+:8080"
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "Pidar.dll"]
