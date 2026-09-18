# Contributing

## Changing content

You almost certainly want [content/README.md](content/README.md), not this file.
Adding a project, a page, or a skill needs no code and no C#.

```pwsh
dotnet run --project src/Portfolio.Generator -- --serve
```

Edit a file under `content/`, save, refresh. A failed content rebuild preserves the
last good preview; the terminal names the offending file.

## Changing code

```pwsh
dotnet build Portfolio.slnx
dotnet test Portfolio.slnx
```

Both must pass. Warnings are errors, so a warning fails the build.

### Where things go

| Change | Location |
| --- | --- |
| Colour, type, spacing, motion values | `assets/styles/tokens.css` — never hard-code in a component |
| Element defaults, utilities, prose | `assets/styles/base.css` |
| Page frame, hero, sections, footer | `assets/styles/layout.css` |
| Cards, tags, buttons, lists | `assets/styles/components.css` |
| Reveal, reduced motion, touch overrides | `assets/styles/motion.css` |
| A new section type | `Sections/*.razor` + one entry in `SectionRegistry` |
| A new collection | A type in `Collections.cs`, a `LoadCollection` call, a `PortfolioContent` property |
| Document head, meta tags | `DocumentTemplate.cs` |
| Hero and section editorial copy | `hero` and `sections` in `content/site.yml` |
| Project artwork | The project's `visual` front-matter key |

### Rules

1. **Content never lives in code.** If a change means editing a `.razor` file to alter a
   fact about Alex, it belongs in `content/` instead.
2. **No hard-coded design values.** Use a token. If no token fits, add one.
3. **Content is visible by default.** Any new reveal or motion must degrade to visible when
   JavaScript fails and when `prefers-reduced-motion: reduce` is set.
4. **No colour-only signalling.** Pair it with text, weight, shape, or an ARIA attribute.
5. **Text-labelled links.** No icon-only interactive elements.
6. **Every new dependency needs a justification** recorded in [docs/decisions.md](docs/decisions.md):
   what it solves, why the platform cannot, its maintenance status, and its client-side
   payload. The answer for payload should be zero — nothing ships to the browser today.
7. **Failures must name the file.** Content errors throw `ContentException` with the source
   path. Never swallow one.

### Before opening a pull request

```pwsh
dotnet build Portfolio.slnx -c Release
dotnet test Portfolio.slnx -c Release --no-build
dotnet run --project src/Portfolio.Generator -c Release --no-build -- --output dist
```

If the change is visual, also check it at 320 px and 1440 px, with reduced motion on, and
with JavaScript disabled. The exact scripts used previously are in
[docs/testing.md](docs/testing.md).

The persisted browser harness also checks blocked scripts, forced colors, keyboard
and touch controls, and live reduced-motion changes. Run it for visual or enhancement
changes; its test-only dependency is documented in `docs/decisions.md`.

### Dependency changes

`RestorePackagesWithLockFile` is on. If you add or update a package, commit the updated
`packages.lock.json` files — CI restores with `--locked-mode` and will fail without them.

## Commit messages

Describe the effect, not the mechanics.

```
Add role and period to the Pi cluster project
Fix gradient text disappearing without background-clip support
Group skills into three columns
```

## What not to do

- Do not commit anything from `dist/`. It is generated and git-ignored.
- Do not add analytics, tracking, or a third-party script without an explicit decision
  record.
- Do not suppress a compiler warning. Fix it, or justify the suppression in the code with
  a one-line comment.
- Do not invent biographical facts, employers, dates, metrics, or project details. If
  something is unknown, add it to [docs/content-review-needed.md](docs/content-review-needed.md)
  instead of guessing.
