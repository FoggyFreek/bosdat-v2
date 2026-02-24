FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BosDAT.Worker/BosDAT.Worker.csproj", "BosDAT.Worker/"]
RUN dotnet restore "BosDAT.Worker/BosDAT.Worker.csproj"
COPY . .
WORKDIR "/src/BosDAT.Worker"
RUN dotnet build "BosDAT.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BosDAT.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV WorkerSettings__Api__BaseUrl=http://api:8080
ENTRYPOINT ["dotnet", "BosDAT.Worker.dll"]
