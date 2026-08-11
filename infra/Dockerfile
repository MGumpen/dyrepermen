# Landingssiden. Byggekonteksten er repo-roten, ikke infra/:
#   docker build -f infra/Dockerfile -t dyrepermen-landing .
#
# Appens egen Dockerfile pa dev kopierer Directory.Build.props og
# Directory.Packages.props for restore. Denne branchen har ingen av delene,
# fordi landingssiden ikke bruker en eneste NuGet-pakke.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY ["src/Dyrepermen.Landingsside/Dyrepermen.Landingsside.csproj", "Dyrepermen.Landingsside/"]
RUN dotnet restore "Dyrepermen.Landingsside/Dyrepermen.Landingsside.csproj"

COPY src/ .
RUN dotnet publish "Dyrepermen.Landingsside/Dyrepermen.Landingsside.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
# Server-GC allokerer per kjerne og spiser opp minnet pa 512 MB-instanser.
ENV DOTNET_gcServer=0
ENV TZ=Europe/Oslo

EXPOSE 8080
COPY --from=build /app/publish .

# Ikke-privilegert bruker foelger med aspnet-bildet fra .NET 8.
USER $APP_UID

ENTRYPOINT ["dotnet", "Dyrepermen.Landingsside.dll"]
