# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["HRMS.API/HRMS.API.csproj", "HRMS.API/"]
COPY ["HRMS.AI/HRMS.AI.csproj", "HRMS.AI/"]

# Restore dependencies
RUN dotnet restore "HRMS.API/HRMS.API.csproj"

# Copy everything else (make sure build context is solution root!)
COPY . .

# Debug: list files
RUN ls -R /src

# Build and publish
WORKDIR "/src/HRMS.API"
RUN dotnet publish "HRMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE 8080

ENTRYPOINT ["dotnet", "HRMS.API.dll"]