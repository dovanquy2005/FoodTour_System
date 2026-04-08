# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FoodTour_WebAdmin.Api.csproj", "./"]
RUN dotnet restore "./FoodTour_WebAdmin.Api.csproj"

COPY . .
RUN dotnet publish "FoodTour_WebAdmin.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# 👇 dùng PORT của Render
ENV ASPNETCORE_URLS=http://+:${PORT}

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FoodTour_WebAdmin.Api.dll"]