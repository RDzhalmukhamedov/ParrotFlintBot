FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ParrotFlintBot.Backend/ParrotFlintBot.Backend.csproj", "ParrotFlintBot.Backend/"]
COPY ["ParrotFlintBot.RabbitMQ/ParrotFlintBot.RabbitMQ.csproj", "ParrotFlintBot.RabbitMQ/"]
COPY ["ParrotFlintBot.DB/ParrotFlintBot.DB.csproj", "ParrotFlintBot.DB/"]
COPY ["ParrotFlintBot.Domain/ParrotFlintBot.Domain.csproj", "ParrotFlintBot.Domain/"]
COPY ["ParrotFlintBot.Shared/ParrotFlintBot.Shared.csproj", "ParrotFlintBot.Shared/"]
RUN dotnet restore "ParrotFlintBot.Backend/ParrotFlintBot.Backend.csproj"
COPY . .
WORKDIR "/src/ParrotFlintBot.Backend"
RUN dotnet build "ParrotFlintBot.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ParrotFlintBot.Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ParrotFlintBot.Backend.dll"]
EXPOSE 6543 5432 5672
