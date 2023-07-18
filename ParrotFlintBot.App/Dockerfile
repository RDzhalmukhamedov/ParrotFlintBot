FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ParrotFlintBot.App/ParrotFlintBot.App.csproj", "ParrotFlintBot.App/"]
COPY ["ParrotFlintBot.RabbitMQ/ParrotFlintBot.RabbitMQ.csproj", "ParrotFlintBot.RabbitMQ/"]
COPY ["ParrotFlintBot.Shared/ParrotFlintBot.Shared.csproj", "ParrotFlintBot.Shared/"]
RUN dotnet restore "ParrotFlintBot.App/ParrotFlintBot.App.csproj"
COPY . .
WORKDIR "/src/ParrotFlintBot.App"
RUN dotnet build "ParrotFlintBot.App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ParrotFlintBot.App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ParrotFlintBot.App.dll"]
EXPOSE 443 5672
