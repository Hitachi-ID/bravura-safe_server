FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

LABEL com.hitachi.product="bravura_vault" com.hitachi.project="setup"

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        openssl \
        gosu \
&& rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY obj/build-output/publish .
COPY entrypoint.sh /
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
