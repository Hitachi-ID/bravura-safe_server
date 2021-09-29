#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
AWS="299258155391.dkr.ecr.us-east-1.amazonaws.com/"
REPO="bravura_pass"

echo ""

if [ $# -gt 1 -a "$1" == "push" ]
then
    TAG=$2

    echo "Pushing bravura_pass ($TAG)"
    echo "========================"

    docker push $AWS$REPO/api:$TAG
    docker push $AWS$REPO/identity:$TAG
    docker push $AWS$REPO/server:$TAG
    docker push $AWS$REPO/attachments:$TAG
    docker push $AWS$REPO/icons:$TAG
    docker push $AWS$REPO/notifications:$TAG
    docker push $AWS$REPO/events:$TAG
    docker push $AWS$REPO/admin:$TAG
    docker push $AWS$REPO/nginx:$TAG
    docker push $AWS$REPO/k8s-proxy:$TAG
    docker push $AWS$REPO/sso:$TAG
    docker push $AWS$REPO/portal:$TAG
    docker push $AWS$REPO/mssql:$TAG
    docker push $AWS$REPO/setup:$TAG

elif [ $# -gt 1 -a "$1" == "pull" ]
then
    TAG=$2

    echo "Pulling bravura_pass ($TAG)"
    echo "========================"

    docker pull $AWS$REPO/api:$TAG
    docker pull $AWS$REPO/identity:$TAG
    docker pull $AWS$REPO/server:$TAG
    docker pull $AWS$REPO/attachments:$TAG
    docker pull $AWS$REPO/icons:$TAG
    docker pull $AWS$REPO/notifications:$TAG
    docker pull $AWS$REPO/events:$TAG
    docker pull $AWS$REPO/admin:$TAG
    docker pull $AWS$REPO/nginx:$TAG
    docker pull $AWS$REPO/k8s-proxy:$TAG
    docker pull $AWS$REPO/sso:$TAG
    docker pull $AWS$REPO/portal:$TAG
    docker pull $AWS$REPO/mssql:$TAG
    docker pull $AWS$REPO/setup:$TAG

    docker tag $AWS$REPO/api:$TAG bravura_pass/api:$TAG
    docker tag $AWS$REPO/identity:$TAG bravura_pass/identity:$TAG
    docker tag $AWS$REPO/server:$TAG bravura_pass/server:$TAG
    docker tag $AWS$REPO/attachments:$TAG bravura_pass/attachments:$TAG
    docker tag $AWS$REPO/icons:$TAG bravura_pass/icons:$TAG
    docker tag $AWS$REPO/notifications:$TAG bravura_pass/notifications:$TAG
    docker tag $AWS$REPO/events:$TAG bravura_pass/events:$TAG
    docker tag $AWS$REPO/admin:$TAG bravura_pass/admin:$TAG
    docker tag $AWS$REPO/nginx:$TAG bravura_pass/nginx:$TAG
    docker tag $AWS$REPO/k8s-proxy:$TAG bravura_pass/k8s:$TAG
    docker tag $AWS$REPO/sso:$TAG bravura_pass/sso:$TAG
    docker tag $AWS$REPO/portal:$TAG bravura_pass/portal:$TAG
    docker tag $AWS$REPO/mssql:$TAG bravura_pass/mssql:$TAG
    docker tag $AWS$REPO/setup:$TAG bravura_pass/setup:$TAG

elif [ $# -gt 1 -a "$1" == "create" ]
then
    TAG=$2

    echo "Create AWS repo names bravura_pass ($TAG)"
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
    aws ecr create-repository --repository-name $REPO/k8s-proxy
    aws ecr create-repository --repository-name $REPO/sso
    aws ecr create-repository --repository-name $REPO/portal
    aws ecr create-repository --repository-name $REPO/mssql
    aws ecr create-repository --repository-name $REPO/setup
elif [ $# -gt 1 -a "$1" == "tag" ]
then
    TAG=$2

    echo "Tagging bravura_pass as '$TAG'"

    docker tag bravura_pass/api:latest $AWS$REPO/api:$TAG
    docker tag bravura_pass/identity:latest $AWS$REPO/identity:$TAG
    docker tag bravura_pass/server:latest $AWS$REPO/server:$TAG
    docker tag bravura_pass/attachments:latest $AWS$REPO/attachments:$TAG
    docker tag bravura_pass/icons:latest $AWS$REPO/icons:$TAG
    docker tag bravura_pass/notifications:latest $AWS$REPO/notifications:$TAG
    docker tag bravura_pass/events:latest $AWS$REPO/events:$TAG
    docker tag bravura_pass/admin:latest $AWS$REPO/admin:$TAG
    docker tag bravura_pass/nginx:latest $AWS$REPO/nginx:$TAG
    docker tag bravura_pass/nginx:latest $AWS$REPO/k8s-proxy:$TAG
    docker tag bravura_pass/sso:latest $AWS$REPO/sso:$TAG
    docker tag bravura_pass/portal:latest $AWS$REPO/portal:$TAG
    docker tag bravura_pass/mssql:latest $AWS$REPO/mssql:$TAG
    docker tag bravura_pass/setup:latest $AWS$REPO/setup:$TAG
else
    echo "Building bravura_pass"
    echo "=================="

    chmod u+x "$DIR/src/Api/build.sh"
    "$DIR/src/Api/build.sh"

    chmod u+x "$DIR/src/Identity/build.sh"
    "$DIR/src/Identity/build.sh"

    chmod u+x "$DIR/util/Server/build.sh"
    "$DIR/util/Server/build.sh"

    chmod u+x "$DIR/util/Nginx/build.sh"
    "$DIR/util/Nginx/build.sh"

    chmod u+x "$DIR/util/Attachments/build.sh"
    "$DIR/util/Attachments/build.sh"

    chmod u+x "$DIR/src/Icons/build.sh"
    "$DIR/src/Icons/build.sh"

    chmod u+x "$DIR/src/Notifications/build.sh"
    "$DIR/src/Notifications/build.sh"

    chmod u+x "$DIR/src/Events/build.sh"
    "$DIR/src/Events/build.sh"

    chmod u+x "$DIR/src/Admin/build.sh"
    "$DIR/src/Admin/build.sh"

    chmod u+x "$DIR/util/MsSql/build.sh"
    "$DIR/util/MsSql/build.sh"

    chmod u+x "$DIR/util/Setup/build.sh"
    "$DIR/util/Setup/build.sh"
fi

