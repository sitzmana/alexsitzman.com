# Decisions

Significant decisions, what was rejected, and why. Newest last.

---

## D1 — Static HTML generation instead of Blazor WebAssembly

**Decision:** render Razor components to static HTML at build time with
`Microsoft.AspNetCore.Components.Web.HtmlRenderer`. Ship no .NET runtime to the browser.

**Rejected: standalone Blazor WebAssembly.** It was the stated preference, and it is the
wrong tool here.

| | Blazor WASM | Static generation |
| --- | --- | --- |
| Initial download | Multiple MB (runtime + assemblies) | ~43 KB for the home page |
| First paint | After runtime boot | Immediately |
| Works without JavaScript | No — the page is empty | Yes, fully |
| Search indexing | Requires JS execution by the crawler | Plain HTML |
| Hosting | Static Web Apps | Static Web Apps |

The authoring model is identical either way: C#, Razor components, strong typing,
`dotnet build`. The only thing given up is client-side interactivity, and this site has
none — it is a document. Nothing on it needs a runtime.

**Rejected: Blazor SSR / server-side rendering.** Requires a running server. The brief
explicitly excludes a permanently running backend, and it would add hosting cost for a
site whose content changes a few times a year.

**Rejected: a non-.NET static site generator.** The brief requires .NET.

---

## D2 — Content lives in files, not in C#

**Decision:** every piece of content is a Markdown file with YAML front matter under
`content/`. Collections are folders. Adding a file adds content.

**Why:** the previous site required a code change and a redeploy to add a project. The
explicit requirement is that dropping a file into the repository updates the site.

**Rejected: strongly typed C# content classes.** Type-safe, but editing content means
editing code, recompiling, and reasoning about C# collection-initialiser syntax to add a
project. That is the problem being solved, not a solution to it.

**Rejected: a single large JSON file.** Merge conflicts on every edit, no per-item
history in `git log`, no place for prose, and no way to co-locate an image with the entry
that references it.

**Consequence:** front matter is not compile-time checked, so the loader validates it at
build time instead. Unknown keys are an error, not a shrug — see D3.

---

## D3 — A content mistake fails the build

**Decision:** the YAML deserialiser runs **without** `IgnoreUnmatchedProperties`. A
misspelled key (`featuerd: true`) fails the build, naming the file and the line.

**Why:** the failure mode of the alternative is silent. `featuerd: true` would parse
cleanly, `Featured` would stay `false`, and the project would quietly not appear on the
home page. The author would have no signal at all. A build failure that names the file is
strictly better than a site that is quietly wrong.

The same principle covers missing titles, unterminated front matter, duplicate slugs, and
unknown `sections:` keys — the last of which lists the valid keys in the error message.

`RealContentTests` loads the actual content tree, so these failures surface in CI too.

---

## D4 — `order:` and the filename prefix share one number line

**Decision:** `020-hiking.md` means order 20. Front-matter `order: 20` means the same
thing and wins if both are present.

**Why:** the first implementation treated them as independent sort keys, which produced
an ordering that could not be predicted from looking at the folder. A test
(`Explicit_order_wins_over_the_file_prefix`) caught it. Unifying them means there is one
concept to learn and the folder listing shows the page order.

---

## D5 — Two build-time dependencies, zero runtime dependencies

**Decision:** use Markdig (Markdown) and YamlDotNet (YAML). Add nothing else.

Both run only in the generator. Neither reaches the browser. The deployed site has no
JavaScript dependencies at all — the one script is 2.9 KB of hand-written code.

**Rejected: writing a Markdown parser.** Not a reasonable use of effort, and correctness
matters (link handling, HTML escaping).

**Rejected: hand-rolling a YAML parser.** A naive `key: value` splitter breaks on nested
lists, multi-line strings, and quoting. It would fail quietly, which D3 rules out.

**Rejected: an animation library (GSAP, Framer Motion, AOS).** All the motion on this
site is a CSS transition toggled by a class. The scroll progress bar uses
`animation-timeline: scroll()` where supported and degrades to nothing where not. A
library would add more bytes than the entire rest of the site.

Markdig is configured with `.DisableHtml()`. Content files are prose, not a template
escape hatch, and that removes an HTML-injection path through content.

---

## D6 — System font stack, no webfont

**Decision:** `Segoe UI Variable Display` → `Segoe UI` → `system-ui` for text,
`Cascadia Code` → `SF Mono` → `ui-monospace` for technical metadata.

**Why:** a self-hosted variable font subset costs 25–60 KB and a render-blocking request,
or a FOUT. The site's entire payload is currently ~43 KB. A font would be the single
largest asset and would roughly double it.

**Cost, stated plainly:** typography differs between platforms. Windows gets Segoe UI
Variable; macOS gets SF; Linux gets whatever `system-ui` resolves to. The layout is built
on a fluid `clamp()` scale rather than fixed sizes, so it tolerates the variance.

Revisit if the site ever gains a distinct brand typeface.

---

## D7 — Dark theme only

**Decision:** ship one theme, executed properly.

The brief permits this explicitly: *"If not, produce one exceptional theme rather than two
mediocre themes."* The visual concept is an instrument panel — a dark field with one
signal colour marking what matters. A light inversion is a different design, not a
recolour, and would need its own contrast pass, its own grid treatment, and its own
gradient behaviour.

`color-scheme: dark` is declared so form controls and scrollbars match.

---

## D8 — Always-visible navigation, no hamburger menu

**Decision:** the primary nav renders as a visible row at every width, including 320 px.

**Why:** there are three links, each one word. At 320 px they occupy roughly half the
available width. A disclosure menu would add a tap, a focus-management problem, an
`aria-expanded` state, and either a JavaScript dependency or a `<details>` element whose
desktop styling is unreliable across engines.

**Trigger to revisit:** more than about five nav items, or any item longer than two words.

---

## D9 — Links are labelled with text, not brand logos

**Decision:** GitHub, LinkedIn, and Dev.to links show their name as text with a neutral
"opens externally" arrow. No brand marks.

**Why:** two reasons, both sufficient. Third-party logos are trademarked artwork and the
brief forbids copying third-party branding. And an icon-only link needs an accessible
name anyway — so the text was always required; only the decoration was optional.

The six glyphs used (`arrow-out`, `arrow-right`, `arrow-down`, `mail`, `node`, `check`)
are original geometry: lines, circles, and rectangles.

---

## D10 — Motion is additive and reversible

**Decision:** content is visible by default. The document ships with `class="no-js"` on
`<html>`, and an inline script in `<head>` swaps it to `js` before first paint. Only
`.js .reveal` is hidden.

**Why:** the common implementation hides content in CSS and reveals it with JavaScript.
If the script fails — blocked, CSP violation, network error, an old browser — the page is
permanently blank. Here, a failed script means everything is simply already visible.

The class swap is inline and synchronous specifically so there is no flash of visible
content before hiding.

Reduced motion is honoured in three places: the reveal transition is removed, the scroll
progress bar is hidden, and the `IntersectionObserver` is never constructed — it reveals
everything immediately instead.

Verified: with JavaScript disabled, `<html>` keeps `no-js`, 0 of 18 reveal elements are
hidden, and 2,036 characters of body text render.

---

## D11 — Card hover lift is disabled on touch devices

**Decision:** `@media (hover: none)` removes the transform and shadow change.

**Why:** a hover style on a touch device either never fires or sticks after a tap until
something else is touched. Neither is useful. Focus styling is untouched, so keyboard
users keep the affordance.

---

## D12 — Gradient text is guarded by `@supports`

**Decision:** `.hero__name` and `.wordmark__mark` set `color: var(--text)` by default and
only become transparent inside
`@supports ((-webkit-background-clip: text) or (background-clip: text))`.

**Why:** found during the accessibility pass. The unguarded version sets
`color: transparent` and paints the visible colour via a clipped background. In an engine
that does not support `background-clip: text`, that renders transparent text on a
transparent background — the site's own name, invisible. The name is the most important
text on the page.

---

## D13 — Email link, not a contact form

**Decision:** `mailto:` link, shown as both a button and the literal address.

A form needs an endpoint. An endpoint means a function app, a third-party form service,
or a serverless handler — cost, a privacy surface, a spam surface, and something that can
break silently. The address is already public on the previous site.

Showing the address as text as well as a button means it works when `mailto:` is not
wired up, which is common on shared and managed machines.

---

## D14 — Output cleaning clears read-only attributes

**Decision:** `SiteBuilder.PrepareOutput` walks the output tree, clears the `ReadOnly`
attribute on every entry, and deletes bottom-up, with a bounded retry.

**Why:** found in practice. This repository lives inside a synced OneDrive folder, which
marks synced directories `ReadOnly, Directory, Archive, ReparsePoint`.
`Directory.Delete(path, recursive: true)` fails on those with "access is denied", which
broke every rebuild after the first. The retry additionally absorbs the brief file handles
that sync clients and editors hold.

---

## D15 — The preview server uses terminal middleware, not `MapFallback`

**Decision:** the 404 handler is `app.Run(...)` rather than `app.MapFallback(...)`.

**Why:** found in practice. `MapFallback` registers an endpoint, which causes routing to
run first and select it. `UseDefaultFiles` and `UseStaticFiles` both short-circuit when an
endpoint is already selected, so `/` and `/about/` returned 404 while `/index.html`
worked — the fallback route's `{*path:nonfile}` constraint skipped paths with extensions,
which is what made the failure look inconsistent.

Preview-only code, but it would have cost real debugging time later.

---

## D16 — Assets are content-hashed and concatenated

**Decision:** the five stylesheet parts are concatenated into one `site.<hash>.css`;
`enhance.js` becomes `enhance.<hash>.js`. `staticwebapp.config.json` serves `/assets/*`
with `max-age=31536000, immutable` and HTML with `must-revalidate`.

Authoring stays split across tokens / base / layout / components / motion; delivery is one
request. The hash changes when the content changes, so an immutable cache is safe and a
deploy can never serve stale CSS against new HTML.

## D17 - Spatial design without a rendering engine

The standalone hero described here is superseded by D25; the dependency and
performance constraints remain.

The hero uses original CSS plates in perspective, with small inline SVG circuits.
Project artwork is selected by the `visual` key in each project's Markdown file.
No WebGL engine, animation library, model download, webfont, or third-party request
is added. Pointer movement schedules at most one pending animation frame; there is
no idle render loop. Touch, reduced motion, and disabled JavaScript keep a static
composition. Forced colors hides decorative artwork.

This supersedes the earlier no-illustration direction while preserving D1 and D5:
the site is still static .NET output with zero client-side dependencies.

## D18 - Enhancements must actually be fail-open

The original `.js .reveal` rule hid all reveal elements even if the external script
never arrived. Only offscreen elements successfully registered with the reveal
observer now receive `is-pending`. Hero content is never hidden. Initialization
failure, keyboard focus, reduced-motion changes, printing, and back/forward cache
restoration all keep or restore visibility.

## D19 - Build away from the published output

Render and validate a complete temporary site before updating the destination.
Content errors and output collisions preserve the last good build. Unchanged
published files retain their timestamps, reducing filesystem and sync churn.
This replaces D14's unconditional output deletion; its bounded deletion retry is
retained for temporary cleanup and removal of obsolete generated files.

Publication is file-by-file, not a transactional deployment: an I/O failure during
the copy phase can still require a rebuild. Assets are copied before HTML.
The preview watcher coalesces bursts and queues changes arriving during a build
instead of dropping them. Its worker and watchers are disposed on shutdown.

## D20 - Browser-only test dependency

Add pinned Python Playwright under `tests/browser/requirements.txt` for repeatable
browser checks. It is test tooling only, not a build-time content dependency and
never part of `dist/`. Production remains zero-dependency HTML/CSS/JavaScript.

Both CI and deployment validation exercise Chromium at phone and desktop widths,
script-blocking, disabled scripting, absent IntersectionObserver, forced colors,
reduced motion, keyboard skip navigation, and pointer-idle behavior. No screenshots
or telemetry are sent to third-party services. Local Edge is supported for
development without an additional browser download.

## D21 - Editorial copy is file-driven

Hero copy, calls to action, section titles, and section introductions now live in
`content/site.yml`, not Razor strings. A section referenced by a page must have
copy under `sections` (except the separately configured hero). Unknown sections,
missing titles, unsupported visuals, invalid links, and duplicate routes fail with
the source filename. Project artwork is decorative, not a factual architecture
diagram or claim about the project's implementation.

## D22 - One validation pipeline per change

`deploy.yml` calls the reusable `ci.yml` build job and consumes that run's artifact.
CI no longer independently repeats the same build for each push or pull request.
The shared gate includes credential scanning, .NET tests, exact byte budgeting,
and browser regressions, so deployment cannot bypass the checks. CI retains a
manual `workflow_dispatch` entry point. No deployment secret is passed into the
read-only validation workflow.

The September 18 portal setup added a second publisher that had no generator
step and could not find the untracked `dist/` directory. Remove that generated
workflow rather than maintaining competing deployment paths. Retain the
portal-created deployment secret name in both upload and preview cleanup.
Workflow regressions parse the YAML with the existing YamlDotNet dependency;
they cover the single publisher, artifact handoff, and matching secret references.

## D23 - Spatial controls work beyond a mouse

Superseded by D25: the decorative layer toggle has been removed.

The hero's layer control is a native, text-labelled toggle button with
`aria-pressed`. It is available on keyboard and touch once the enhancement has
initialized; without JavaScript it is hidden and the static illustration remains.
Reduced motion changes layers immediately, without a transition. Only decorative
artwork changes; all portfolio content remains readable throughout.

## D24 - Static routes need real 404s

Remove the SPA `navigationFallback` that served the 404 document with a success
status for unknown paths. Keep the explicit 404 response override instead.
Set revalidation globally so directory routes, not just `*.html` requests, receive
the HTML cache policy; hashed assets override it with immutable caching.
This follows Azure Static Web Apps' documented fallback and global-header rules.
The browser harness also checks the interaction script under the configured CSP.

## D25 - Depth belongs to useful content

Replace the standalone initials chip and its decorative layer toggle with native
links to actual projects. The hero index and main cards share `FeaturedProjects`;
their titles, categories, order, artwork, and anchor IDs derive from the same files.
Index links follow the projects-listing page rather than assuming it must be Home.

Depth now operates on the project cards themselves. Stationary outer frames measure
pointer position; inner surfaces tilt without geometry feedback. All summaries,
tags, and links remain visible. Keyboard and touch activate real destinations, not
a simulation of hover. No custom cursor, scroll interception, canvas, model, or
new dependency is introduced.

## D26 - Restore original features as honest snapshots

A rendered revisit of the original About page on September 17, 2026 showed working
music and GitHub sections, correcting the initial audit's loading/empty result.
Restore its twelve actual Spotify-linked tracks, five repositories, and public
contribution dates as Markdown/YAML collections. Public GitHub data establishes
which repositories are forks; the UI labels them explicitly.

These are dated snapshots, not a live feed, currently-playing status, or a claim
that contribution count measures all work. No credentials, background fetching,
third-party embeds, album covers, or brand logos are needed. Music uses original
CSS record art and native links, with scrollable content available even when
scripts are blocked. Previous/next buttons enhance the shelf without autoplay or
duplicate slides. Any future automatic refresh needs a separate explicit decision.

## D27 - Connect skills to content, not proficiency claims

Add a file-driven Explorer page instead of a decorative network with invented
relationships or skill percentages. Its catalog comes from actual project,
credential, repository, and now entries. Only declared topics and repository
languages create connections. Existing skill names become links only when such
a connection exists. Forks, credentials, and work in progress retain their labels.

The full catalog ships as static HTML. Native controls are disabled until
enhancement initializes, and the no-script explanation stays honest. Local
filtering uses a one-time text index, changes existing nodes, and makes no network
request. Search URLs are shareable and support Back/Forward. Invalid filter
values, unavailable clipboard access, and blocked history updates are surfaced.
There is no new dependency, modal focus trap, server endpoint, or analytics.

Move the complete credential collection to its own content page and let
`featured: true` select the smaller home highlight. This reduces repeated home-page
content on phones without losing any credential. Page files still determine
routes, navigation, and section order; sitemap-driven browser checks cover additions.

## D28 - Reading navigation is generated content, not a second outline

Derive a compact contents panel from top-level Markdown h2 headings and each
page's declared section titles. Show it only when there are at least three
destinations; leave short pages uncluttered. Project detail pages use the same
component. The implementation tour is a normal page file, not a special template.

Use Markdig's existing automatic identifiers and scope them by collection and
slug, with a double-hyphen boundary that normalized slugs cannot contain. Shared
collection bodies cannot duplicate each other's heading IDs, even when a slug
and a heading share words.
Rewrite matching local Markdown fragments to those IDs, and make anchor targets
focusable without adding them to the tab order. Reject empty headings at their
source. Labels come from `site.yml` and the content itself.

Navigation and the footer's return link are ordinary anchors. They work without
scripts, preserve browser history, and follow the existing reduced-motion rules.
The small enhancement marks the URL's current destination; there is no
scroll-spy observer, sticky reading rail, or new dependency. Print omits the
controls, not the content.
