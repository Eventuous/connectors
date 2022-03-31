FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/connector-esdb-elastic/connector-esdb-elastic.csproj"
WORKDIR "src/connector-esdb-elastic"
RUN dotnet build "connector-esdb-elastic.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "connector-esdb-elastic.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "connector-esdb-elastic.dll"]
