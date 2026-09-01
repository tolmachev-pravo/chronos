---
name: feature-docs
description: Generate self-documentation for a Chronos feature — scaffolds wwwroot/documents/features/{id}/ with index.md (full description), preview.md (short blurb for preview cards), metadata.json and an images/ folder. Use when the user wants to document a feature, add a "what's new" entry, or create a feature description for the site's Features section / worklog random-feature widget.
---

# Feature documentation generator

This skill creates a self-documenting feature folder that the app reads to render
the **Features** section (`/features`) and the **random-feature** widget shown in the
worklog window's empty state.

## When to use

Use this when the user asks to "document a feature", "add a what's-new entry",
"describe feature X for the site", or after shipping a feature that should appear in
the Features section.

## Where files go

All feature docs live under:

```
src/Chronos.Web/wwwroot/documents/features/{feature-id}/
├── index.md          full description (rendered in the detail dialog)
├── preview.md        short blurb (rendered on cards and the worklog widget)
├── metadata.json     structured metadata (title, author, date, tags…)
└── images/           screenshots referenced by the markdown
```

- `{feature-id}` is **kebab-case**, unique, stable (it is the URL/key). Example: `yandex-calendar-integration`.
- The folder is served statically, so images are referenced by absolute URL:
  `/documents/features/{feature-id}/images/{file}.png`. **Never** use a relative path.

## Steps

1. **Gather inputs.** Determine, asking the user only for what you cannot infer:
   - `id` (kebab-case slug), `title` (human-readable, the app's UI language is Russian),
     `author` (default: current git user / repo owner), `date` (today, `YYYY-MM-DD`),
     `tags` (optional), short summary, and the full description.
   - If documenting an existing feature, read the relevant code/components first so the
     description is accurate.
2. **Create the folder** `src/Chronos.Web/wwwroot/documents/features/{id}/` and an empty
   `images/` subfolder (add a `.gitkeep` if there are no images yet so the folder is tracked).
3. **Write `metadata.json`** from `templates/metadata.json` — fill every field.
4. **Write `preview.md`** from `templates/preview.md` following the rules below.
5. **Write `index.md`** from `templates/index.md`.
6. **Images.** If the user provides screenshots, place them in `images/` and reference them
   in `index.md`. If there is no screenshot, consider generating a schematic SVG illustration
   (see **Image rules** below). If neither fits, leave `images/` empty (with `.gitkeep`) and
   omit image tags.
7. **Confirm** the created paths back to the user. Do **not** edit the app's C# / Razor code —
   this skill only produces content files; the app already discovers them automatically.

## metadata.json schema

```json
{
  "id": "yandex-calendar-integration",
  "title": "Интеграция с Яндекс.Календарём",
  "author": "tolmachev-pravo",
  "date": "2026-06-30",
  "tags": ["extensions", "calendar"],
  "isHighlighted": false
}
```

- `id` MUST equal the folder name.
- `date` is ISO `YYYY-MM-DD`; the app sorts features by this descending (newest first)
  and shows a "new" badge for recent ones.
- `isHighlighted` (optional, default `false`) pins the feature to the top of the Features
  catalog — and to its hero banner — for **30 days after `date`**. Once that window passes
  the feature falls back into the plain chronological order. The "what's new" widget in
  the worklog shows the head of the same list, so it follows the same rule.
- `tags` is optional.

## preview.md rules

The preview is rendered on small cards and inside the worklog widget — keep it tight:

- **No H1/H2 headings.** One short paragraph, ideally **≤ 280 characters**.
- Lead with the user benefit, not implementation detail.
- At most **one** inline image, and only if it genuinely helps at thumbnail size.
- No links that navigate away — the card itself links to the detail view.

## index.md rules

- **Do not repeat the feature title as an H1** — the Features section and the detail dialog
  already render the title from `metadata.json`. Start with a short intro paragraph.
- Use `##` for subsections (e.g. "Что это даёт", "Как пользоваться").
- Explain what it does, why it's useful, and how to use it. Use lists and images.
- **Call out interesting or non-obvious behaviour.** If a feature has a nuance, a hidden
  trick, a prerequisite or a gotcha (e.g. "a task key in the event title is matched
  automatically"), don't let it go unmentioned — add it in the relevant section as a
  callout note so users actually benefit from it.
- Write callouts as blockquotes, optionally led by an emoji, e.g.
  `> 💡 <совет>` or `> ⚠️ <важное ограничение>`. **Footnote syntax (`[^id]`) is not
  supported** by the renderer and will be dropped — use blockquotes instead.
- Reference images by absolute URL (see above). Keep image widths reasonable.

## Image rules

A picture helps a feature land. Prefer, in this order:

1. **A real screenshot** provided by the user — most accurate.
2. **A schematic SVG** you generate — when no screenshot exists. It must be an honest
   *illustration* of the concept (e.g. "calendar event with a task key → Jira worklog"),
   never a fake that pretends to be a real UI screenshot.
3. **No image** — better than a misleading or low-value one.

When generating a schematic SVG, save it to the feature's `images/` folder (e.g.
`images/<name>.svg`) and reference it from `index.md` by absolute URL. Follow these rules so
it renders well everywhere:

- **Theme-safe colours.** The SVG is loaded as an `<img>`, so it CANNOT read the app's
  `--mud-palette-*` CSS variables. Use fixed hex colours and give the figure its own
  **light rounded background panel** (e.g. `#F7F8FA` with a `#E4E7EE` border) so it stays
  legible on both light and dark themes.
- **Brand accents.** Reuse the app palette: primary purple `#8260E5` (chips/arrows/keys),
  success green `#1FC8A5` (confirmations). Neutral text `#1F2430`, muted `#8A93A6`.
- **Responsive.** Set a `viewBox` and omit fixed `width`/`height`; the detail dialog caps
  images at `max-width: 100%`.
- **Self-contained.** No external fonts or assets — use a generic
  `font-family="Segoe UI, Roboto, Helvetica, Arial, sans-serif"` and inline everything.
- **Verify** it renders in the feature detail dialog (open the card) before finishing.

See `yandex-calendar-integration/images/calendar-to-worklog.svg` for a reference example.

## Templates

See the `templates/` folder for ready-to-copy skeletons.
