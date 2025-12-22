# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution
COPY ["HRMS.sln", "."]

# Copy project files
COPY ["HRMS.API/HRMS.API.csproj", "HRMS.API/"]
COPY ["HRMS.AI/HRMS.AI/HRMS.AI.csproj", "HRMS.AI/HRMS.AI/"]

# Restore dependencies
RUN dotnet restore "HRMS.API/HRMS.API.csproj"

# Copy full source
COPY . .

# Publish API
RUN dotnet publish "HRMS.API/HRMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Set environment and expose port
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

# Start the API
ENTRYPOINT ["dotnet", "HRMS.API.dll"]
