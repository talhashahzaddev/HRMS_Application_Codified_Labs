# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Create a dummy .git folder to avoid Railway .git/HEAD errors
RUN mkdir .git && echo "ref: refs/heads/main" > .git/HEAD

# Copy only the csproj files for restore
COPY HRMS.sln .
COPY HRMS.API/HRMS.API.csproj HRMS.API/
COPY HRMS.AI/HRMS.AI/HRMS.AI.csproj HRMS.AI/HRMS.AI/

# Restore dependencies
RUN dotnet restore "HRMS.API/HRMS.API.csproj"

# Copy only actual source folders
COPY HRMS.API/ HRMS.API/
COPY HRMS.AI/ HRMS.AI/

# Publish API
RUN dotnet publish "HRMS.API/HRMS.API.csproj" \
    -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:SourceLinkCreate=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "HRMS.API.dll"]
