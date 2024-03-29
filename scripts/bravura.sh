#!/usr/bin/env bash
set -e

cat << "EOF"
BRAVURA-SECURITY
Bravura Safe
EOF

cat << EOF
Open source password management solutions
===================================================
EOF

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SCRIPT_NAME=$(basename "$0")
SCRIPT_PATH="$DIR/$SCRIPT_NAME"
OUTPUT="$DIR/bvdata"
if [ $# -eq 2 ]
then
    OUTPUT=$2
fi

if command -v docker-compose &> /dev/null
then
    dccmd='docker-compose'
else
    dccmd='docker compose'
fi

SCRIPTS_DIR="$OUTPUT/scripts"
GITHUB_BASE_URL="https://gitlab.hitachi-id.com/bravura-vault/server"

# Please do not create pull requests modifying the version numbers.
COREVERSION="latest"
WEBVERSION="latest"
KEYCONNECTORVERSION="1.0.1"

echo "bravura.sh version $COREVERSION"
docker --version
if [[ "$dccmd" == "docker compose" ]]; then
    $dccmd version
else
    $dccmd --version
fi

echo ""

# Functions

function downloadSelf() {
    if curl -s -w "http_code %{http_code}" -o $SCRIPT_PATH.1 $GITHUB_BASE_URL/scripts/bravura.sh | grep -q "^http_code 20[0-9]"
    then
        mv $SCRIPT_PATH.1 $SCRIPT_PATH
        chmod u+x $SCRIPT_PATH
    else
        rm -f $SCRIPT_PATH.1
    fi
}

function downloadRunFile() {
    if [ ! -d "$SCRIPTS_DIR" ]
    then
        mkdir $SCRIPTS_DIR
    fi

    # Until we have a published, public place to stash this, manually copy run.sh with bravura.sh and use that file
    # curl -s -o $SCRIPTS_DIR/run.sh $GITHUB_BASE_URL/scripts/run.sh
    cp run.sh $SCRIPTS_DIR/run.sh

    chmod u+x $SCRIPTS_DIR/run.sh
    rm -f $SCRIPTS_DIR/install.sh
}

function checkOutputDirExists() {
    if [ ! -d "$OUTPUT" ]
    then
        echo "Cannot find a Bravura Safe installation at $OUTPUT."
        exit 1
    fi
}

function checkOutputDirNotExists() {
    if [ -d "$OUTPUT/docker" ]
    then
        echo "Looks like Bravura Safe is already installed at $OUTPUT."
        exit 1
    fi
}

function listCommands() {
cat << EOT
Available commands:

install
start
restart
stop
update
updatedb
updaterun
updateself
updateconf
uninstall
renewcert
rebuild
help

EOT
}

# Commands

case $1 in
    "install")
        checkOutputDirNotExists
        mkdir -p $OUTPUT
        downloadRunFile
        $SCRIPTS_DIR/run.sh install $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        echo "*****"
        echo "*****"
        echo "Please remember after a fresh install to correctly set email username/password"
        echo "in the docker/docker-compose.yml if you wish to correctly relay emails via AWS SES"
	echo "Otherwise all emails will be sent only to maildev container and will never reach intended recipient."
        echo "Then restart containers if any setup changes are made."
        echo "*****"
        echo "*****"
        ;;
    "start" | "restart")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh restart $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "update")
        checkOutputDirExists
        downloadRunFile
        $SCRIPTS_DIR/run.sh update $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "rebuild")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh rebuild $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "updateconf")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh updateconf $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "updatedb")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh updatedb $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "stop")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh stop $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "renewcert")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh renewcert $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "updaterun")
        checkOutputDirExists
        downloadRunFile
        ;;
    "updateself")
        # Until we have a published, public place to stash this, manually retrieve bravura.sh
        # downloadSelf && echo "Updated self." && exit
        echo "Please manually retrieve latest file." && exit
        ;;
    "uninstall")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh uninstall $OUTPUT
        ;;
    "help")
        listCommands
        ;;
    *)
        echo "No command found."
        echo
        listCommands
esac
