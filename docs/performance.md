# Performance

## Measured payload

Measured from the September 18, 2026 Release output. Gzip is a local compression
measurement, not a claim about a deployed CDN response.

| Resource | Raw bytes | Gzip bytes |
| --- | --- | --- |
| Home HTML | 30,000 | 4,436 |
| Shared stylesheet | 52,665 | 9,934 |
| Enhancement script | 12,402 | 3,506 |
| **Home HTML + CSS + JS** | **95,067** | **17,876** |
| About HTML | 52,383 | 5,786 |
| Now HTML | 7,166 | 2,241 |
| Explore HTML | 31,018 | 5,046 |
| Credentials HTML | 12,062 | 2,488 |
| This site HTML | 12,907 | 3,791 |
| Whole generated site | 216,614 | 40,008 |

The home payload is **92.8 KiB raw / 17.5 KiB gzip**, below the **150 KiB**
uncompressed budget. The project index, depth-enabled cards, record shelf, Explorer,
and reading navigation deliberately add bytes over the old design; they do not add a browser framework,
font download, model file, or third-party request.

`scripts/check-performance.ps1` counts the actual bytes of the assets referenced
by the home document. It fails at or above 153,600 bytes, rather than rounding each
resource down to whole KiB. The shared CI/deployment pipeline runs this gate.

## Browser work

- Static .NET-generated HTML; no production application server or .NET download.
- One stylesheet and one deferred script, shared across pages.
- Original inline SVG and CSS artwork, with no external image request.
- Hero hover animates only a visual surface inside a stationary link. It needs
  no JavaScript, pointer listeners, geometry reads, or polling.
- The hero heading is visible immediately; it never waits for an observer.
- Only offscreen reveal targets are observed, and each is unobserved on reveal.
- The former unused active-section observer has been removed.
- Pointer events coalesce into at most one pending animation frame. Each frame
  reads geometry before writing styles. There is no continuous render loop.
- Tilt resets on pointer exit/cancellation, page hiding, loss of window focus,
  and motion-preference changes. Touch does not register tilt listeners.
- Shelf controls run only on input, scroll, resize, or motion-preference changes.
  There is no autoplay, polling, runtime GitHub request, or Spotify embed.
- Reduced motion also cancels a shelf scroll already in progress.
- Explorer prepares its text/topic index once and filters existing elements on
  input. Address-bar updates are debounced; there is no search API or idle polling.
- The home credential collection is now a smaller featured panel; the complete
  collection lives on its own page rather than lengthening the mobile homepage.
- Contents links and heading anchors are generated at build time. Current-location
  marking reuses the existing hash-change handling, without a scroll-spy observer,
  layout-measuring loop, or another browser dependency.

Browser regressions assert no new animation-frame requests while the pointer is
idle and less than 0.01 measured home-page layout shift during enhancement initialization.
These checks do not establish device-wide zero CPU usage.

## Build and preview efficiency

The generator builds in a temporary directory and publishes only changed files.
Unchanged files retain timestamps, reducing filesystem writes and sync activity.
Content/rendering failures preserve the last good preview. The watcher coalesces
bursts and retains events arriving during a build.

Person JSON-LD is serialized once per build, not once per page. Asset hashing and
writing reuse the same UTF-8 byte array. Output accounting includes copied assets.

Deployment now calls the reusable CI workflow instead of independently repeating
the same build and test steps. Validation still gates the deployable artifact.

## Caching and missing routes

Global responses revalidate with `public, max-age=0, must-revalidate`, including
directory routes such as `/about/`. Content-hashed `/assets/*` files override
this with `public, max-age=31536000, immutable`.

There is no SPA navigation fallback. Unknown routes use the explicit 404
response override rather than returning success with a missing-page document.
Hosted behavior must still be checked after deployment; no deployment was made
as part of this revision.

## Reproducing

```pwsh
dotnet build Portfolio.slnx -c Release
dotnet run --project src\Portfolio.Generator -c Release --no-build -- --output dist
.\scripts\check-performance.ps1
```

See `docs/testing.md` for browser checks. No Lighthouse score, field Core Web
Vitals measurement, Brotli transfer measurement, or real-device battery result
is claimed. The stylesheet still ships as a single shared bundle.
