#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo -e "\n## Building Nginx"

echo -e "\nBuilding docker image"
docker --version
docker build -t bravura_vault/nginx "$DIR/."


echo -e "\n## Building k8s-proxy"

echo -e "\nBuilding docker image"
docker build -f $DIR/Dockerfile-k8s -t bravura_vault/k8s-proxy "$DIR/."
