# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY Pidar.csproj ./
RUN dotnet restore

# Copy everything else
COPY . ./

# Build the project
RUN dotnet build -c Debug -o /app/build

# Publish the project (for production)
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Development
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy source (optional if using volume mounts)
COPY --from=build /src ./

# Run with dotnet watch
ENTRYPOINT ["dotnet", "watch", "--project", "Pidar.csproj", "run", "--no-restore"]

# Stage 3: Production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy published output
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Pidar.dll"]
