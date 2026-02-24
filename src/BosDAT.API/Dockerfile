FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/BosDAT.API/BosDAT.API.csproj", "src/BosDAT.API/"]
COPY ["src/BosDAT.Core/BosDAT.Core.csproj", "src/BosDAT.Core/"]
COPY ["src/BosDAT.Infrastructure/BosDAT.Infrastructure.csproj", "src/BosDAT.Infrastructure/"]
RUN dotnet restore "src/BosDAT.API/BosDAT.API.csproj"
COPY . .
WORKDIR "/src/src/BosDAT.API"
RUN dotnet build "BosDAT.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BosDAT.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BosDAT.API.dll"]
