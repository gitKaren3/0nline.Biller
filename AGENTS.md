# AGENTS.md

Repo-specific guidance for `Biller/`. For cross-repo architecture, conventions, and code-generation workflow, see the workspace-level [`../AGENTS.md`](../AGENTS.md).

## Build

Always build with an artifacts path — the tree sits on a Samba mount where MSBuild cannot set mtime on files owned by another user:

```bash
dotnet build Z_0nline.Biller.slnx --artifacts-path /tmp/opencode/biller-artifacts -v q --nologo
```

Never omit `--artifacts-path`. Without it you get MSB3374 errors on `.Up2Date` markers, and the build writes into the tree's `obj/` and `bin/` — ignored now, but there is no reason to keep them there.

## Filesystem permissions on the share

This tree sits on a Samba mount shared with Windows. Files created from the Linux
side are not group-writable by default, so the Windows build fails with
"Access to the path ... is denied" and new untracked files can be edited only by
the Linux user that created them.

Always set the umask before writing:

```bash
umask 002
```

After creating new files, make them group-writable (and directories setgid) so the
Windows side can edit them too:

```bash
find . -type f ! -perm -g+w -print0 | xargs -0 chmod g+w
find . -type d ! -perm -g+w -print0 | xargs -0 chmod g+ws
```

## Git

Git on this Samba mount needs both `-C` and `safe.directory` (dubious ownership otherwise):

```bash
git -c safe.directory=/srv/samba/shared/Projects/0nline/Biller \
    -C /srv/samba/shared/Projects/0nline/Biller status --short
```

No global `user.name`/`user.email` is configured — pass `-c user.name=... -c user.email=...` when committing.

## Tenancy

Which tables are isolated and which are not is in [`docs/tenancy.md`](docs/tenancy.md).
The shared mechanism — the claim, the middleware, the opt-in provider rule — is in
`Workspace/docs/tenancy.md`. Read it before touching a query provider or an update path.

## Tests

xUnit + bUnit. All test classes inherit `MudTestContext` (`Test/Base/`), which stubs MudBlazor JSInterop. Render components with `Render<T>()`.

```bash
# Run all tests
dotnet test Z_0nline.Biller.slnx

# Run a single test class
dotnet test Z_0nline.Biller.slnx --filter "FullyQualifiedName~Sample"
```

## Conventions

- Namespaces start with `_0nline` (leading underscore — C# identifiers can't start with a digit). Namespace mirrors folder structure.
- Database tables: singular, PascalCase. Enum tables use stable keys (no identity), `tinyint`/`smallint`.
- API route pattern: `api/biller/{entity}`. All endpoints require authorization.
- Lookups are app-specific: `DL/SqlQueryGenerator.json` (`LookupLabels`, `LookupFilters`) decides how a row is labelled and which rows a picker offers. Biller labels a `Contact` as `Surname, FirstName` and keeps the tenant's own profile out of the client pickers; the generated `LookupQuery` is not hand-edited.
- MudBlazor is the UI library. Never guess its behaviour — decompile with `ilspycmd` if unsure.
