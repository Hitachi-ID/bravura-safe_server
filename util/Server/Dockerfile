FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine

LABEL com.hitachi.product="bravura_vault"

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY obj/build-output/publish /bitwarden_server
