# Architecture

## Shape

```
  content/*.md ──┐
  content/*.yml ─┤
                 ▼
        Portfolio.Content          parse, validate, sort, render Markdown
                 │  PortfolioContent (typed)
                 ▼
        Portfolio.Components       Razor components, presentation only
                 │  RenderFragment
                 ▼
        Portfolio.Generator        HtmlRenderer → HTML strings → files
                 │
                 ▼
             dist/                 static HTML, one CSS file, one JS file
                 │
                 ▼
     Azure Static Web Apps         no compute, no database
```

Nothing in this pipeline runs in production. The output is files.

## Projects

### `Portfolio.Content`

The content model and loader. Knows nothing about HTML, CSS, or Razor.

| Type | Role |
| --- | --- |
| `ContentEntry` | Base: slug, title, summary, tags, links, order, draft, body |
| `ContentHeading` | Derived plain-text h2 title and collision-free body anchor |
| `Project`, `Certification`, `NowItem`, `SkillGroup`, `Interest`, `Fact` | Collection types |
| `RepositoryEntry`, `Track`, `ActivitySnapshot` | File-backed public-work and music snapshots |
| `ContributionDay` | A verified date/count within an activity period |
| `EvidenceItem`, `EvidenceKind` | Derived catalog entries with real source destinations and declared topics |
| `ExplorerCopy` | Required file-backed control labels and state messages |
| `Page` | A routable page; declares which `sections` it renders |
| `SiteConfig` | `content/site.yml` |
| `PortfolioContent` | Everything, plus derived views (`NavPages`, `FeaturedProjects`, `Evidence`) and section-route lookup |
| `ContentLoader` | Walks the tree, binds YAML, renders Markdown, sorts, validates |
| `FrontMatter` | Splits `---` fenced YAML from the body |
| `Slug` | File name → URL segment; reads the numeric sort prefix |
| `ContentException` | Carries the source path so build failures name the file |

Dependencies: Markdig, YamlDotNet. Both build-time only.

**Loading rules**

- An entry is `<collection>/<slug>.md` or `<collection>/<slug>/index.md`.
- The folder form collects sibling non-Markdown files as `ContentAsset`s, copied to
  `/<collection>/<slug>/`, so an image sits next to the text referencing it.
- Sort key: front-matter `order`, else the `020-` filename prefix, else last; ties break
  on title.
- `draft: true` is excluded unless `--drafts`.
- A missing collection folder yields zero entries, not an error.

**Failure rules.** Every one of these fails the build naming the file:
unknown front-matter key, missing `title`, unterminated front matter, duplicate slug,
a slug that reduces to empty, a missing `site.yml`, a content tree with no home page.

### `Portfolio.Components`

Razor components. Reads `PortfolioContent` through a cascading value; performs no I/O.

```
PageView            page heading, body, then its declared sections
ProjectPage         project detail page
SiteHeader          wordmark + primary nav
SiteFooter          nav, profiles, colophon
SectionRegistry     section key → component type; the one registration point
SectionContext      cascaded section index, for the "01 / PROJECTS" label
Primitives/         ContentSection, PageContents, TagList, LinkList, IconGlyph
Sections/           Hero, Projects, Certifications, Skills, Now, Interests, Facts,
                    Connect, Contact, Activity, Repositories, Listening,
                    Explorer, CredentialHighlight
Hosts/              PageHost, ProjectHost — render roots that supply the cascade
```

`SectionRegistry` is the seam between content and code. A page's `sections:` list is
resolved through it, and an unknown key throws a `ContentException` listing the valid
keys. Adding a section type means adding one component and one dictionary entry.

The hero project index and project destinations share the same featured-project
selection. Their native anchor navigation works without enhancement. Hero links
are stationary pointer targets; only their non-interactive inner surfaces transform.
Per-link perspective and reserved clearance keep those surfaces inside the targets.
Project depth reads a stationary frame and transforms its inner surface. Listening uses
a native horizontal shelf; JavaScript only adds explicit previous/next controls.
Snapshot rendering performs no network I/O.

The evidence catalog is derived at render time from loaded, non-draft collections.
It uses exact declared topics (plus repository languages), not inferred skill
ratings. `SectionRoute` locates source pages; project detail pages and existing
outbound links prevent dangling destinations when a section is omitted.

Explorer ships the entire catalog in HTML. A disabled fieldset becomes usable
only after enhancement initializes. Search indexes are prepared once, filters
toggle existing elements, and `q`, `topic`, and `kind` are serialized to the URL.
Typing replaces the current URL after a short debounce; explicit filter changes
add history entries. Back/Forward restores the same view. Clipboard and history
failures are visible, not success-shaped fallbacks. No server search or database
is involved.

Long pages also derive their contents navigation from loaded content. The loader
uses Markdig's existing identifier support, scopes heading IDs by collection and
slug, and records plain-text, top-level h2 headings as `BodyHeadings`. Local
Markdown heading links follow those scoped IDs. Shared collection bodies cannot
collide with layout IDs or one another.

`PageContents` combines body headings with declared section titles, omits the
hero, and renders only for at least three destinations. It precedes an article,
follows a short introduction, or follows the hero on Home. Project detail pages
reuse the same component. All links and focus targets exist in generated HTML;
the script only adds current-location marking and immediately reveals a selected
destination. No scroll observer or second content catalog is added.

### `Portfolio.Generator`

Console app. Uses `Microsoft.NET.Sdk.Web` for `HtmlRenderer` and the preview server;
neither is part of the deployed output.

| Type | Role |
| --- | --- |
| `Program` | CLI; resolves paths against the repository root |
| `SiteBuilder` | Orchestrates the build |
| `DocumentTemplate` | Wraps rendered markup in the HTML shell — head, meta, JSON-LD |
| `AssetPipeline` | Concatenates stylesheet parts, content-hashes CSS and JS |
| `PreviewServer` | `--serve`: static file server plus a file watcher |
| `RepositoryRoot` | Finds the repository root from any working directory |

**Build sequence**

1. Load and validate content. Any error aborts before anything is written.
2. Reject unsafe output paths and validate section copy, duplicate routes and sections.
3. Concatenate and hash assets in an isolated temporary output directory.
4. Render each page through `PageHost`; write `/index.html` or `/<slug>/index.html`.
5. Render each project **that has a body** through `ProjectHost` to
   `/projects/<slug>/index.html`. A project without prose gets a card but no page.
6. Write `404.html`.
7. Copy content assets and everything in `static/`.
8. Write `sitemap.xml` and `robots.txt`.
9. Publish changed files only, assets before HTML, then remove obsolete generated files.
   Content/rendering errors leave the published directory untouched. The temporary
   directory is cleaned on both success and failure.

Published files with identical bytes retain their timestamps. Publication itself
is file-by-file, not an atomic filesystem transaction. A copy failure can still
require another build.

Preview changes pass through a bounded, single-reader `RebuildQueue`. Bursts coalesce;
changes arriving while a build is running trigger a follow-up build instead of being
discarded. Shutdown cancels and awaits the worker.

`HtmlRenderer` requires its dispatcher, so rendering runs inside
`renderer.Dispatcher.InvokeAsync`.

### `Portfolio.Tests`

xUnit. Covers the loader in isolation, the real `content/` tree, and the full generator
end to end. See [testing.md](testing.md).

## Routing

| Source | Route | Output |
| --- | --- | --- |
| `content/pages/010-home.md` | `/` | `dist/index.html` |
| `content/pages/020-about.md` | `/about/` | `dist/about/index.html` |
| `content/projects/020-pi.md` *(with body)* | `/projects/pi/` | `dist/projects/pi/index.html` |

A page slugged `home` or `index` becomes the root. Directory-style URLs with trailing
slashes mean no server rewriting is needed — `staticwebapp.config.json` sets
`"trailingSlash": "always"`.

## Boundaries

| Rule | Enforced by |
| --- | --- |
| Content never contains markup | Markdig runs with `.DisableHtml()` |
| Components never read files | `Portfolio.Components` has no I/O dependency |
| Content types never know about HTML | `Portfolio.Content` does not reference Components |
| The generator never invents content | It only renders what the loader produced |
| A content mistake fails loudly | Unmatched-key YAML binding, plus `RealContentTests` |

Hero text, calls to action, section headings, and section introductions live under
`hero` and `sections` in `content/site.yml`. Project `visual` keys select decorative
SVG artwork; the CSS 3D hero is presentation, not a diagram of any real deployment.

## Adding things

**A section type:** create `Sections/Foo.razor`, add `["foo"] = typeof(FooSection)` to
`SectionRegistry`, then reference `foo` in a page's `sections:`.

**A collection:** add a type deriving from `ContentEntry`, a `LoadCollection<T>("folder")`
call in `ContentLoader.LoadAll`, and a property on `PortfolioContent`.

**A page, project, certification, skill, interest, or fact:** add a file. No code.
