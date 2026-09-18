# alexsitzman.com

Personal portfolio for Alex Sitzman — Senior Technical Support Engineer, Microsoft Azure.

Written in C# and Razor, rendered to **static HTML at build time**, and hosted on Azure
Static Web Apps. There is no database, no API, and no server-side runtime in production:
the deployed artifact is HTML, one stylesheet, and one small script.

**The site is driven entirely by files in [content/](content/).** Add a Markdown file,
get a card, a section, or a whole new page. No code changes. See
[content/README.md](content/README.md).

---

## Architecture

```
content/          Markdown + YAML. The only thing you edit to change the site.
assets/           Stylesheet parts and the progressive-enhancement script.
static/           Copied verbatim to the site root (favicon, OG image, SWA config).
src/
  Portfolio.Content      Content model and loader. Parses files, fails loudly.
  Portfolio.Components   Razor components. Presentation only.
  Portfolio.Generator    Console app. Renders components to static HTML.
tests/
  Portfolio.Tests        Unit and end-to-end tests over the whole pipeline.
dist/                    Build output. Git-ignored. This is what gets deployed.
```

The generator uses `HtmlRenderer` from ASP.NET Core to render Razor components to
HTML strings at build time, then writes them to disk.

### Why static generation rather than Blazor WebAssembly

Blazor WebAssembly was the starting assumption and was rejected. For a content site
with no interactive application state, it costs a multi-megabyte runtime download, a
blank frame before first paint, and a dependency on JavaScript for the page to exist
at all. Static generation keeps the authoring model — C#, Razor, strong typing — and
removes all three costs. The project index, depth-enabled cards, record shelf, and
evidence explorer add no client framework: the home page is 92.8 KiB uncompressed,
or 17.5 KiB with local gzip.

Full reasoning and the rejected alternatives are in [docs/decisions.md](docs/decisions.md).

---

## Prerequisites

- [.NET SDK 10.0.400](https://dotnet.microsoft.com/download) or a later 10.0.4xx feature band
  (pinned in [global.json](global.json))

Nothing else is needed to build or generate. Browser regression checks use optional
Python/Playwright tooling described in [docs/testing.md](docs/testing.md). There is
no Node.js, npm, or CSS toolchain requirement.

---

## Running it

```pwsh
# Build the site into ./dist
dotnet run --project src/Portfolio.Generator -- --output dist

# Build, then serve on http://localhost:5000 and rebuild whenever a file changes
dotnet run --project src/Portfolio.Generator -- --serve

# Include entries marked `draft: true`
dotnet run --project src/Portfolio.Generator -- --serve --drafts

# All options
dotnet run --project src/Portfolio.Generator -- --help
```

`--serve` watches `content/`, `assets/`, and `static/`. Saving a Markdown file rebuilds
the site; refresh the browser to see it. Bursts are coalesced, changes during a build
are retained, and content errors leave the last good preview intact.

---

## Testing

```pwsh
dotnet test Portfolio.slnx
```

The 146-test suite covers front-matter parsing, slug rules, ordering, drafts, output
safety, preview rebuild behavior, metadata, and end-to-end generated HTML.

`RealContentTests` loads the actual `content/` folder, so a malformed or mis-keyed content
file fails the test suite with the offending file path rather than silently vanishing from
the site.

See [docs/testing.md](docs/testing.md).

---

## Production build

```pwsh
dotnet build Portfolio.slnx -c Release
dotnet test Portfolio.slnx -c Release --no-build
dotnet run --project src/Portfolio.Generator -c Release --no-build -- --output dist
```

`dist/` is the complete deployable artifact.

---

## Updating content

Everything is a file. The full reference is in [content/README.md](content/README.md); the
short version:

| To do this | Do that |
| --- | --- |
| Change your title, location, email, or profile links | Edit `content/site.yml` |
| Change hero copy, section headings, or calls to action | Edit `hero` and `sections` in `content/site.yml` |
| Select project artwork | Set `visual: network`, `stack`, or `signal` in the project file |
| Add a project | Add `content/projects/040-name.md` |
| Add a certification | Add `content/certifications/030-name.md` |
| Feature a credential on Home | Set `featured: true` in its certification file |
| Change Explorer labels or state messages | Edit `explorer` in `content/site.yml` |
| Change reading-navigation labels | Edit `pageNavigation` in `content/site.yml` |
| Update what you're working on | Edit or add a file in `content/now/` |
| Add a skill | Add one line to a file in `content/skills/` |
| Add a record to the music shelf | Add a file in `content/listening/` with artist and labelled link |
| Update the repository/activity snapshots | Edit `content/repositories/` or `content/activity/`, and their dated section notes |
| Add a whole new page | Add `content/pages/040-name.md` |
| Reorder anything | Change the numeric filename prefix, or set `order:` |
| Hide something temporarily | Set `draft: true` |

A numeric prefix (`020-`) sets sort position and is stripped from the URL. Front-matter
`order:` shares the same number line and overrides the prefix.

Mistakes fail the build with the file name and line number. They never fail silently.

## Exploring the portfolio

`/explore/` connects declared topics to projects, credentials, repositories, and
work in progress. Search, topic selection, and evidence-type filters work together;
their state is bookmarkable and shareable. Clipboard denial offers a manual-copy
link. Filtering runs locally, without an API, search service, or analytics.

The catalog is generated from existing content files, not maintained separately.
Linked skills on Home open the matching Explorer topic. `/credentials/` contains
the full credential collection; a smaller featured panel keeps Home focused.
All six pages and the complete catalog remain readable with scripts disabled.

`/site/` explains the actual static pipeline, browser payload, and design tradeoffs.
Long pages get **On this page** navigation derived from their Markdown h2 headings
and declared sections; project detail pages reuse the same behavior. Native anchors
move keyboard focus, and a footer link returns to the top without requiring scripts.

---

## Deployment

Push to `main`. [.github/workflows/deploy.yml](.github/workflows/deploy.yml) calls the
shared CI workflow to build, test, generate, enforce the byte budget, and check browser
behavior before deploying to Azure Static Web Apps. Pull requests
get a preview environment that is torn down when the PR closes.

**Azure resources required:** one Static Web App (Free tier is sufficient). No database,
no storage account, no compute.

**Required secret:** `AZURE_STATIC_WEB_APPS_API_TOKEN_JOLLY_WATER_0E62D601E`
(created by Azure). Upload and preview cleanup use the same secret. Keep
`deploy.yml` as the only deployment entry point; a second portal-generated
workflow must not bypass the site's build and validation.

Setup, custom-domain migration, and rollback are in [docs/deployment.md](docs/deployment.md).

---

## Documentation

| Document | Contents |
| --- | --- |
| [docs/architecture.md](docs/architecture.md) | Component boundaries and the build pipeline |
| [docs/decisions.md](docs/decisions.md) | Every significant decision, with the rejected alternatives |
| [docs/design-system.md](docs/design-system.md) | Tokens, type scale, colour, motion |
| [docs/current-site-audit.md](docs/current-site-audit.md) | Audit of the previous site |
| [docs/content-inventory.md](docs/content-inventory.md) | What was carried over and where it lives now |
| [docs/content-review-needed.md](docs/content-review-needed.md) | **Items needing your verification** |
| [docs/accessibility.md](docs/accessibility.md) | WCAG 2.2 AA position and measured results |
| [docs/performance.md](docs/performance.md) | Budget and measured payloads |
| [docs/testing.md](docs/testing.md) | Test strategy and coverage |
| [docs/deployment.md](docs/deployment.md) | Azure setup, domains, rollback |

---

## Known limitations

- **One theme.** Dark only. A light theme was deliberately not shipped rather than
  shipping two mediocre ones.
- **No webfont.** The site uses a system font stack, so typography differs slightly
  between Windows, macOS, and Linux. This buys a zero-byte font payload.
- **Snapshots, not live integrations.** The original site's twelve Spotify-linked
  tracks, five public repositories, and GitHub activity were restored from verified
  September 17, 2026 sources. Updating them means editing content files; there is
  no runtime API, token store, autoplay, or third-party embed.
- **No analytics.** Nothing tracks visitors. Adding any is an explicit decision.
- **Lighthouse has not been run.** Payload sizes are measured and enforced in CI, but no
  Lighthouse score has been produced, and none is claimed.
