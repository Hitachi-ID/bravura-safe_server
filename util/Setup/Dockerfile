FROM mcr.microsoft.com/dotnet/aspnet:6.0

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
