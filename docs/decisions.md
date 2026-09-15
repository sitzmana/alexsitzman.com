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
