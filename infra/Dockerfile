# Byggekonteksten er repo-roten, ikke infra/. Se plan kapittel 3.5:
#   docker build -f infra/Dockerfile -t dyrepermen .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Ma kopieres FOR restore. Utelates disse, feiler bygget med
# "pakke mangler versjon" - en feilmelding som ikke peker mot arsaken.
COPY Directory.Build.props Directory.Packages.props global.json ./

COPY ["src/Dyrepermen.Web/Dyrepermen.Web.csproj", "Dyrepermen.Web/"]
COPY ["src/Dyrepermen.Application/Dyrepermen.Application.csproj", "Dyrepermen.Application/"]
COPY ["src/Dyrepermen.Domain/Dyrepermen.Domain.csproj", "Dyrepermen.Domain/"]
COPY ["src/Dyrepermen.Infrastructure/Dyrepermen.Infrastructure.csproj", "Dyrepermen.Infrastructure/"]
RUN dotnet restore "Dyrepermen.Web/Dyrepermen.Web.csproj"

COPY src/ .
RUN dotnet publish "Dyrepermen.Web/Dyrepermen.Web.csproj" \
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

ENTRYPOINT ["dotnet", "Dyrepermen.Web.dll"]
