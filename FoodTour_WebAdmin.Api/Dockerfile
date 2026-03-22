# 1. Giai đoạn Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy file .csproj và restore các packages
COPY ["FoodTour_WebAdmin.Api.csproj", "./"]
RUN dotnet restore "./FoodTour_WebAdmin.Api.csproj"

# Copy toàn bộ code còn lại và build
COPY . .
RUN dotnet publish "FoodTour_WebAdmin.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Giai đoạn Runtime (Chạy app)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FoodTour_WebAdmin.Api.dll"]