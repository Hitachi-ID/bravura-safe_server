-------------------

**Hitachi ID Bravura Safe is a modified version of BitwardenÂ®. It was developed using Bitwarden open source software.  
Hitachi ID Systems, Inc. and Bravura Safe are not affiliated with or endorsed by Bitwarden or Bitwarden, Inc.  
Bitwarden is a trademark or registered trademark of Bitwarden, Inc. in the United States and/or other countries.**


The original work is available at [https://github.com/bitwarden/server]. 
The original documentation is available at [https://bitwarden.com/help/].
A complete list of all changes is available in the git history of this project.


## Build/Run

Please read the [Setup guide](https://github.comhitachi-id/bravura-safe_server/blob/master/SETUP.md) for a step-by-step guide to set up your own local development server.

### Requirements

- [.NET 5.0 SDK](https://dotnet.microsoft.com/download)
- [SQL Server 2017](https://docs.microsoft.com/en-us/sql/index)

*These dependencies are free to use.*

### Recommended Development Tooling

- [Visual Studio](https://www.visualstudio.com/vs/) (Windows and macOS)
- [Visual Studio Code](https://code.visualstudio.com/) (other)

*These tools are free to use.*

### API

```
cd src/Api
dotnet restore
dotnet build
dotnet run
```

visit http://localhost:4000/alive

### Identity

```
cd src/Identity
dotnet restore
dotnet build
dotnet run
```

visit http://localhost:33657/.well-known/openid-configuration

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

#### Git blame

We also recommend that you configure git to ignore the prettier revision using:

```bash
git config blame.ignoreRevsFile .git-blame-ignore-revs
```
