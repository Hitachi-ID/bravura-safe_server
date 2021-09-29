#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo -e "\n## Building Notifications"

echo -e "\nBuilding app"
echo ".NET Core version $(dotnet --version)"
echo "Restore"
dotnet restore "$DIR/Notifications.csproj"
echo "Clean"
dotnet clean "$DIR/Notifications.csproj" -c "Release" -o "$DIR/obj/build-output/publish"
echo "Publish"
dotnet publish "$DIR/Notifications.csproj" -c "Release" -o "$DIR/obj/build-output/publish"

if [ "$1" != "nodocker" ]
then
    echo -e "\nBuilding docker image"
    docker --version
    docker build -t bravura_vault/notifications "$DIR/."
fi
