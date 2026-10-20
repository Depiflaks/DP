FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["EventsLogger.csproj", "."]
RUN dotnet restore "EventsLogger.csproj"

COPY . .
RUN dotnet build "EventsLogger.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/build .

ENTRYPOINT ["dotnet", "EventsLogger.dll"]
