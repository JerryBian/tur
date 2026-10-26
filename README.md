# Tur

[![master](https://github.com/JerryBian/tur/actions/workflows/build.yml/badge.svg)](https://github.com/JerryBian/tur/actions/workflows/build.yml)

Command line tool to manage files.

_This is developed only for my personal daily usage purposes. It's not intended to fit all requirements for everyone. Use it at your own risk._

## Installation

### Recommended

Install via NuGet [package](https://www.nuget.org/packages/tur/).

```sh
dotnet tool install -g tur
```

For tab completion, please refer to [dotnet-suggest](https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md).

### Alternative

Go to the [Release page](https://github.com/JerryBian/tur/releases/tag/latest) and download the binary according to your computer architecture. The binary is a self-contained executable file, making it independent of any other files on your system.

## Usage

At this moment, `tur` supports three subcommands:
- [`dff`](#dff)
- [`sync`](#sync)
- [`rm`](#rm)

In general, the syntax looks like:

```sh
tur [command] [options]
```

### `dff`

```sh
PS C:\Users\jerry> tur dff --help
Description:
  Duplicate files finder for target directories.

Usage:
  tur dff <dir>... [options]

Arguments:
  <dir>  The target directories to analyze.

Options:
  -i, --include <i>                          Glob patterns for included files.
  -e, --exclude <e>                          Glob patterns for excluded files.
  -o, --output <o>                           The output directory for logs or any file generated during processing.
  --last-modify-after <last-modify-after>    Last modify time after filter. e.g., 2022-10-01T10:20:21
  --last-modify-before <last-modify-before>  Last modify time before filter. e.g., 2022-08-02T16:20:21
  --create-after <create-after>              Create time after filter. e.g., 2022-07-01T10:20:21
  --create-before <create-before>            Create time before filter. e.g., 2022-12-02T16:20:21
  --ignore-error                             Ignore errors during file processing.
  -v, --verbose                              Enable logging in detailed mode.
  --no-user-interaction                      Indicates running environment is not user interactive mode.
  --write-list <write-list>                  Write duplicates to delete (all but the kept one per group) to the specified file, one path per line. Feed it to 'tur rm --from-file'.
  --keep <keep>                              Which duplicate to keep with --write-list: first (default, alphabetical), newest, oldest.
  --min-size <min-size>                      Only process files with size greater than or equal to this value. e.g., 1024, 10mb
  --max-size <max-size>                      Only process files with size less than or equal to this value. e.g., 1024, 10mb
  --max-depth <max-depth>                    Do not descend into directories deeper than this level (direct children of the root = 1).
  --threads <threads>                        Maximum degree of parallelism for file processing. Unlimited by default.
  --json                                     Write machine-readable NDJSON lines to stdout instead of human-readable output.
  --progress                                 Show a transient progress line (interactive terminals only; never written when output is redirected).
  --follow-links                             Follow symbolic links/junctions (loops are detected and skipped). Links are skipped by default.
  -?, -h, --help                             Show help and usage information
```

Duplicate detection runs in three stages for speed: group by size, then compare a
partial (head 4KB) SHA256 hash, then a full SHA256 hash. Typical dedup workflow:

```sh
tur dff D:\photos --min-size 1mb --keep newest --write-list dups.txt
tur rm --from-file dups.txt --dry-run --trash   # preview what would be trashed
tur rm --from-file dups.txt --trash -y          # move duplicates to recycle bin
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
  <dest>  The destination directory.

Options:
  -i, --include <i>                          Glob patterns for included files.
  -e, --exclude <e>                          Glob patterns for excluded files.
  -o, --output <o>                           The output directory for logs or any file generated during processing.
  --last-modify-after <last-modify-after>    Last modify time after filter. e.g., 2022-10-01T10:20:21
  --last-modify-before <last-modify-before>  Last modify time before filter. e.g., 2022-08-02T16:20:21
  -v, --verbose                              Enable logging in detailed mode.
  --no-user-interaction                      Indicates running environment is not user interactive mode.
  -n, --dry-run                              Perform a trial run with no changes made.
  -d, --delete                               Delete extraneous files from destination directory.
  --size-only                                Skip files that match in both name and size.
  --exclude-from <exclude-from>              Read exclude glob patterns from the specified file (one per line, blank lines and '#' comments ignored).
  --delete-excluded                          Also delete excluded files from the destination directory.
  --bwlimit <bwlimit>                        Limit copy throughput per second, e.g. 10mb. Omitted or 0 means unlimited.
  --create-after <create-after>              Create time after filter. e.g., 2022-07-01T10:20:21
  --create-before <create-before>            Create time before filter. e.g., 2022-12-02T16:20:21
  --ignore-error                             Ignore errors during file processing.
  --min-size <min-size>                      Only process files with size greater than or equal to this value. e.g., 1024, 10mb
  --max-size <max-size>                      Only process files with size less than or equal to this value. e.g., 1024, 10mb
  --max-depth <max-depth>                    Do not descend into directories deeper than this level (direct children of the root = 1).
  --threads <threads>                        Maximum degree of parallelism for file processing. Unlimited by default.
  --json                                     Write machine-readable NDJSON lines to stdout instead of human-readable output.
  --progress                                 Show a transient progress line (interactive terminals only; never written when output is redirected).
  --follow-links                             Follow symbolic links/junctions (loops are detected and skipped). Links are skipped by default.
  -?, -h, --help                             Show help and usage information
```

### `rm`

```sh
PS C:\Users\jerry> tur rm --help
Description:
  Remove files or directories.

Usage:
  tur rm [<dest>] [options]

Arguments:
  <dest>  The destination directory.

Options:
  -i, --include <i>                          Glob patterns for included files.
  -e, --exclude <e>                          Glob patterns for excluded files.
  -o, --output <o>                           The output directory for logs or any file generated during processing.
  --last-modify-after <last-modify-after>    Last modify time after filter. e.g., 2022-10-01T10:20:21
  --last-modify-before <last-modify-before>  Last modify time before filter. e.g., 2022-08-02T16:20:21
  -v, --verbose                              Enable logging in detailed mode.
  --no-user-interaction                      Indicates running environment is not user interactive mode.
  -f, --file                                 Delete files only.
  -d, --dir                                  Delete directories only.
  --empty-dir                                Delete all empty directories.
  --from-file <from-file>                    Delete all files/directories listed in specified file.
  --trash                                    Move items to the system trash/recycle bin instead of deleting permanently.
  -y, --yes                                  Skip the confirmation prompt. Prompts never appear when input is redirected (subprocess safe).
  --create-after <create-after>              Create time after filter. e.g., 2022-07-01T10:20:21
  -n, --dry-run                              Perform a trial run with no changes made.
  --create-before <create-before>            Create time before filter. e.g., 2022-12-02T16:20:21
  --ignore-error                             Ignore errors during file processing.
  --min-size <min-size>                      Only process files with size greater than or equal to this value. e.g., 1024, 10mb
  --max-size <max-size>                      Only process files with size less than or equal to this value. e.g., 1024, 10mb
  --max-depth <max-depth>                    Do not descend into directories deeper than this level (direct children of the root = 1).
  --threads <threads>                        Maximum degree of parallelism for file processing. Unlimited by default.
  --json                                     Write machine-readable NDJSON lines to stdout instead of human-readable output.
  --progress                                 Show a transient progress line (interactive terminals only; never written when output is redirected).
  --follow-links                             Follow symbolic links/junctions (loops are detected and skipped). Links are skipped by default.
  -?, -h, --help                             Show help and usage information
```

## Safety and subprocess guarantees

- `rm` asks for confirmation **only on a real interactive terminal**. When stdin or
  stdout is redirected (i.e. called as a subprocess), prompts never appear and the
  command never blocks. Use `-y/--yes` to skip the prompt explicitly, `-n/--dry-run`
  to preview, and `--trash` to make deletions recoverable.
- Exit codes: `0` on success, `1` on any error, invalid argument, or user abort.
  Per-file failures without `--ignore-error` also result in exit code `1`.
- When output is redirected, unicode prefixes (`→ ▪ • √`) are automatically replaced
  with ASCII (`-> * - +`) and colors are disabled, so piped logs never contain
  mojibake or ANSI control characters.
- `--json` writes one NDJSON record per event (`ts`, `level`, `message`, `suffix`,
  `error`) for machine consumers.
- Symbolic links and junctions are skipped by default, preventing link-loop
  recursion. Use `--follow-links` to traverse them; loops are detected and skipped.

## Network paths (SMB / NFS)

`tur` detects automatically when a source/destination path lives on a network
share — UNC paths and mapped drives via `GetDriveType` on Windows, `/proc/mounts`
on Linux, `mount` output on macOS — and switches I/O strategy, because
high-latency links need concurrency to hide round trips:

| | Local | Remote (auto-detected) |
|---|---|---|
| Directory scan | sequential | 8 directories in parallel |
| sync copy concurrency | processor count | 16 |
| sync copy path | OS-native `File.Copy` | 1 MB pooled buffers, sequential-scan hints, async I/O |
| dff hash concurrency | processor count | 8 |

Detection happens once per run and any detection failure falls back to "local"
(never breaks a run). Recognized remote file systems: `cifs`, `smbfs`, `smb2`,
`smb3`, `nfs`, `nfs4`, `fuse.sshfs`, `afs`. `--threads N` overrides every
concurrency default explicitly, and also enables parallel directory scanning on
local disks.

## Configuration file

Defaults can be stored in `~/.tur.json` (or the file pointed to by the `TUR_CONFIG`
environment variable). Explicit command line arguments always win:

```json
{
  "includes": null,
  "excludes": ["**/*.tmp", "**/node_modules/**"],
  "output": "D:\\tur-logs",
  "minSize": "1kb",
  "maxDepth": null,
  "threads": 4,
  "bwlimit": "50mb",
  "ignoreError": true,
  "trash": true,
  "json": false,
  "progress": true,
  "followLinks": false,
  "noUserInteraction": false,
  "yes": false,
  "verbose": false,
  "keep": "newest",
  "deleteExcluded": false
}
```

A missing or malformed config file is not fatal; a warning is written to stderr and
built-in defaults are used.

## License
[GPLv3](https://github.com/JerryBian/tur/blob/master/LICENSE)