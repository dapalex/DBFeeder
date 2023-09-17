#Set base
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

#Set build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

#Copy compiled source
WORKDIR /src
COPY ["ScraperService/ScraperService.csproj", "ScraperService/"]

#Load project and dependencies
RUN dotnet restore "ScraperService/ScraperService.csproj"
COPY . .

#Build project and dependencies
WORKDIR "/src/ScraperService"
RUN dotnet build "ScraperService.csproj" -c Release -o /app/build

#Publish project and dependencies
FROM build AS publish
RUN dotnet publish "ScraperService.csproj" -c Release -o /app/publish /p:UseAppHost=false

#Copy publish files
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN echo "Copying config files..."
ADD ./ScraperService/configs/*.json /app/configs

ENV HOST=amqp
ENV CONF=wiki-TableA-commonvalue

RUN echo "Starting entrypoint..."
WORKDIR /app
ENTRYPOINT dotnet ScraperService.dll $HOST $CONF $DAQueue