---

**Bravura Safe is a modified version of Bitwarden®. It was developed using Bitwarden open source software.  
Bravura Security, Inc. and Bravura Safe are not affiliated with or endorsed by Bitwarden or Bitwarden, Inc.  
Bitwarden is a trademark or registered trademark of Bitwarden, Inc. in the United States and/or other countries.**


The original work is available at [https://github.com/bitwarden/server]. 
The original documentation is available at [https://bitwarden.com/help/].
A complete list of all changes is available in the git history of this project.


## Developer Documentation

Please refer to the [Server Setup Guide](https://contributing.bravurasecurity.com/server/guide/) in the [Contributing Documentation](https://contributing.bravurasecurity.com/) for build instructions, recommended tooling, code style tips, and lots of other great information to get you started.

## Deploy


### Requirements

- [Docker](https://www.docker.com/community-edition#/download)
- [Docker Compose](https://docs.docker.com/compose/install/) (already included with some Docker installations)

_These dependencies are free to use._

### Linux & macOS

Bitwarden name in the following refers to scripts contained within the repository and do not imply any use of their trademark.

```
curl -s -L -o bitwarden.sh \
    "https://func.bitwarden.com/api/dl/?app=self-host&platform=linux" \
    && chmod +x bitwarden.sh
./bitwarden.sh install
./bitwarden.sh start
```

### Windows

```
Invoke-RestMethod -OutFile bitwarden.ps1 `
    -Uri "https://func.bitwarden.com/api/dl/?app=self-host&platform=windows"
.\bitwarden.ps1 -install
.\bitwarden.ps1 -start
```

## Contribute

Code contributions to this fork are not required.  Please consider contributing to the original project.


### Dotnet-format

Consider installing our git pre-commit hook for automatic formatting.

```bash
git config --local core.hooksPath .git-hooks
```
