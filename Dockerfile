ARG RUNNER_IMG

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview AS builder
ARG TARGETARCH

WORKDIR /app

ARG RUNTIME

COPY ./src/Directory.Build.props ./src/*/*.csproj ./NuGet.config ./src/
RUN for file in $(ls src/*.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore ./src/Eventuous.Connector -nowarn:msb3202,nu1503 -a $TARGETARCH

FROM builder as publish
ARG TARGETARCH
COPY ./src ./src
RUN dotnet publish ./src/Eventuous.Connector -c Release -a $TARGETARCH -clp:NoSummary --no-self-contained -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runner

WORKDIR /app
COPY --from=publish /app/publish .

ENV ALLOWED_HOSTS "*"
ENV ASPNETCORE_URLS "http://*:5000"
ENTRYPOINT ["dotnet", "Eventuous.Connector.dll"]
