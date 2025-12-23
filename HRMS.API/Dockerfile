# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["HRMS.API.csproj", "."]
RUN dotnet restore "HRMS.API.csproj"

# Copy the rest of the source
COPY . .

# Build and publish in release mode
RUN dotnet publish "HRMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Set environment and ports
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

# Start the API
ENTRYPOINT ["dotnet", "HRMS.API.dll"]
