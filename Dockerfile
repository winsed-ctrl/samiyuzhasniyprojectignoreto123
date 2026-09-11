# Сборка
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем весь проект
COPY . .

# Восстанавливаем и публикуем
RUN dotnet restore "GromCore.Laser.Server/GromCore.Laser.Server.csproj"
RUN dotnet publish "GromCore.Laser.Server/GromCore.Laser.Server.csproj" -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Railway передаёт порт через переменную PORT
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GromCore.Laser.Server.dll"]