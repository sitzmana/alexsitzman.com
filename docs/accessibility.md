# Accessibility

Target: **WCAG 2.2 AA**.

Automated checks catch a minority of accessibility problems. This document separates
what was *measured* from what was *reviewed by reading the markup*, and states what has
not been verified at all.

---

## Measured

Run against the generated site in Chromium via Playwright. Reproduce with the commands in
[testing.md](testing.md).

### Colour contrast

Every text node on the home page was measured against its computed background, with the
4.5:1 threshold for normal text and 3:1 for large text.

| Result | Detail |
| --- | --- |
| Violations | **0** |
| Elements checked | All text-bearing `p`, `span`, `a`, `li`, `h1`–`h4`, `dt`, `dd` |

Two initial failures were reported and both were fixed:

- `.hero__name` and `.wordmark__mark` computed to `color: transparent`. This was a real
  defect, not a measurement artefact — see D12 in [decisions.md](decisions.md). Both now
  fall back to an opaque colour outside `@supports`.

Token contrast ratios against `--bg` (`#080b12`):

| Token | Hex | Ratio | Use |
| --- | --- | --- | --- |
| `--text` | `#e8eef8` | ~16.8:1 | Body and headings |
| `--text-muted` | `#9aa7bd` | ~8.1:1 | Secondary prose |
| `--text-subtle` | `#7787a0` | ~5.4:1 | Metadata, labels |
| `--accent` | `#4db8ff` | ~9.0:1 | Links, markers |
| `--accent-bright` | `#8ad2ff` | ~11.9:1 | Link text on dark |

The lowest is 5.4:1, above the 4.5:1 requirement.

### Target size (2.5.8, AA — 24×24 CSS px)

| Result | Detail |
| --- | --- |
| Violations | **0** at 390 px viewport |

One failure was found and fixed: the plain-text email address under the contact button
was 161×15 px. It now has `min-height: 2.75rem`.

Interactive elements use `min-height: 2.75rem` (44 px) where they are not already larger —
above the AA minimum and at the AAA 44×44 threshold.

### Reflow (1.4.10) and horizontal scrolling

Measured as `scrollWidth - clientWidth` on every route.

| Viewport | `/` | `/about/` | `/now/` |
| --- | --- | --- | --- |
| 320 px | 0 | 0 | 0 |
| 375 px | 0 | 0 | 0 |
| 390 px | 0 | 0 | 0 |
| 768 px | 0 | 0 | 0 |
| 1024 px | 0 | 0 | 0 |
| 1440 px | 0 | 0 | 0 |
| 1920 px | 0 | 0 | 0 |

No horizontal overflow at any tested width. `overflow-wrap: break-word` on `body` prevents
long technical terms from forcing a scrollbar.

### Motion (2.3.3)

With `prefers-reduced-motion: reduce`:

| Check | Result |
| --- | --- |
| Reveal elements hidden | **0 of 18** |
| Scroll progress bar | `display: none` |
| `IntersectionObserver` constructed | No — content is revealed immediately |

### Scripting unavailable

Not a WCAG criterion, but it determines whether the page exists for anything that does not
run JavaScript.

| Check | Result |
| --- | --- |
| `<html>` class | `no-js` (never swapped) |
| Reveal elements hidden | **0 of 18** |
| Body text rendered | 2,036 characters |
| Section headings present | 4 |

The page is complete without JavaScript.

### Keyboard focus

First eight tab stops on the home page:

| Order | Element | Visible focus indicator |
| --- | --- | --- |
| 1 | Skip to content | Yes |
| 2 | Wordmark → home | Yes |
| 3 | Home | Yes |
| 4 | About | Yes |
| 5 | Now | Yes |
| 6 | GitHub | Yes |
| 7 | LinkedIn | Yes |
| 8 | Dev.to | Yes |

Focus order follows DOM order, which follows visual order. There is no `tabindex` above 0
anywhere in the codebase, and no focus trap — there is no modal, drawer, or overlay.

`:focus-visible` gives a 2 px `--accent-bright` outline at 3 px offset, which contrasts
~11.9:1 against the background.

---

## Reviewed in markup

Verified by reading the rendered HTML and the accessibility tree, and asserted in tests
where practical.

### Structure

- One `<h1>` per page. Asserted for all routes.
- Heading levels descend without skipping: page `h1` → section `h2` → card `h3` → nested
  `h4` in the grouped "now" list.
- Landmarks: one `banner`, one `main`, one `contentinfo`, `navigation` labelled "Primary"
  and "Footer".
- `<html lang="en">` on every page. Asserted in tests.
- Skip link is the first focusable element, targets `#main`, and is visible on focus.
  Asserted in tests.

### Links and controls

- Every link has a text accessible name. There are no icon-only links.
- Links opening a new tab append a visually hidden "(opens in a new tab)".
- External links carry `rel="noopener noreferrer"`; profile links add `rel="me"`.
- The current page is marked three ways — `aria-current="page"`, a background tint, and
  heavier weight — so it is never signalled by colour alone (1.4.1).
- Project cards use a full-surface pseudo-element on the title link, so the whole card is
  clickable while exactly one link exists in the accessibility tree. Nested links inside a
  card (`.entry-link`) are raised above it so they remain independently clickable.

### Images and decoration

- Decorative SVG glyphs: `aria-hidden="true"` and `focusable="false"`.
- Decorative layout elements (hero grid, glow, bullet markers, progress bar):
  `aria-hidden="true"`, or empty `<span>` with no text content.
- No content images ship with the site today. Images added beside a content file are
  authored in Markdown, where alt text is `![alt](file)` — **alt text is the author's
  responsibility and is not currently enforced by a test.**

### Content semantics

- Tag lists are `<ul>`/`<li>` with an `aria-label` naming their entry, so a screen reader
  announces "Technologies used in Kubernetes Raspberry Pi Cluster, list, 3 items".
- The project factsheet is a `<dl>` with grouped `<div>` wrappers.
- No ARIA is used where native HTML suffices. The only ARIA present is `aria-label`,
  `aria-current`, and `aria-hidden`.

### Zoom (1.4.4)

Type is set in `rem` on a fluid `clamp()` scale with no `maximum-scale` or
`user-scalable=no` in the viewport meta tag. Layout uses `auto-fit` grid tracks with
`minmax()`, so columns collapse rather than clip as the effective viewport narrows under
zoom.

---

## Not verified

Stated rather than implied.

- **No screen reader has been used.** Structure was confirmed via the accessibility tree,
  which is not the same as listening to NVDA, JAWS, or VoiceOver read the page.
- **No axe-core or Lighthouse accessibility scan has been run.** The contrast and target
  checks above are custom measurements, not a full automated ruleset.
- **Only Chromium was tested.** No Firefox or WebKit verification.
- **No testing with users of assistive technology.**
- **Windows High Contrast Mode / forced-colors has not been tested.** The gradient text
  and translucent surfaces are the likely risk areas.
- **Alt text on author-supplied images is unenforced.** A test asserting that every
  Markdown image has non-empty alt text would close this.

## Reproducing the measurements

```pwsh
dotnet run --project src/Portfolio.Generator -- --serve --port 5173
```

Then drive `http://localhost:5173` with Playwright. The exact scripts used are recorded in
[testing.md](testing.md).
