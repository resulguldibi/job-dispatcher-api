#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["job-dispatcher-api/job-dispatcher-api.csproj", "job-dispatcher-api/"]
COPY ["client.kafka/client.kafka.csproj", "client.kafka/"]
COPY ["client.cassandra/client.cassandra.csproj", "client.cassandra/"]
COPY ["client.socket/client.socket.csproj", "client.socket/"]
COPY ["job-dispatcher/job-dispatcher.csproj", "job-dispatcher/"]
RUN dotnet restore "client.kafka/client.kafka.csproj"
RUN dotnet restore "client.socket/client.socket.csproj"
RUN dotnet restore "client.cassandra/client.cassandra.csproj"
RUN dotnet restore "job-dispatcher-api/job-dispatcher-api.csproj"
RUN dotnet restore "job-dispatcher/job-dispatcher.csproj"
COPY . .
WORKDIR "/src/job-dispatcher-api"
RUN dotnet build "job-dispatcher-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "job-dispatcher-api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "job-dispatcher-api.dll"]