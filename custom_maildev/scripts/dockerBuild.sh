#!/bin/bash
# Requires:
#   jq (https://stedolan.github.io/jq/)

# Build cross platform by default
DEFAULT_PLATFORM="linux/amd64,linux/arm64"
PLATFORM="${1:-$DEFAULT_PLATFORM}"

# VERSION=`npm version --json | jq -r .maildev`
VERSION="5.5.5"

CMD="docker buildx build --load -t maildev/maildev:$VERSION -t bravura_vault/mailrelay:latest ."

echo "Running $CMD..."

$CMD