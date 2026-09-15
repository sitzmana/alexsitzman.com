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
removes all three costs. The whole site is under 70 KB uncompressed.

Full reasoning and the rejected alternatives are in [docs/decisions.md](docs/decisions.md).

---

## Prerequisites

- [.NET SDK 10.0.400](https://dotnet.microsoft.com/download) or a later 10.0.4xx feature band
  (pinned in [global.json](global.json))

Nothing else. No Node.js, no npm, no CSS toolchain.

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
the site in roughly 200 ms; refresh the browser to see it.

---

## Testing

```pwsh
dotnet test Portfolio.slnx
```

The suite covers front-matter parsing, slug rules, ordering, draft handling, and a set of
end-to-end tests that run the real generator and assert on the produced HTML.

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
| Add a project | Add `content/projects/040-name.md` |
| Add a certification | Add `content/certifications/030-name.md` |
| Update what you're working on | Edit or add a file in `content/now/` |
| Add a skill | Add one line to a file in `content/skills/` |
| Add a whole new page | Add `content/pages/040-name.md` |
| Reorder anything | Change the numeric filename prefix, or set `order:` |
| Hide something temporarily | Set `draft: true` |

A numeric prefix (`020-`) sets sort position and is stripped from the URL. Front-matter
`order:` shares the same number line and overrides the prefix.

Mistakes fail the build with the file name and line number. They never fail silently.

---

## Deployment

Push to `main`. [.github/workflows/deploy.yml](.github/workflows/deploy.yml) restores,
builds, tests, generates, and only then deploys to Azure Static Web Apps. Pull requests
get a preview environment that is torn down when the PR closes.

**Azure resources required:** one Static Web App (Free tier is sufficient). No database,
no storage account, no compute.

**Required secret:** `AZURE_STATIC_WEB_APPS_API_TOKEN`.

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
- **Two features from the previous site were not rebuilt:** the Spotify "what I'm
  listening to" panel and the live GitHub repository list. Both require a runtime API
  call; both were non-functional on the previous site. See
  [docs/content-review-needed.md](docs/content-review-needed.md).
- **No analytics.** Nothing tracks visitors. Adding any is an explicit decision.
- **Lighthouse has not been run.** Payload sizes are measured and enforced in CI, but no
  Lighthouse score has been produced, and none is claimed.
