FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Assessment.DataIngestor/Assessment.DataIngestor.csproj", "Assessment.DataIngestor/"]
RUN dotnet restore "Assessment.DataIngestor/Assessment.DataIngestor.csproj"
COPY . .
WORKDIR "/src/Assessment.DataIngestor"
RUN dotnet build "Assessment.DataIngestor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Assessment.DataIngestor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Assessment.DataIngestor.dll"]
