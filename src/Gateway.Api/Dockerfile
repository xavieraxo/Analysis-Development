FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Gateway.Api/Gateway.Api.csproj", "src/Gateway.Api/"]
COPY ["src/Shared.Abstractions/Shared.Abstractions.csproj", "src/Shared.Abstractions/"]
COPY ["src/Orchestrator.App/Orchestrator.App.csproj", "src/Orchestrator.App/"]
COPY ["src/Agents.PM/Agents.PM.csproj", "src/Agents.PM/"]
COPY ["src/Agents.PO/Agents.PO.csproj", "src/Agents.PO/"]
COPY ["src/Agents.Dev/Agents.Dev.csproj", "src/Agents.Dev/"]
COPY ["src/Agents.UX/Agents.UX.csproj", "src/Agents.UX/"]
COPY ["src/Agents.UR/Agents.UR.csproj", "src/Agents.UR/"]
RUN dotnet restore "src/Gateway.Api/Gateway.Api.csproj"
COPY . .
WORKDIR "/src/src/Gateway.Api"
RUN dotnet build "Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]

