#Set base
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

#Set build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

#Copy compiled source
WORKDIR /src
COPY ["DACService/DACService.csproj", "DACService/"]

#Load project and dependencies
RUN dotnet restore "DACService/DACService.csproj"
COPY . .

#Build project and dependencies
WORKDIR "/src/DACService"
RUN dotnet build "DACService.csproj" -c Release -o /app/build

#Publish project and dependencies
FROM build AS publish
RUN dotnet publish "DACService.csproj" -c Release -o /app/publish /p:UseAppHost=false

#Copy publish files
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ARG RMQHOST
ENV HOST=$RMQHOST
ARG ENTITY
ENV RMQENTITY=$ENTITY

WORKDIR /app
ENTRYPOINT dotnet DACService.dll $HOST $RMQENTITY
