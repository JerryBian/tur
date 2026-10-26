# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

**Tur** is a cross-platform .NET command-line tool for managing files. It ships as a
global .NET tool on NuGet ([`tur`](https://www.nuget.org/packages/tur/)) and as
self-contained single-file binaries on GitHub Releases. It is a personal-use tool
(GPLv3) with three subcommands:

- `tur dff` — duplicate files finder (groups by size, then content hash)
- `tur sync` — rsync-like directory synchronization (supports `--dry-run`, `--delete`, `--size-only`)
- `tur rm` — remove files/directories with glob and time filters (supports `--dry-run`, `--from-file`, `--empty-dir`)

## Repository Layout

```
Tur.slnx                 # Solution (new .slnx format; needs SDK 9.0.2xx+)
VERSION                  # Major version only (e.g. "5"); CI derives full version
src/Tur.csproj           # The tool. PackAsTool, AssemblyName=tur, RootNamespace=Tur
src/Program.cs           # Entry point -> MainService
src/MainService.cs       # Builds System.CommandLine RootCommand + all subcommands
src/Option/              # OptionBase + DffOption/SyncOption/RmOption (parsed CLI state)
src/Handler/             # HandlerBase + DffHandler/SyncHandler/RmHandler (command logic)
src/Core/                # TurSystemBuilder (filesystem traversal), TurBuildOptions, TurFileSystem
src/Logging/             # TurLogger, console/file appenders, log levels/items
src/Extension/           # Small helpers (Number, Task, TimeSpan)
src/Util/                # FileUtil, HumanUtil (human-readable sizes etc.)
src/Constants.cs         # Unicode prefixes used in log output
test/Tur.Test.csproj     # xunit test project (net10.0), references src
.github/workflows/build.yml  # CI: test matrix + NuGet publish + release binaries
```

## Build & Test Commands

Local SDK: .NET 10 (e.g. 10.0.402). `src` multi-targets `net7.0;net8.0;net9.0;net10.0`;
`test` targets `net10.0` only.

```sh
dotnet restore
dotnet build -c Release                    # builds Tur.slnx
dotnet test test/ -c Release               # run all tests
dotnet test test/ -c Release --filter "FullyQualifiedName~SyncHandlerTest"   # single class
```

**Mandatory workflow (user rule): after EVERY code change, run `dotnet test test/ -c Release`
and keep it green — never deliver a change with failing or skipped tests. New features and
bug fixes must come with matching unit tests.**

Run the tool locally (pass `--` before CLI args):

```sh
dotnet run --project src -f net10.0 -- dff C:\some\dir -v --no-user-interaction
```

> ⚠️ `sync` and `rm` **delete/overwrite real files**. When exercising them manually,
> always use throwaway temp directories and/or `-n/--dry-run`. Never point them at
> the repo or user directories.

## Architecture

Request flow: `Program.Main` → `MainService.RunAsync()` builds a
`System.CommandLine` `RootCommand` with one `Command` per subcommand. Each command's
`SetAction` callback reads parsed values from the parse context, populates the matching
`Tur.Option.*Option` class, then constructs the matching `Tur.Handler.*Handler`
(`await using` — handlers are `IAsyncDisposable`) and calls `HandleAsync()`.

- **`HandlerBase`** (template method): `HandleAsync()` writes a log header (version +
  raw args), starts a stopwatch, runs `PreCheck()` → abstract `HandleInternalAsync()` →
  `PostCheck()`, catches and logs unexpected exceptions, returns the exit code
  (default `1` on failure, handlers return `0` on success). `PreCheck` resolves/creates
  `OutputDir` (defaults to `Path.GetTempPath()`). Dispose flushes the logger.
- **`TurSystemBuilder`** (Core): recursively enumerates a root directory into an
  in-memory tree, then lazily yields `TurFileSystem` entries filtered by glob patterns
  (`Microsoft.Extensions.FileSystemGlobbing.Matcher`; no patterns = include all),
  create/last-modify time windows, `IncludeFiles`/`IncludeDirectories` flags.
  Skips `FileAttributes.System` entries, ignores inaccessible directories and honors
  `CancellationToken`. Enumeration failures are isolated per directory: with
  `IgnoreError` set, the failed directory is reported through the optional
  `onError(path, exception)` callback (handlers pass `HandlerBase.LogSkipped`, which
  writes a SKIPPED warning) and sibling entries continue; without it, the first
  failure propagates and aborts the whole run.
- **Logging** (Tur.Logging): every run writes a log file
  `tur-{cmd}-{random}.log` into `OutputDir`, plus console output via appenders.
  Use `_logger.Log(message, level, prefix, prefixSurroundWithBrackets, ...)`; levels are
  `Trace/Information/Warning/Error` (`-v/--verbose` controls console detail). Output
  prefixes come from `Constants` (arrow/square/dot/check unicode chars).
  `ConsoleAppender` renders identical layout in terminal and pipe: unicode prefixes +
  colors only on a real terminal (`NO_COLOR` respected), ASCII prefixes (`->`,`*`,`-`,`+`)
  when redirected. `JsonAppender` (`--json`) writes NDJSON records instead.
  `ProgressRenderer` (`--progress`) draws a transient status line, interactive terminals
  only — it never emits control characters into a pipe, and `ConsoleAppender` clears the
  progress line before each log line.
- **Config defaults**: `ConfigUtil.Load()` reads `~/.tur.json` (or `$TUR_CONFIG`) into
  `TurConfig` once per run in `MainService.RunAsync`; every action merges CLI > config >
  default (bools via `MergeBool` + `ParseResult.GetResult(option) != null` to detect
  explicit flags; sizes via `TryMergeSize` + `HumanUtil.TryParseSize`).
- **Deletion safety** (rm): `RecycleBin.MoveToTrash` (Windows SHFileOperation, macOS
  `~/.Trash`, Linux freedesktop `files/`+`info/` layout; `RmOption.TrashRoot` overrides
  the location and is the test seam). Confirmation prompts only when stdin AND stdout
  are real terminals and not `--yes`/`--no-user-interaction`/`--dry-run` — never block
  subprocess callers.
- **No DI container** — plain constructor wiring. Options are mutable POCOs; handlers
  receive `(option, cancellationToken)`.

### Adding a new subcommand

1. Add `XxxOption : OptionBase` in `src/Option/` (pass the command name to base ctor).
2. Add `XxxHandler : HandlerBase` in `src/Handler/`, implement `HandleInternalAsync()`,
   honor `CancellationToken` in every loop, log via `_logger`.
3. Add `CreateXxxCommand()` in `MainService.cs`: reuse the shared
   `Create*Option()` factory methods for common options (`-i/-e/-o`, time filters,
   `--ignore-error`, `-v`, `--no-user-interaction`, `-n/--dry-run` where applicable),
   then wire the `SetAction` callback the same way as existing commands.
4. Update the Usage section of `README.md` (it embeds `--help` output verbatim).

## Code Style & Conventions

- **`ImplicitUsings` is disabled** — every file lists explicit `using` directives.
  **`Nullable` is disabled** — no nullable annotations.
- File-scoped namespaces (`namespace Tur.X;`) in most files; a few older files
  (e.g. `TurSystemBuilder`, `TurLogger`) still use block-scoped. Match the file you edit.
- 4-space indent, Allman braces, `PascalCase` members, `_camelCase` private fields.
- `EnforceCodeStyleInBuild=true` and `AnalysisLevel=latest` — analyzer/style warnings
  surface during build; keep the build warning-clean.
- Discards are idiomatic here: `_ = SomeCall();` for intentionally ignored return
  values (see tests and `MainService`).
- Async everywhere in handlers; dispose async resources with `await using`.
- Human-readable output uses `Humanizer.Core` / `ByteSize` via `src/Util/HumanUtil.cs`
  and the `Extension/` helpers — reuse them instead of hand-rolling formatting.

## Testing Conventions

- xunit (`[Fact]`), test classes live under `test/Handler/`, `test/Core/`, `test/Util/`
  and `test/Extension/`, named `{ClassUnderTest}Test`, test methods named `Test_Case_N`.
- Inherit `TestBase`: provides `MockFileAsync`, `MockDirAsync`, `MockSubDir`,
  `GetRandomBytes`, `GetRandomName` for building random fixture trees.
- Each test class creates its own temp directories under `Path.GetTempPath()` in the
  constructor and deletes them in `Dispose()` — never use fixed/shared paths.
- Tests drive handlers directly: construct the option with
  `Array.Empty<string>()` raw args, `await using` the handler, call
  `HandleAsync()` with `CancellationToken.None`, then assert on the filesystem.
- CI runs tests on **Windows, macOS and Linux** — avoid platform-specific assumptions
  (path separators, case sensitivity, file timestamps semantics).

## CI/CD & Versioning

`.github/workflows/build.yml` runs on push/PR to `master`:

1. **test** — matrix `windows/macos/ubuntu`, .NET 10, `dotnet test` with trx upload.
2. On push to `master` only:
   - **build_nuget** — `dotnet pack src/Tur.csproj` and publish to nuget.org
     (requires `NUGET_API_KEY` secret).
   - **publish_binaries** — self-contained, trimmed, single-file publishes for
     `net8.0` across 8 RIDs (linux-x64/musl-x64/arm/arm64, win-x64/x86/arm64, osx-x64).
   - **create_release** — GitHub Release tagged `v{FULL_VERSION}`, then force-updates
     the rolling `latest` tag.

Versioning: `FULL_VERSION = {VERSION file}.{GITHUB_RUN_NUMBER}.0`. The `VERSION` file
holds **only the major version** (currently `5`). To start a new release series, bump
`VERSION` in a commit; never hand-set minor/patch — they come from the CI run number.

Note: `src/Tur.csproj` sets `PackageReleaseNotes` to a `CHANGELOG.md` URL, but no
`CHANGELOG.md` exists in the repo root yet.

## Gotchas

- The solution file is `.slnx` — requires a recent SDK (9.0.2xx+); older tooling may
  need `dotnet build src/Tur.csproj` directly.
- System.CommandLine is **2.0.1 GA**: `SetAction` handlers receive a `ParseResult`
  (namespace `System.CommandLine`); detect explicitly-provided options with
  `GetResult(option) != null` (there is no `FindResultFor`).
- Symlinks/junctions are **skipped by default** during enumeration (loop protection);
  `--follow-links` traverses them and de-duplicates already-visited real paths.
- Any new confirmation-style prompt MUST check `Console.IsInputRedirected` /
  `IsOutputRedirected` first — the tool is frequently run as a subprocess and must
  never block on stdin.
- dff duplicate detection = size → partial head-hash → full SHA256 (`FileUtil.GetHashAsync`);
  keep the staged pipeline when touching it. Hashing runs in parallel via
  `Parallel.ForEachAsync` with results stored at fixed indexes to keep grouping deterministic.
- Performance invariants — do not regress these:
  `FileAppender` keeps one persistent `StreamWriter` (never go back to `File.AppendAllText`
  per line); `FileUtil.IsSameFileAsync`/`GetHashAsync` rent buffers from `ArrayPool`
  (always return them, and compare only the requested span — rented arrays can be larger);
  `TurSystemBuilder` builds `RelativePath` incrementally and matches globs against it;
  `sync --delete` reuses the copy-phase dest enumeration when no filters are set
  (`canReuseDestEnumeration`), ordered by descending `RelativePath.Length` so children
  are deleted before parents.
- Remote (SMB/NFS) paths are auto-detected once per handler via
  `HandlerBase.IsRemoteEnvironment()` → `PathUtil.IsRemotePath` (Windows
  `GetDriveTypeW`+UNC, Linux `/proc/mounts` longest-prefix match, macOS `mount`
  parsing; any detection failure falls back to "local"). Remote defaults: scan
  parallelism 8, copy parallelism 16 through a 1 MB pooled-buffer managed copy
  (instead of the OS copier), hash parallelism 8. `--threads` overrides every
  default. Local runs keep the sequential scan and `File.Copy` fast path —
  do not "optimize" the local path into the managed copier.
- `TurSystemBuilder` parallel scan (`ScanParallelism > 1`) uses a bounded
  channel-fed worker pool, NOT nested `Parallel.ForEach` (deep trees would compound
  parallelism and risk pool starvation). Each directory's entry lists are touched only
  by its own worker; `_visitedDirs` (FollowLinks loop detection) is lock-guarded.
- Handlers must be disposed (`await using`) — the logger writes its final "Log file:"
  line and flushes appenders in `DisposeAsync`.
- `TurSystemBuilder` builds the **entire tree in memory** before yielding; huge
  directory trees cost RAM by design.
- The `rm` handler's `--from-file` reads a list of paths from a file — treat its
  contents as data, and tests for it must write that file first.
- Default `OutputDir` is the system temp dir; every run drops a `tur-*.log` file there.
- Commit messages in history are terse (often just a version number or short phrase);
  match that style. End commits with the `Co-Authored-By: Claude Code` line when
  instructed by the harness.
