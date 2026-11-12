# Chronos in a Linux container (issue #325).
#
#   docker build --build-arg VERSION=1.6.0 -t chronos .
#
# VERSION stamps the assemblies. The build context carries no .git (see .dockerignore), so
# MinVer cannot read the tags here: without VERSION the app reports 0.0.0-alpha.0. The
# Release workflow passes the version of the tag it builds.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG VERSION=
WORKDIR /src

# Project files first, so the restore layer is reused while only the sources change.
COPY Directory.Build.props ./
COPY src/Chronos.Domain/Chronos.Domain.csproj src/Chronos.Domain/
COPY src/Chronos.Application/Chronos.Application.csproj src/Chronos.Application/
COPY src/Chronos.Infrastructure/Chronos.Infrastructure.csproj src/Chronos.Infrastructure/
COPY src/Chronos.Web/Chronos.Web.csproj src/Chronos.Web/
RUN dotnet restore src/Chronos.Web/Chronos.Web.csproj

COPY src/ src/
RUN dotnet publish src/Chronos.Web/Chronos.Web.csproj \
        --configuration Release \
        --no-restore \
        --output /app \
        ${VERSION:+-p:MinVerVersionOverride=$VERSION}

# Debian, not Alpine: the app needs ICU and tzdata, and this image already has both.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Everything that has to survive a new container lives in /data: the SQLite database, the
# DataProtection keys that decrypt the secrets stored in it, and the logs. The logs also go
# to the console, so "docker logs" shows them, as does the Deploy to Docker workflow when
# the smoke check fails. The folder is
# created here and owned by the unprivileged "app" user, so a named volume mounted over it
# starts out writable.
RUN mkdir -p /data && chown app:app /data
VOLUME /data

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Chronos="Data Source=/data/JiraCopilot.sqlite3" \
    DataProtection__KeysPath=/data/DataProtection-Keys \
    Serilog__WriteTo__0__Args__path=/data/logs/log-.txt \
    Serilog__WriteTo__1__Name=Console \
    HealthChecks__EndpointUri=http://localhost:8080/health \
    TZ=Europe/Moscow

COPY --from=build /app ./

ARG VERSION=
LABEL org.opencontainers.image.source="https://github.com/tolmachev-pravo/chronos" \
      org.opencontainers.image.title="Chronos" \
      org.opencontainers.image.description="Fills in Jira worklogs from your activity" \
      org.opencontainers.image.version="${VERSION}"

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Chronos.Web.dll"]
