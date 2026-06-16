FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

# Beschreibbare Ordner vorbereiten
# Wichtig für:
# - DataProtection-Keys
# - Uploads
RUN mkdir -p /app/DataProtection-Keys \
    && mkdir -p /app/Data/Uploads \
    && chown -R app:app /app/DataProtection-Keys \
    && chown -R app:app /app/Data

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Dsms.Web/Dsms.Web.csproj", "Dsms.Web/"]
RUN dotnet restore "./Dsms.Web/Dsms.Web.csproj"

COPY . .
WORKDIR "/src/Dsms.Web"
RUN dotnet build "./Dsms.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Dsms.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish --chown=app:app /app/publish .

USER app

ENTRYPOINT ["dotnet", "Dsms.Web.dll"]