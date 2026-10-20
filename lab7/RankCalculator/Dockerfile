FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src/RankCalculator
COPY ["RankCalculator/RankCalculator.csproj", "."]
RUN dotnet restore "RankCalculator.csproj"
COPY RankCalculator .
COPY Sharding ../Sharding
RUN dotnet build "RankCalculator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RankCalculator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RankCalculator.dll"]
