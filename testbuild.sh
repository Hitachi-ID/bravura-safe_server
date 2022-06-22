#!/usr/bin/env bash
set -e

VERSION="0.2"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo ""

if [ $# -eq 1 -a "$1" == "help" ]
then
    echo "Version $VERSION"
    echo "Usage: testbuild.sh [command] [REPO | REPO TAG | REPO1 TAG1 REPO2 TAG2]"
    echo "Without any parameters will call the individual build scripts and generate docker images."
    echo "========================"
    echo "push REPO TAG"
    echo "pull REPO TAG"
    echo "aws-create REPO"
    echo "tag REPO1 TAG1 REPO2 TAG2"
    echo ""
    echo ""

elif [ $# -gt 1 -a "$1" == "push" ]
then
    REPO=$2
    TAG=$3

    echo "Pushing bravura_vault ($TAG)"
    echo "========================"

    docker push $REPO/api:$TAG
    docker push $REPO/identity:$TAG
    docker push $REPO/server:$TAG
    docker push $REPO/attachments:$TAG
    docker push $REPO/icons:$TAG
    docker push $REPO/notifications:$TAG
    docker push $REPO/events:$TAG
    docker push $REPO/admin:$TAG
    docker push $REPO/nginx:$TAG
    docker push $REPO/sso:$TAG
    docker push $REPO/mssql:$TAG
    docker push $REPO/setup:$TAG

elif [ $# -gt 1 -a "$1" == "pull" ]
then
    REPO=$2
    TAG=$3

    echo "Pulling bravura_vault ($TAG)"
    echo "========================"

    docker pull $REPO/api:$TAG
    docker pull $REPO/identity:$TAG
    docker pull $REPO/server:$TAG
    docker pull $REPO/attachments:$TAG
    docker pull $REPO/icons:$TAG
    docker pull $REPO/notifications:$TAG
    docker pull $REPO/events:$TAG
    docker pull $REPO/admin:$TAG
    docker pull $REPO/nginx:$TAG
    docker pull $REPO/sso:$TAG
    docker pull $REPO/mssql:$TAG
    docker pull $REPO/setup:$TAG

    docker tag $REPO/api:$TAG bravura_vault/api:$TAG
    docker tag $REPO/identity:$TAG bravura_vault/identity:$TAG
    docker tag $REPO/server:$TAG bravura_vault/server:$TAG
    docker tag $REPO/attachments:$TAG bravura_vault/attachments:$TAG
    docker tag $REPO/icons:$TAG bravura_vault/icons:$TAG
    docker tag $REPO/notifications:$TAG bravura_vault/notifications:$TAG
    docker tag $REPO/events:$TAG bravura_vault/events:$TAG
    docker tag $REPO/admin:$TAG bravura_vault/admin:$TAG
    docker tag $REPO/nginx:$TAG bravura_vault/nginx:$TAG
    docker tag $REPO/mssql:$TAG bravura_vault/sso:$TAG
    docker tag $REPO/mssql:$TAG bravura_vault/mssql:$TAG
    docker tag $REPO/setup:$TAG bravura_vault/setup:$TAG

elif [ $# -gt 1 -a "$1" == "aws-create" ]
then
    REPO=$2

    echo "Create AWS repo names"
    echo "========================"

    aws ecr create-repository --repository-name $REPO/api
    aws ecr create-repository --repository-name $REPO/identity
    aws ecr create-repository --repository-name $REPO/server
    aws ecr create-repository --repository-name $REPO/attachments
    aws ecr create-repository --repository-name $REPO/icons
    aws ecr create-repository --repository-name $REPO/notifications
    aws ecr create-repository --repository-name $REPO/events
    aws ecr create-repository --repository-name $REPO/admin
    aws ecr create-repository --repository-name $REPO/nginx
    aws ecr create-repository --repository-name $REPO/sso
    aws ecr create-repository --repository-name $REPO/mssql
    aws ecr create-repository --repository-name $REPO/setup

elif [ $# -gt 1 -a "$1" == "tag" ]
then
    REPO1=$2
    TAG1=$3
    REPO2=$4
    TAG2=$5

    echo "Tagging"
    echo "=================="

    docker tag $REPO1/api:$TAG1 $REPO2/api:$TAG2
    docker tag $REPO1/identity:$TAG1 $REPO2/identity:$TAG2
    docker tag $REPO1/server:$TAG1 $REPO2/server:$TAG2
    docker tag $REPO1/attachments:$TAG1 $REPO2/attachments:$TAG2
    docker tag $REPO1/icons:$TAG1 $REPO2/icons:$TAG2
    docker tag $REPO1/notifications:$TAG1 $REPO2/notifications:$TAG2
    docker tag $REPO1/events:$TAG1 $REPO2/events:$TAG2
    docker tag $REPO1/admin:$TAG1 $REPO2/admin:$TAG2
    docker tag $REPO1/nginx:$TAG1 $REPO2/nginx:$TAG2
    docker tag $REPO1/mssql:$TAG1 $REPO2/sso:$TAG2
    docker tag $REPO1/mssql:$TAG1 $REPO2/mssql:$TAG2
    docker tag $REPO1/setup:$TAG1 $REPO2/setup:$TAG2
else
    echo "Building bravura_vault"
    echo "=================="

    "$DIR/scripts/build" api
    "$DIR/scripts/build-docker" api

    "$DIR/scripts/build" identity
    "$DIR/scripts/build-docker" identity

    "$DIR/scripts/build" server
    "$DIR/scripts/build-docker" server

    "$DIR/scripts/build" nginx
    "$DIR/scripts/build-docker" nginx

    "$DIR/scripts/build" attachments
    "$DIR/scripts/build-docker" attachments

    "$DIR/scripts/build" icons
    "$DIR/scripts/build-docker" icons

    "$DIR/scripts/build" notifications
    "$DIR/scripts/build-docker" notifications

    "$DIR/scripts/build" events
    "$DIR/scripts/build-docker" events

    "$DIR/scripts/build" admin
    "$DIR/scripts/build-docker" admin

    "$DIR/scripts/build" sso
    "$DIR/scripts/build-docker" sso
    
    "$DIR/scripts/build" mssql
    "$DIR/scripts/build-docker" mssql

    "$DIR/scripts/build" setup
    "$DIR/scripts/build-docker" setup
fi
