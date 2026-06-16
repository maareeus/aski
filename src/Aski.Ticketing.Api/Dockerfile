# Immagine dell'istanza di ticketing, avviata per ogni progetto provisionato.
# docker build -f src/Aski.Ticketing.Api/Dockerfile -t aski-ticketing .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Aski.Ticketing.Api/Aski.Ticketing.Api.csproj src/Aski.Ticketing.Api/
RUN dotnet restore src/Aski.Ticketing.Api/Aski.Ticketing.Api.csproj

COPY src/Aski.Ticketing.Api/ src/Aski.Ticketing.Api/
RUN dotnet publish src/Aski.Ticketing.Api/Aski.Ticketing.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Aski.Ticketing.Api.dll"]
