---
name: release
description: Cut a Chronos release — works out the next major/minor/patch version from the latest v* tag, drafts the Russian release notes from what was merged, shows them to the user for approval, and only then tags, pushes and fills in the GitHub release. Use when the user asks to release, cut a release, bump the version, or ship a major/minor/patch.
---

# Release

Releases are tag-driven. Pushing a `vX.Y.Z` tag makes [release.yml](../../../.github/workflows/release.yml)
build and test the tagged commit, publish the app, attach the archive and open a **draft**
GitHub release. Publishing that draft is what deploys to production
([deploy-iis.yml](../../../.github/workflows/deploy-iis.yml)).

This skill covers everything around the tag: picking the version, writing the notes,
getting them approved, and putting the approved text on the release.

**Nothing about the version lives in the source.** MinVer derives it from the tag, so never
edit a csproj, `Directory.Build.props` or any version constant as part of a release.

## Hard rules

- **Never push the tag before the user has approved the notes text.** Presenting the draft
  and getting an explicit yes is the point of this skill.
- **Never publish the release.** Publishing deploys to production — the user does that, or
  asks for it in a separate, explicit instruction.
- **Never move or delete an existing tag** without the user asking for it.

## Steps

### 1. Check the ground

- The working tree is clean and the branch is `master`, in sync with `origin/master`
  (`git fetch origin && git status -sb`). If not, stop and say what is off — releasing from a
  stale or dirty master produces a release that matches nothing.
- Current version: `git describe --tags --abbrev=0 --match 'v*'`. Confirm the tag is an
  ancestor of HEAD.
- Verify the tag you are about to create does not exist yet (`git tag --list vX.Y.Z` and
  `git ls-remote --tags origin`).

### 2. Work out the next version

`vMAJOR.MINOR.PATCH`, [SemVer](https://semver.org/lang/ru/):

| Bump | When | 1.5.0 becomes |
|---|---|---|
| major | incompatible changes, a migration the user has to do | 2.0.0 |
| minor | new features, backwards compatible | 1.6.0 |
| patch | fixes only | 1.5.1 |

If the user named the bump, use it. If they just said "релиз", read the merged changes first
(step 3), then propose a bump with one sentence of reasoning and ask for confirmation — do not
silently guess.

### 3. Collect the material

```bash
prev=$(git describe --tags --abbrev=0 --match 'v*')
git log --first-parent --pretty=format:'%s' "$prev..HEAD"          # what was merged
git diff --name-only "$prev..HEAD" -- src/Chronos.Web/wwwroot/documents/features/  # new feature docs
```

- Merge subjects carry PR numbers; commit subjects carry issue numbers like `(#245)`.
- Read the referenced issues (`gh issue view <n>`) when a subject does not make the user-facing
  point obvious. Commit messages describe the change; release notes describe the benefit.
- A new folder under `documents/features/` is a strong signal of a headline feature — its
  `preview.md` is often the best starting text.
- Ignore pure chores that no user can observe (formatting, test-only changes) unless they are
  worth a line in **🧰 Прочее**.

### 4. Draft the notes

Follow [.github/RELEASE_TEMPLATE.md](../../../.github/RELEASE_TEMPLATE.md) — it is the source of
truth for the format. The essentials:

- Title: `vX.Y.Z — <тема релиза>`.
- Russian, phrased as user benefit, never a retelling of commits.
- Groups **✨ Новые возможности** / **🐛 Исправления** / **🧰 Прочее**; drop empty groups.
- Reference PRs and issues as `#<number>`.
- End with the compare link `<prev-tag>...<new-tag>`.

Fill in real text — this skill exists so the user gets finished notes, not the skeleton
that `release.yml` would put there on its own.

### 5. Get it approved

Show the user the full proposed **title and body** in chat and ask to approve, edit, or cancel.
Apply any wording they ask for and show the result again. Do not proceed on a vague reply.

### 6. Tag and push

```bash
git tag -a vX.Y.Z -m "vX.Y.Z — <тема релиза>"
git push origin vX.Y.Z
```

An annotated tag, because the message doubles as the release theme in `git tag -n`.

### 7. Put the approved text on the draft

`release.yml` creates the draft with a skeleton body; the approved text has to replace it.
Wait for the run to finish, then edit the release:

```bash
gh run list --workflow=Release --limit 1                 # watch it, ~2-3 minutes
gh release edit vX.Y.Z --title "vX.Y.Z — <тема>" --notes-file <path-to-notes.md>
```

Write the body to a file first (scratchpad is fine) and pass `--notes-file`. Never inline
multi-line Russian text into a shell argument.

### 8. Hand it over

Report to the user:

- the draft release URL (`gh release view vX.Y.Z --json url -q .url`);
- that the artifact is attached and the notes are in place;
- that **publishing the draft deploys to production**, and that this is their call.

## When something goes wrong

- **`release.yml` failed** — the tag exists but there is no release, so nothing shipped. Fix
  the problem on master and release the next patch version. Prefer that over force-moving a
  tag: a tag is a one-off label.
- **The user wants no deploy yet** — that is already the default: the draft sits there until
  published.
