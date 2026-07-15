# FunWithGithub

A CLI tool that turns the GitHub contribution graph (7×52 tiles) into a
drawing canvas: a text string is rendered through a pixel font and written
back as backdated commits, so it appears as pixel art in the contribution
graph.

For details on the concept and known limitations, see [VISION.md](VISION.md).

## Build & run

```
dotnet build GitHubCanvas.slnx
```

Run from inside the target git repository (the tool operates on the current
working directory):

```
dotnet run --project src/GitHubCanvas -- --text "HI" --start-date 2026-01-04 --commits-per-date 2
```

## CLI parameters

| Parameter               | Required | Description                                                        |
|--------------------------|:--------:|----------------------------------------------------------------------|
| `--text`                | yes      | Text to render (A-Z, 0-9, space).                                    |
| `--start-date`          | yes      | Calendar date (`yyyy-MM-dd`) for column 0 of the grid. Must be a Sunday. |
| `--commits-per-date`    | no       | Commits to create per lit pixel (default: `1`).                      |
| `--author`              | no       | `"Name <email>"` for the generated commits. Defaults to the repo's configured git identity. |

The tool prints a preview of the grid and asks for confirmation before
creating any commits. It never rewrites history or pushes — pushing the
result is a manual, separate step.