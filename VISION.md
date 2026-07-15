# Vision: GitHub Contribution Canvas Art

## Idea

The GitHub contribution graph (the green tiles on a profile) is essentially
a bitmap: **7 rows** (days of the week, Sunday–Saturday) × **52–53 columns**
(calendar weeks of a year). The shade of green in a tile depends on how many
commits count for that day.

We want to use this grid as a drawing canvas: a CLI tool renders a text
string through a built-in pixel font onto the 7×52 grid and generates
backdated commits from it, so the text appears as "pixel art" in the
contribution graph.

## Non-goal

No history rewriting, no force-push. The tool only adds new, additional
commits (optionally via `--allow-empty` or a trivial file change) — existing
commit history in a repo stays untouched. This makes it low-risk to use even
in existing, "real" repos.

## How it works (roughly)

1. **Text → bitmap**: A built-in, hardcoded dot-matrix font (e.g. 5×7 or 4×7
   pixels per character including spacing) renders the given string into a
   7×N pixel grid (0/1 per pixel, no anti-aliasing — not meaningful with so
   few pixels).
2. **Pixel → calendar day**: Column 0 corresponds to `--start-date` (must be
   a Sunday, since GitHub weeks start on Sunday), row 0 = Sunday, row 6 =
   Saturday. Each pixel maps to a specific calendar date this way.
3. **Pixel → commits**: For every "on" pixel, `--commits-per-pixel` commits
   are created with author/committer date backdated to that day (time of day
   arbitrary/distributed). "Off" pixels produce no commits.
4. **Execution**: Commits are created either via a git process call (`git
   commit --allow-empty --date=...` + `GIT_AUTHOR_DATE`/`GIT_COMMITTER_DATE`)
   or alternatively via a git library (e.g. LibGit2Sharp) — interchangeable,
   not a core architectural decision.

## Planned CLI parameters (draft)

| Parameter               | Meaning                                                           |
|--------------------------|--------------------------------------------------------------------|
| `--text`                | The string to render                                                |
| `--commits-per-pixel`   | Number of commits per "on" pixel (controls relative darkness)       |
| `--start-date`          | Calendar date for column 0 (must be a Sunday, otherwise warning/error)|
| `--repo-path`           | Target repo (default: current directory)                            |
| `--branch`              | Target branch (default: current branch)                             |
| `--dry-run`             | Preview only (see below), no commits                                |
| `--author-name/--email` | Optional override, otherwise falls back to git config                |

## Dry run / preview

Before actually creating commits, the tool should render the grid as
colored console output (similar to the real contribution graph), including
how many commits would be created on which days. Important, since a typo
could otherwise quickly produce hundreds of unwanted commits.

## Known limitations / caveats

- **Relative shading**: GitHub grades green shades relative to your own
  commit history (quantiles), not fixed thresholds. More commits per pixel
  tends to produce a darker tile, but there's no guaranteed exact step.
- **GitHub's counting conditions**: Commits only count toward the
  contribution graph if they land on the default branch (or via a merged
  PR) and the commit author email matches a verified email on the GitHub
  account. The tool can't enforce this — it's up to the user (branch
  choice, push/merge, git configuration).
- **Limited width**: A year only offers 52–53 columns. Longer text has to
  be truncated or would need multiple years/start dates (a later extension,
  not a v1 goal).
- **Font coverage**: v1 will likely only cover A–Z/0–9 and a few
  punctuation marks.

## Open questions for later iterations

- Should there be a `--push` switch that automatically pushes after commit
  creation, or does that always stay a manual step?
- Should image import (PNG → grid) be added as an alternative to the text
  renderer?
- Should support for private contributions (a GitHub profile setting) be
  documented?
