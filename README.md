# Tur

[![master](https://github.com/JerryBian/tur/actions/workflows/build.yml/badge.svg)](https://github.com/JerryBian/tur/actions/workflows/build.yml)

Command line tool to manage files.

_This is developed only for my personal daily usage purpose, it's not intended to fit all requirements for everyone, use it at your own risk._

## Installation

### Recommend

Install via NuGet [package](https://www.nuget.org/packages/tur/).

```sh
dotnet tool install -g tur
```

Fot tab completion, please refer to [dotnet-suggest](https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md).

### Alternative

Go to [Release page](https://github.com/JerryBian/tur/releases/tag/latest), download the binary according to your computer architecture. The binary is self contained executable file, make it not depend on any other files in your system.

## Usage

At this moment, `tur` supports thress subcommands:
- [`dff`](#dff)
- [`sync`](#sync)
- [`rm`](#rm)

In general the syntax likes:

```sh
tur [command] [options]
```

### `dff`

```sh
PS C:\Users\jerry> tur dff --help
Description:
  Duplicate files finder.

Usage:
  tur dff <dir> [options]

Arguments:
  <dir>  The target directory to analysis.

Options:
  -i, --include <include>  Glob patterns for included files.
  -e, --exclude <exclude>  Glob patterns for excluded files.
  -o, --output <output>    The output directory for logs or any file generated during processing.
  -v, --verbose            Display detailed logs.
  -?, -h, --help           Show help and usage information
```

### `sync`

```sh
PS C:\Users\jerry> tur sync --help
Description:
  Synchronize files from source to destination directory.

Usage:
  tur sync <src> <dest> [options]

Arguments:
  <src>   The source directory.
  <dest>  The Destination directory.

Options:
  -i, --include <include>  Glob patterns for included files.
  -e, --exclude <exclude>  Glob patterns for excluded files.
  -o, --output <output>    The output directory for logs or any file generated during processing.
  -v, --verbose            Display detailed logs.
  -n, --dry-run            Perform a trial run with no changes made.
  -d, --delete             Delete extraneous files from destination directory.
  --size-only              Skip files that match in both name and size.
  -?, -h, --help           Show help and usage information
```

### `rm`

```sh
PS C:\Users\jerry> tur rm --help
Description:
  Remove files or directories.

Usage:
  tur rm <dest> [options]

Arguments:
  <dest>  The destination directory.

Options:
  -i, --include <include>  Glob patterns for included files.
  -e, --exclude <exclude>  Glob patterns for excluded files.
  -o, --output <output>    The output directory for logs or any file generated during processing.
  -v, --verbose            Display detailed logs.
  -y, --yes                Perform deletion without confirmation.
  -f, --file               Delete files only.
  -d, --dir                Delete directories only.
  --empty-dir              Delete all empty directories.
  --from-file <from-file>  Delete all files/directories listed in specified file.
  -?, -h, --help           Show help and usage information
```

## Misc

This project is under active development, you can peek the [CHANGELOG](https://github.com/JerryBian/tur/blob/master/CHANGELOG.md) for each release.

## License
[GPLv3](https://github.com/JerryBian/tur/blob/master/LICENSE)