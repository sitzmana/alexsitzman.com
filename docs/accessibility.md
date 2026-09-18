# Accessibility

The target remains WCAG 2.2 AA. The checks below are useful regressions, **not a
claim of complete conformance**.

## Verified in the September 18, 2026 browser pass

The persisted harness is `tests/browser/check_site.py`; commands are in
`docs/testing.md`.

- All six routes fit 320, 375, 768, 1024, and 1440 px without horizontal overflow.
- Each real page has one h1, unique element IDs, and text-labelled controls.
- The first tab stop is Skip to content; activating it moves focus to `main`.
- JavaScript-disabled, script-blocked, and missing-IntersectionObserver contexts
  retain visible content at 320 and 1440 px.
- Reduced motion reveals all content, disables the progress bar, stops tilt,
  and removes transitions. Changes to this preference work without reloading.
- Project index links work with keyboard, pointer, touch, and disabled scripting.
  They focus the corresponding project, visible below the sticky header.
- Hero link hit areas remain stationary during animation. Corners and edges retain
  their target, while non-interactive visual layers cannot intercept pointer input.
- The record shelf supports native keyboard scrolling and touch swiping, with
  optional text-labelled previous/next buttons and correctly disabled endpoints.
- Forced colors leaves text opaque and hides decorative artwork.
- Print reveals content and uses a light, ink-friendly palette.
- Long tags and additional navigation entries remain within a 320 px viewport.
- Explorer uses labelled native inputs and buttons, `aria-pressed` selection,
  result announcements, explicit empty/error states, and stable search focus.
- Search/filter controls remain disabled when scripts are unavailable, with an
  explanation and the entire linked catalog still visible.
- Clipboard denial exposes a labelled, selected link for manual copying.
- Long-page contents and return-to-top links have 44 px targets, move native
  keyboard focus, and preserve Back/Forward behavior at 320 and 1440 px, including
  disabled/blocked scripts and forced colors.
- Direct heading fragments remain usable after reload. Selected sections reveal
  their content immediately; print hides the controls, not the article headings.

The 3D effects are never a prerequisite for understanding the portfolio. The
project index remains meaningful navigation in flat form. Only the original
project artwork and record-sleeve art are `aria-hidden`; full titles, artists,
categories, and links remain available.

## Structural safeguards

Navigation uses native links, labelled navigation landmarks, `aria-current`, and
weight as well as colour. Shelf controls are native buttons, not clickable divs,
and stay hidden until enhancement makes them usable. Missing labels fail the build.

The global focus outline is not removed. Main landmarks are programmatically
focusable for skip navigation. No positive tabindex, focus trap, modal, custom
cursor, or scroll hijacking is introduced.

Project titles link to their detail page when present, otherwise to the existing
first outbound link. Project cards have no full-card link overlay that could
obscure their independent links. New-tab links retain their accessible announcement
and safe rel values.

Contribution activity includes a total, an explicit period, and a visible list of
every nonzero date/count. The visual calendar is an optional scroll region, with
active days distinguished by a plus sign as well as colour. It is not a live feed
or an assertion that contribution count measures all work.

Tags wrap rather than overflowing. Numbered Markdown lists retain their numbering;
code blocks and tables scroll within the prose region instead of widening the page.
Explorer inputs use the normal body font size; controls stack on narrow screens.
Printing retains the complete catalog rather than hiding nonmatching entries.

Section headings and introductions are editable in YAML. Missing section copy,
duplicate sections, malformed links, and duplicate routes fail before publication.
Contents labels derive from those section titles and plain-text Markdown headings.
Heading IDs are scoped by collection and slug with an unambiguous separator, so
shared bodies and layout elements cannot create duplicate targets. Empty headings
and incomplete navigation labels fail with their source file.

## Colour and motion

Text uses opaque high-contrast tokens against the dark background and card fill.
Gradient hero text has an opaque fallback and a forced-colors override. Status
and selection use text, weight, shape, or ARIA alongside colour.

Only successfully observed offscreen reveal targets get `is-pending`; neither
the `js` class nor a failed external script can hide the page. Focus-within also
reveals a pending target. Print and back/forward restoration clear pending reveals.

## Not yet verified

- No NVDA, JAWS, or VoiceOver session.
- No full automated accessibility ruleset or exhaustive contrast audit.
- No physical assistive-technology or high-contrast device testing.
- No Firefox or WebKit run.
- No automatic alt-text enforcement for author-supplied Markdown images.
- No accessibility testing with users.
