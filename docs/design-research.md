# Design research

Principles drawn from well-made technology sites, and how each was transformed rather
than copied. No markup, CSS, artwork, icon, or effect was taken from any of these sites.

## Principles adopted

**Restraint with colour** — the strongest technical sites use one accent against a large
neutral field, spending colour only where it carries meaning.
*Applied:* a single hue, `#4db8ff`, on links, section indices, status dots, and the
primary button. Nothing else is coloured. A reader can locate every interactive element
by scanning for one colour.

**Typography as the primary visual device** — hierarchy comes from size, weight, and
tracking rather than from boxes and rules.
*Applied:* a seven-step fluid scale from 13 px to 80 px. The hero name is the largest
element on the site by a wide margin, and the page has no decorative container around it.

**Generous, consistent vertical rhythm** — content is allowed to breathe, and the spacing
between sections is identical everywhere.
*Applied:* one `--section-y` token, `clamp(3.5rem, 2rem + 7vw, 7.5rem)`, used by every
section. Scaled by viewport, never overridden.

**Depth through layering, not decoration** — surfaces separate with a hairline and a soft
shadow rather than borders and gradients.
*Applied:* a 1 px inset highlight plus a large-offset, low-opacity shadow. Two tokens
cover every raised surface on the site.

**Motion that confirms rather than performs** — good interaction feedback is short,
directional, and stops.
*Applied:* four behaviours, `opacity` and `transform` only, nothing looping, everything
removable by `prefers-reduced-motion`.

**Progressive narrative** — a page should be an argument, not a gallery of cards.
*Applied:* hero → work → credentials → tools → contact. Projects come before
certifications because demonstrated work is stronger evidence than a credential.

**Machine-adjacent metadata as texture** — monospace type used for identifiers,
timestamps, and labels gives technical products their character.
*Applied:* section indices (`01 / projects`), tags, location, hostnames, and the colophon
are monospace. Prose never is.

## Transformation into an original identity

The motif is the **control plane**: a dark instrument field, a faint measurement grid, and
one signal colour marking live state.

- The hero grid is two repeating linear gradients at 72 px, radially masked to fade before
  it competes with the headline. It suggests a diagram surface without drawing one.
- Section headers borrow from ordered manifests — a two-digit index, a short gradient rule,
  and the section's own key in lowercase, which is literally the string in the content
  file's `sections:` list.
- Cards grow an accent hairline along the top edge on interest, reading as a link lighting
  up between nodes.
- Status is shown as a small filled dot with a soft halo, always with a text label.

None of this is a cyberpunk aesthetic, a terminal pastiche, or a topology illustration.
The grid is barely visible. The restraint is the point: it should look like a well-built
instrument, not a poster about infrastructure.

## Deliberately not adopted

| Common pattern | Why not |
| --- | --- |
| Full-screen scroll-jacked sections | Breaks the scrollbar, keyboard paging, and find-in-page |
| Sticky pinned storytelling | Expensive on mobile and poor with reduced motion, for little gain across three pages |
| Animated mesh or aurora backgrounds | Continuous GPU work and battery cost for decoration |
| Large hero illustration or photograph | Would be the heaviest asset on a site that currently ships no images |
| Skill proficiency bars | Assign a number to a skill and it is unverifiable; the brief rules them out |
| Content carousels | Hide most of their content and add keyboard and motion problems |
| Custom cursors, magnetic buttons | Fail entirely on touch and harm pointer accuracy |
| Webfont display type | 25–60 KB, more than the rest of the site combined |

## What this is not

It is not an Apple, Linear, or Vercel clone. Those sites sell products to broad audiences
and can afford bespoke illustration, motion budgets, and brand typefaces. This is a
personal portfolio read by hiring managers and engineers, often on a phone, sometimes on a
poor connection. The design goal is that the content is read — so the whole page arrives in
about 10 KB, renders instantly, and works with JavaScript disabled.
