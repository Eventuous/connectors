FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Eventuous.Connector.EsdbElastic/Eventuous.Connector.EsdbElastic.csproj"
WORKDIR "src/Eventuous.Connector.EsdbElastic"
RUN dotnet build "Eventuous.Connector.EsdbElastic.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Eventuous.Connector.EsdbElastic.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Eventuous.Connector.EsdbElastic.dll"]
