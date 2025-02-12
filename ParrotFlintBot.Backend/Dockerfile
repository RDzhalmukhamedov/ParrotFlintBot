# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Create a user with a known UID/GID within range 10000-20000.
# This is required by Choreo to run the container as a non-root user.
RUN adduser \
    --disabled-password \
    --gecos "" \
    --home "/nonexistent" \
    --shell "/sbin/nologin" \
    --no-create-home \
    --uid 10014 \
    "choreo"

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ParrotFlintBot.Backend/ParrotFlintBot.Backend.csproj", "ParrotFlintBot.Backend/"]
COPY ["ParrotFlintBot.DB/ParrotFlintBot.DB.csproj", "ParrotFlintBot.DB/"]
COPY ["ParrotFlintBot.Domain/ParrotFlintBot.Domain.csproj", "ParrotFlintBot.Domain/"]
COPY ["ParrotFlintBot.Shared/ParrotFlintBot.Shared.csproj", "ParrotFlintBot.Shared/"]
COPY ["ParrotFlintBot.RabbitMQ/ParrotFlintBot.RabbitMQ.csproj", "ParrotFlintBot.RabbitMQ/"]
RUN dotnet restore "./ParrotFlintBot.Backend/ParrotFlintBot.Backend.csproj"
COPY . .
WORKDIR "/src/ParrotFlintBot.Backend"
RUN dotnet build "./ParrotFlintBot.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ParrotFlintBot.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Use the above created unprivileged user
USER 10014

ENTRYPOINT ["dotnet", "ParrotFlintBot.Backend.dll"]
EXPOSE 6543 5432 5672
