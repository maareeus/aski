# Build context = root del repo (serve il progetto Aski.Shared referenziato).
# docker build -f src/Aski.ControlPlane/Dockerfile -t aski-controlplane .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (cache layer): copia solo i .csproj referenziati.
COPY src/Aski.Shared/Aski.Shared.csproj src/Aski.Shared/
COPY src/Aski.ControlPlane/Aski.ControlPlane.csproj src/Aski.ControlPlane/
RUN dotnet restore src/Aski.ControlPlane/Aski.ControlPlane.csproj

# Sorgenti + publish.
COPY src/Aski.Shared/ src/Aski.Shared/
COPY src/Aski.ControlPlane/ src/Aski.ControlPlane/
RUN dotnet publish src/Aski.ControlPlane/Aski.ControlPlane.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Aski.ControlPlane.dll"]
