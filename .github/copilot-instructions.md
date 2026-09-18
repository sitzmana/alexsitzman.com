# Copilot instructions

Personal portfolio for Alex Sitzman. C# and Razor components rendered to **static HTML at
build time**. No database, no API, no server-side runtime in production.

## The rule that matters most

**Content lives in files, never in code.**

If a task is "add a project", "update the bio", "change the job title", or "add a page",
the answer is a Markdown or YAML file under `content/` — not a `.razor` edit, not a C#
collection initialiser, not a hard-coded string.

Adding `content/pages/040-uses.md` creates `/uses/` and adds it to the navigation on every
page. No code change. Preserve that property in anything you build.

## Layout

```
content/      Markdown + YAML. The only thing edited to change the site.
assets/       tokens.css, base.css, layout.css, components.css, motion.css, enhance.js
static/       Copied verbatim to the site root
src/Portfolio.Content      Models + loader. No HTML knowledge.
src/Portfolio.Components   Razor components. Presentation only. No I/O.
src/Portfolio.Generator    Console app. HtmlRenderer → files.
tests/Portfolio.Tests      xUnit, 143 tests.
tests/browser              Optional local Playwright tooling; required in CI.
dist/                      Generated. Git-ignored. Never commit.
```

## Commands

```pwsh
dotnet run --project src/Portfolio.Generator -- --serve   # preview at :5000, watches files
dotnet test Portfolio.slnx                                # 143 tests
dotnet build Portfolio.slnx -c Release
```

Do not claim a change works without running these.

## Non-negotiables

1. **Content is visible by default.** `<html>` ships with `class="no-js"`; an inline script
   swaps it to `js`. Only `.js .reveal.is-pending` is hidden, after the enhancement
   registers an offscreen element with its observer. The hero is never hidden.
   Disabled or blocked JavaScript must leave every content element visible.
2. **`prefers-reduced-motion: reduce` disables all motion** and reveals everything
   immediately.
3. **No design value is hard-coded.** Use a token from `tokens.css`. If none fits, add one.
4. **Nothing is signalled by colour alone.** Pair with text, weight, shape, or ARIA.
5. **Every link has a text label.** No icon-only interactive elements. No third-party brand
   logos.
6. **A content mistake fails the build, naming the file.** YAML binds without
   `IgnoreUnmatchedProperties` on purpose — a typo must never silently drop an entry.
7. **No new dependency without a decision record** in `docs/decisions.md`. Client-side
   payload must stay at zero.
8. **Never invent facts** about Alex — employers, dates, metrics, certifications, project
   details. Unknown information goes in `docs/content-review-needed.md`.

## Conventions

- Entries are `<collection>/<slug>.md` or `<collection>/<slug>/index.md` (folder form
  co-locates images).
- A `020-` filename prefix sets sort order and is stripped from the slug. Front-matter
  `order:` shares the same number line and wins.
- `draft: true` excludes an entry.
- A page's `sections:` keys resolve through `SectionRegistry`. An unknown key fails the
  build and lists the valid keys.
- Page contents derive from Markdown h2 headings and declared sections, never a second outline.
- Body anchors use `<collection>-<slug>--<heading>`; local Markdown heading links are rewritten to match.
- Hero and section copy live in `content/site.yml`. Every used non-hero section needs
  a lowercase `sections` entry with a title.
- `visual: network`, `stack`, or `signal` selects decorative project artwork.
- Hero project links and their destinations share `FeaturedProjects`; do not duplicate the selection.
- Hero deck links stay stationary; animate only their pointer-transparent inner surfaces within reserved clearance.
- Repositories, activity, and listening are dated file-backed snapshots, not live feeds.
- Listening entries require an artist/link; section copy requires previous/next labels.
- Explorer is derived from entries and declared topics; never maintain a second catalog or infer proficiency.
- Source destinations use `SectionRoute`; credential `featured: true` selects the home highlight.
- Warnings are errors. Do not suppress; fix.
- Nullable reference types are enabled throughout.
- Do not name a Razor loop variable `page` — `@page` parses as a directive.

## Performance budget

Home page HTML + CSS + JS must stay under **150 KiB uncompressed**. Currently 92.8 KiB
(17.5 KiB gzipped). `scripts/check-performance.ps1` checks exact bytes. There are no webfonts, no
frameworks, and no third-party requests; keep it that way.

## Before saying something is done

- `dotnet test Portfolio.slnx` passes
- `dotnet build -c Release` is clean
- The site generates
- Visual changes checked at 320 px and 1440 px, with reduced motion, and with JS disabled
- Run `tests/browser/check_site.py` for visual or enhancement changes; it also exercises
  blocked scripts, keyboard/touch controls, forced colors, and runtime motion preferences.
- No secret, token, or credential added to any tracked file

Full context: `docs/architecture.md`, `docs/decisions.md`, `docs/design-system.md`.
