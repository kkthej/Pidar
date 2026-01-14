# =========================
# Stage 1: Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file first (cache-friendly)
COPY Pidar.csproj ./
RUN dotnet restore

# Copy the rest of the source
COPY . ./

# Build & publish
RUN dotnet publish -c Release -o /app/publish


# =========================
# Stage 2: Development (Hot Reload)
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /app

EXPOSE 80
EXPOSE 443

# Copy project file and restore (optional but safe)
COPY Pidar.csproj ./
RUN dotnet restore

# Source will be overridden by docker-compose volume mount
COPY . ./

# 🔥 Hot reload enabled here
ENTRYPOINT ["dotnet", "watch", "--project", "Pidar.csproj", "run", "--no-restore", "--urls", "http://+:80"]


# =========================
# Stage 3: Production
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production
WORKDIR /app

EXPOSE 80
EXPOSE 443

# Copy published output only
COPY --from=build /app/publish ./

# 🚀 Correct production entrypoint
ENTRYPOINT ["dotnet", "Pidar.dll"]
