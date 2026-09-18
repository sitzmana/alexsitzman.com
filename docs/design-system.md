# Design system

Concept: **infrastructure, in perspective**. A quiet dark field, large editorial
type, original spatial artwork, and a single blue accent. The page presents the
work rather than imitating a terminal or dashboard.

All design tokens live in `assets/styles/tokens.css`. Editorial copy lives in
`content/site.yml`; project artwork is selected in the project's front matter.

## Voice and wording

Write as Alex speaking to a visitor: friendly, direct, and professional. Use
first person for the bio and personal work, natural contractions, and concrete
descriptions instead of repeated slogans or generic claims about curiosity.
Keep technical terms where they explain the work, not as decoration.

Controls should describe the action: "Search my work", "Clear filters", and
"View details". Error messages say what happened and what the visitor can do
next. Keep snapshot dates, fork labels, and credential names accurate, while
leaving implementation details and proficiency disclaimers in documentation.
Copy edits must not invent experience, outcomes, interests, or current activity.

## Typography and layout

- System sans-serif for headings and prose; system monospace for metadata.
- Fluid hero name: 56-116 px, with tight tracking and a short line measure.
- Section titles: 32-60 px; secondary-page headings: 44-88 px.
- An 80 rem maximum shell with fluid gutters and section spacing.
- Desktop hero: introduction beside a perspective index of actual projects.
  Below 60 em it stacks; narrow screens use flat, generous link targets.
- Projects are large alternating art/text cards, stacked vertically. Mobile
  places the art above the text. Summaries, tags, and links never require a hover.
- Tags and navigation wrap, including long technical terms. Once navigation
  grows to five items, the header stops sticking so it cannot obscure the page.
- On phones, navigation shares the header row with the wordmark and wraps in
  its available column. There is no hidden hamburger menu or extra menu state.
- Home shows a compact featured-credential panel instead of the full seven-card
  collection; the Credentials page contains the complete list.

## Colour and shape

| Token | Value | Purpose |
| --- | --- | --- |
| `--bg` | `#080b12` | Page background |
| `--bg-raised` | `#101620` | Cards and footer |
| `--text` | `#f0f3f8` | Primary text |
| `--text-muted` | `#a8b3c5` | Prose |
| `--text-subtle` | `#8997ad` | Metadata |
| `--accent` | `#4db8ff` | Actions and visual accents |
| `--accent-bright` | `#8ad2ff` | Links and focus |
| `--radius-lg` | 24 px | Main cards and spatial plates |

Cards use a hairline, restrained fill, and inset highlight. Background glows are
static gradients. No blur is animated; the header's backdrop filter is fixed.
The browser theme-color meta tag is derived from `--bg` at build time.

## Spatial content

The standalone initials chip has been removed. The hero's perspective cards are
native links to the actual project cards below, with matching titles, categories,
numbers, and artwork. Both surfaces use the same file-driven featured-project
selection. There is no three-project limit or separately maintained navigation.

Each index link is a stationary hit area with its own perspective. Only its
inner surface transforms; that surface and its visual children ignore pointer
events. Tokenized clearance contains the entire resting and animated card so
corners remain clickable and neighboring cards cannot take over hover. Keyboard
focus belongs to the stationary link, not to the moving visual.

Project cards place original artwork above a raised surface. A stationary outer
frame measures pointer position; the inner card tilts by at most four degrees.
Click, tap, and Enter navigate real content rather than manipulating decoration.
Anchor destinations receive native focus and clear the masthead.

Project `visual` values select abstract `network`, `stack`, or `signal` SVG art.
These are motifs, not factual diagrams of the projects.

The About page restores public repositories, dated contribution data, and a
Spotify-linked record shelf. Original CSS sleeves and vinyl discs are the links,
not borrowed album art or embeds. Native horizontal scrolling, keyboard access,
and optional labelled previous/next buttons expose every record without autoplay
or duplicate slides. Warm neutral record labels distinguish the personal section
without adding a new interactive-state colour.

## Evidence navigation

Explore is a content-backed way to move between skills and actual entries. Four
type controls show matching counts for projects, credentials, repositories, and
work in progress. Selecting one narrows the view; selecting it again restores
all types. Exact topic filtering and keyword search can be combined.

Selected type controls use a border, heavier text, an arrow, and `aria-pressed`,
not colour alone. Cards remain stationary and readable during filtering. The
compact section header avoids repeating a large heading beneath the page title.
Forms stack on phones and use native, comfortably sized inputs and selects.
URLs preserve focused views without introducing a modal, framework, or backend.

## Reading navigation

Long pages have a compact **On this page** panel made from their actual section
titles and top-level Markdown headings. The panel follows short introductory
copy or the Home hero, and precedes long article prose. Links wrap into a
single column on phones and have tokenized touch targets. It is not sticky and
does not compete with the masthead or consume the viewport while reading.

Native links move keyboard focus to their destinations. Enhancement marks the
selected link with a border, heavier text, and `aria-current="location"`; this
reflects the URL fragment, not inferred reading progress. The footer's text link
returns to and focuses the masthead. Both controls work without JavaScript.
Print omits navigation controls while retaining all article content.

The **This site** page describes the actual static pipeline and its tradeoffs.
Its copy is Markdown, not a dedicated template or a live diagnostics dashboard.

## Motion and fallbacks

| Interaction | Behavior |
| --- | --- |
| Scroll reveal | Only offscreen, observed elements are armed; hero stays visible |
| Project index | Perspective card comes forward on hover/focus; activation follows a native anchor |
| Pointer depth | One pending animation frame at most; no idle loop |
| Record shelf | Sleeves and discs separate on interest; explicit navigation only |
| Scroll progress | Native CSS scroll timeline where supported; no JS fallback |

Reduced motion disables animations and transitions, reveals all content, and
removes pointer tilt. A preference change also stops an in-progress shelf scroll.

Disabled or blocked JavaScript leaves the project links and native shelf usable;
unavailable shelf buttons stay hidden. Forced colors removes decorative artwork,
not project navigation, and restores opaque text. Print shows all record titles
in a grid and switches to a light, ink-friendly presentation.

Keyboard focus uses the global accent outline. Links have text labels; active
navigation uses weight and `aria-current`, not colour alone.

No WebGL engine, animation framework, webfont, continuous background animation,
scroll hijacking, custom cursor, or third-party asset request is used.
