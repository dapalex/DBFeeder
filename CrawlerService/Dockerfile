FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CrawlerService/CrawlerService.csproj", "CrawlerService/"]
RUN dotnet restore "CrawlerService/CrawlerService.csproj" -v diag
COPY . .
WORKDIR "/src/CrawlerService"
RUN dotnet build "CrawlerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CrawlerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ARG RMQHOST
ENV RMQHOSTENV=$RMQHOST
RUN echo "Here it is ENV: $NEWENVARG"
ENTRYPOINT dotnet CrawlerService.dll RMQHOSTENV



