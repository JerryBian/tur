# Changelog

## Unreleased

### Added

- `rm`: `--trash` moves items to the system trash (Windows Recycle Bin, macOS Trash,
  freedesktop Trash on Linux) instead of deleting permanently.
- `rm`: interactive confirmation before deleting — only on real terminals; never
  blocks subprocess callers (skipped when input/output is redirected). `-y/--yes`
  skips the prompt.
- `dff`: `--write-list <file>` writes the duplicates to delete (all but the kept one
  per group) for `tur rm --from-file`; `--keep first|newest|oldest` selects the keeper.
- `dff`: three-stage duplicate detection (size → partial head hash → full SHA256),
  replacing O(n²) pairwise full-file comparison.
- `sync`: `--exclude-from <file>`, `--delete-excluded`, `--bwlimit <rate>` copy
  throttling.
- All commands: `--min-size` / `--max-size`, `--max-depth`, `--threads`,
  `--follow-links`, `--json` (NDJSON output), `--progress` (interactive terminals only).
- Config file `~/.tur.json` (or `$TUR_CONFIG`) providing defaults; CLI wins.
- Enumeration failures are isolated per directory and reported through the logger
  (`SKIPPED` warnings) when `--ignore-error` is set.

### Changed

- Symbolic links/junctions are skipped by default (previously followed, with an
  infinite-recursion risk); `--follow-links` traverses them with loop detection.
- Exit codes: `sync`/`rm` now return 1 when a file operation failed without
  `--ignore-error` (previously returned 0 despite logged errors).
- Console output: identical layout in terminal and pipe, ASCII prefixes when
  redirected, colors only on interactive terminals (respects `NO_COLOR`).
- Summaries: byte totals, `(dry run)` annotation, correct pluralization, and
  `Completed with errors.` footer on failure.

### Performance

- File log appender keeps one open handle (was open/write/close per line — the dominant
  cost on operations logging tens of thousands of lines).
- Content comparison and hashing buffers come from `ArrayPool` (was 2×3MB LOH allocations
  per compared pair); hashing uses the static `SHA256.HashData/HashDataAsync` APIs.
- dff hashes files in parallel (`--threads`, default = processor count).
- Directory enumeration builds relative paths incrementally instead of
  `Path.GetRelativePath` per entry, and glob matching reuses the already-computed
  relative path instead of recomputing it.
- `sync --delete` reuses the copy-phase destination enumeration when no filters are set
  (skips a second full walk of the destination tree).
- Redirected (non-color) console output is assembled per line and written with a single
  `Console.Write` instead of 5+ synchronized writes.
- sync copy loop defaults to processor-count parallelism, avoiding thread-pool injection
  stalls on large trees.
- Network shares (SMB/NFS/SSHFS) are auto-detected (`PathUtil`): directory scanning
  switches to a bounded channel-fed worker pool (8 concurrent directories), sync copies
  run at concurrency 16 through 1 MB pooled buffers with sequential-scan/async hints
  instead of the OS copier, and dff hashing runs at concurrency 8 — latency-bound links
  need round-trip overlap. Local behavior is unchanged; `--threads` overrides all of it.

### Fixed

- `dff`/`rm` time filters (`--last-modify-*`, `--create-*`) were silently not applied
  (file attributes were never read during enumeration).
- `rm --from-file` without a destination crashed with
  `Value cannot be null. (Parameter 'path')`.
- Console appender mutated shared log items across threads (race with file appender).
