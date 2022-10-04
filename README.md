-------------------

**Hitachi ID Bravura Safe is a modified version of Bitwarden®. It was developed using Bitwarden open source software.  
Hitachi ID Systems, Inc. and Bravura Safe are not affiliated with or endorsed by Bitwarden or Bitwarden, Inc.  
Bitwarden is a trademark or registered trademark of Bitwarden, Inc. in the United States and/or other countries.**


The original work is available at [https://github.com/bitwarden/server]. 
The original documentation is available at [https://bitwarden.com/help/].
A complete list of all changes is available in the git history of this project.


## Developer Documentation

Please refer to the [Server Setup Guide](https://contributing.hitachi-id.com/server/guide/) in the [Contributing Documentation](https://contributing.hitachi-id.com/) for build instructions, recommended tooling, code style tips, and lots of other great information to get you started.

## Deploy


### Requirements

- [Docker](https://www.docker.com/community-edition#/download)
- [Docker Compose](https://docs.docker.com/compose/install/) (already included with some Docker installations)

*These dependencies are free to use.*

### Linux & macOS

Bitwarden name in the following refers to scripts contained within the repository and do not imply any use of their trademark.

```
curl -s -o bitwarden.sh \
    https://raw.githubusercontent.com/hitachi-id/bravura-safe_server/master/scripts/bitwarden.sh \
    && chmod +x bitwarden.sh
./bitwarden.sh install
./bitwarden.sh start
```

### Windows

```
Invoke-RestMethod -OutFile bitwarden.ps1 `
    -Uri https://raw.githubusercontent.com/hitachi-id/bravura-safe_server/master/scripts/bitwarden.ps1
.\bitwarden.ps1 -install
.\bitwarden.ps1 -start
```

## Contribute

Code contributions to this fork are not required.  Please consider contributing to the original project.


### Dotnet-format

We recently migrated to using dotnet-format as code formatter. All previous branches will need to updated to avoid large merge conflicts using the following steps:

1. Check out your local Branch
2. Run `git merge 61dc65aa598b1f492d2f0222bb7bf0dd15d116f5`
3. Resolve any merge conflicts, commit.
4. Run `dotnet tool run dotnet-format`
5. Commit
6. Run `git merge -Xours 23b0a1f9df25058ab29785ecad9a233113c10889`
7. Push
