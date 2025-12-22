# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY HRMS.sln .
COPY HRMS.API/HRMS.API.csproj HRMS.API/
COPY HRMS.AI/HRMS.AI/HRMS.AI.csproj HRMS.AI/HRMS.AI/

# Restore dependencies
RUN dotnet restore "HRMS.API/HRMS.API.csproj"

# Copy only the source code (avoid .git, bin, obj)
COPY HRMS.API/ HRMS.API/
COPY HRMS.AI/ HRMS.AI/

# Publish API
RUN dotnet publish "HRMS.API/HRMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "HRMS.API.dll"]
