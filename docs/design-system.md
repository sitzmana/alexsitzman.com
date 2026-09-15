# Design system

Concept: **control plane**. A dark instrument panel with technical metadata, a precise
grid, and one signal colour reserved for what matters. It should read as something an
infrastructure engineer would build, not as a purchased portfolio template.

Tokens live in [assets/styles/tokens.css](../assets/styles/tokens.css) and are the single
source of truth. No component hard-codes a colour, size, or duration.

## Colour

| Token | Value | Use |
| --- | --- | --- |
| `--bg` | `#080b12` | Page field |
| `--bg-raised` | `#0d121c` | Cards, footer |
| `--surface` | `rgb(255 255 255 / 2.5%)` | Translucent card fill |
| `--border` | `rgb(148 163 184 / 14%)` | Hairlines, dividers |
| `--border-strong` | `rgb(148 163 184 / 30%)` | Hover and focus states |
| `--text` | `#e8eef8` | Headings, body |
| `--text-muted` | `#9aa7bd` | Secondary prose |
| `--text-subtle` | `#7787a0` | Metadata, labels |
| `--accent` | `#4db8ff` | Markers, glyphs, rules |
| `--accent-bright` | `#8ad2ff` | Link text |
| `--accent-soft` | `rgb(77 184 255 / 12%)` | Active nav, badge fill |
| `--on-accent` | `#06101c` | Text on the accent button |
| `--grid-line` | `rgb(148 163 184 / 7%)` | Hero grid |

One accent hue. It marks links, the current section index, status dots, and the primary
button — nothing else. Contrast ratios are in [accessibility.md](accessibility.md); the
lowest is 5.4:1.

**Signalling rule.** Nothing is communicated by colour alone. Links carry an underline,
the active nav item carries `aria-current` and heavier weight, and status carries text.

## Typography

System stack, no webfont (see [decisions.md](decisions.md) D6).

```
--font-sans: "Segoe UI Variable Display", "Segoe UI", system-ui, -apple-system, ...
--font-mono: "Cascadia Code", "SF Mono", "JetBrains Mono", ui-monospace, Consolas, ...
```

Monospace is not decoration. It marks machine-adjacent information — section indices,
tags, the location line, hostnames, the colophon — and separates it from prose at a glance.

Fluid scale; every step is a `clamp()` so nothing needs a breakpoint:

| Token | Range | Use |
| --- | --- | --- |
| `--step--1` | 13 → 14 px | Metadata, tags |
| `--step-0` | 16 → 17 px | Body |
| `--step-1` | 18 → 21 px | Lead paragraphs, card titles |
| `--step-2` | 22 → 28 px | Project titles |
| `--step-3` | 26 → 40 px | Section titles |
| `--step-4` | 32 → 56 px | Page titles |
| `--step-5` | 40 → 80 px | Hero name |

`--tracking-tight: -0.022em` on headings; `--tracking-wide: 0.14em` on uppercase mono
labels. `text-wrap: balance` on headings, `pretty` on paragraphs.
`--measure: 68ch` caps line length.

## Space

A 4 px base: `--space-1` (4 px) through `--space-10` (80 px).

`--section-y: clamp(3.5rem, 2rem + 7vw, 7.5rem)` gives every section the same vertical
rhythm from phone to wide desktop.
`--shell-max: 72rem`, `--shell-pad: clamp(1.25rem, 0.8rem + 2.2vw, 3rem)`.

## Shape and depth

Radii: `6px` / `10px` / `16px` / pill. Depth comes from a 1 px inset highlight plus a
soft, large-offset shadow — not from heavy blur.

```
--shadow-raised: 0 1px 0 0 rgb(255 255 255 / 4%) inset, 0 12px 32px -18px rgb(0 0 0 / 90%);
--shadow-lift:   0 1px 0 0 rgb(255 255 255 / 7%) inset, 0 24px 48px -24px rgb(0 0 0 / 95%);
```

Translucency is used twice — the sticky masthead and the card fill — and never over text
that has to stay readable. Glassmorphism is not a theme here.

## Motion

| Token | Value |
| --- | --- |
| `--dur-fast` | 120 ms |
| `--dur-base` | 220 ms |
| `--dur-slow` | 620 ms |
| `--ease-out` | `cubic-bezier(0.22, 1, 0.36, 1)` |
| `--ease-in-out` | `cubic-bezier(0.65, 0, 0.35, 1)` |

Four behaviours, no more:

1. **Reveal on scroll** — 14 px rise and fade, 70 ms stagger across a card group.
2. **Masthead settle** — a border and background shift once the page scrolls.
3. **Card interest** — 3 px lift, a brighter border, and an accent hairline along the top
   edge on hover or focus-within.
4. **Scroll progress** — a 2 px bar driven by CSS `animation-timeline: scroll()`.

Only `opacity` and `transform` animate. Nothing loops. Nothing moves unless the user acts.

Fallbacks are described in [decisions.md](decisions.md) D10 and D11 and measured in
[accessibility.md](accessibility.md).

## Layering

`--z-base: 0`, `--z-raised: 10`, `--z-header: 50`, `--z-progress: 60`, `--z-skip: 100`.
Five values, named by role. No arbitrary `z-index: 9999`.

## Focus

```css
:focus-visible {
  outline: 2px solid var(--accent-bright);
  outline-offset: 3px;
  border-radius: var(--radius-sm);
}
```

One rule, global, never removed. `:focus-visible` rather than `:focus` so mouse users do
not get a ring on click while keyboard users always do.

## Breakpoints

Mobile-first. Only four, all in `em` so they respond to the user's font size:

| Width | Change |
| --- | --- |
| `40em` | Cards become a multi-column auto-fit grid |
| `46em` | Skill groups become columns |
| `52em` | Footer becomes two columns |
| `(hover: none)` | Hover lift removed |

Most of the layout has no breakpoint at all — `clamp()`, `auto-fit`, and `minmax()` handle
it continuously.

## Structural motifs

**Numbered section frames.** Each section opens with `01` in accent, a short gradient
rule, and the section key in lowercase mono — a nod to configuration files and ordered
manifests.

**Hero grid field.** Two repeating linear gradients at 72 px, radially masked so the grid
fades out before it can compete with the text. Pure CSS, no image, no animation.

**Accent hairline.** Cards grow a gradient hairline along the top edge on interest. It
reads as a connection lighting up.

**Status dots.** A small filled circle with a soft accent halo, used for location and
"in progress" — always paired with text.

## Rejected

Explicitly avoided, per the brief and on the merits:

skill percentage bars (unverifiable), neon cyberpunk palettes, animated backgrounds,
matrix rain, decorative terminal windows, stock photography, heavy glassmorphism,
multi-hue gradients, carousels for content that fits on screen, and any effect that
competes with the text it surrounds.
