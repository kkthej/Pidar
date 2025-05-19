# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution/project files
COPY *.csproj ./
RUN dotnet restore

# Copy everything else
COPY . .

# Build (keep Debug for development)
RUN dotnet build -c Debug -o /app/build

# Publish (for production)
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run (Development)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy build output (Debug)
COPY --from=build /app/build .

# For development, we'll use dotnet run with mounted volumes
ENTRYPOINT ["dotnet", "watch", "run", "--no-restore"]

# Stage 3: Run (Production)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy publish output (Release)
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Pidar.dll"]