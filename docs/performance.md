# Performance

## Budget

Set before implementation, enforced in CI by
[.github/workflows/ci.yml](../.github/workflows/ci.yml). The build fails if the home page
payload exceeds it.

| Metric | Budget | Measured | Headroom |
| --- | --- | --- | --- |
| Home page total (HTML + CSS + JS, uncompressed) | 150 KB | **43.8 KB** | 71% |
| Blocking requests before first paint | ≤ 2 | **1** (the stylesheet) | — |
| Webfonts | 0 | **0** | — |
| Third-party requests | 0 | **0** | — |
| JavaScript frameworks | 0 | **0** | — |

## Measured output

Release build, `dotnet run --project src/Portfolio.Generator -c Release -- --output dist`.
Gzip measured at `SmallestSize`; Azure Static Web Apps serves Brotli where the client
supports it, which is typically smaller still.

| File | Raw | Gzip |
| --- | --- | --- |
| `assets/site.<hash>.css` | 26,799 B | 5,882 B |
| `index.html` | 15,045 B | 3,156 B |
| `about/index.html` | 8,656 B | 2,278 B |
| `now/index.html` | 6,103 B | 2,048 B |
| `assets/enhance.<hash>.js` | 2,956 B | 1,117 B |
| `404.html` | 2,061 B | 933 B |
| `og.svg` | 1,676 B | 690 B |
| `staticwebapp.config.json` | 1,350 B | 663 B |
| `favicon.svg` | 424 B | 283 B |
| `sitemap.xml` | 279 B | 163 B |
| `robots.txt` | 73 B | 89 B |
| **Whole site** | **63.9 KB** | **16.9 KB** |

**A first visit to the home page transfers about 9.9 KB compressed** — HTML, stylesheet,
and script combined. Every subsequent page is HTML only, because the CSS and JS are
served immutably from cache.

## What makes it small

- **No client runtime.** Static generation instead of Blazor WebAssembly removes a
  multi-megabyte download. See D1 in [decisions.md](decisions.md).
- **No webfont.** A system font stack. A single variable-font subset would be 25–60 KB —
  larger than the entire current payload. See D6.
- **No framework.** `enhance.js` is 2.9 KB of hand-written code and is `defer`-loaded, so
  it never blocks rendering.
- **No images.** The hero backdrop is CSS gradients and repeating linear gradients. The
  favicon and Open Graph image are SVG, and the OG image is never fetched by a browser
  during a normal visit.
- **No third parties.** No analytics, no fonts, no CDN, no embeds. Zero DNS lookups beyond
  the origin.

## Core Web Vitals position

No Lighthouse run has been performed, so no score is claimed. The structural risks that
drive each metric were reviewed:

**LCP** — the largest element is the hero heading, which is text in the initial HTML
response with no webfont to wait for. It is not inside a `.reveal` wrapper that would
delay it behind an `IntersectionObserver`; the hero reveals immediately on load.

**CLS** — the main risk in this design is the reveal animation. It animates `opacity` and
`transform` only, both of which are compositor properties that do not affect layout.
Elements occupy their final space before they become visible, so revealing shifts nothing.
There are no ads, embeds, or late-loading images.

**INP** — there is no JavaScript in the interaction path. Navigation is plain links;
hover and focus styling is CSS. The only scripted work is three `IntersectionObserver`
callbacks that toggle a class, and each observer calls `unobserve` after firing.

## Caching

Set in [static/staticwebapp.config.json](../static/staticwebapp.config.json):

| Path | `Cache-Control` |
| --- | --- |
| `/assets/*` | `public, max-age=31536000, immutable` |
| `/*.html` | `public, max-age=0, must-revalidate` |

Assets carry a content hash in the filename, so a one-year immutable cache is safe — a
changed stylesheet gets a new URL. HTML revalidates on every request, so a deploy is
visible immediately and can never pair new HTML with a stale stylesheet.

## Animation cost

Every animated property is `opacity` or `transform`, which stay on the compositor and do
not trigger layout or paint. No `width`, `height`, `top`, or `margin` is animated
anywhere.

The scroll progress bar uses CSS `animation-timeline: scroll()` where supported, so there
is no scroll event listener and no work on the main thread during scrolling. Where
unsupported, it simply does not animate — there is no JavaScript fallback, by choice.

There is no continuously running animation. Nothing moves unless the user scrolls, hovers,
or focuses. Idle CPU is zero, which matters on battery.

## Known limitations

- **No Lighthouse or WebPageTest run.** Payload is measured and enforced; field metrics
  are not.
- **No real-device testing.** Responsive behaviour was verified in Chromium at seven
  viewport widths, not on physical hardware.
- **CSS ships whole.** All 26.8 KB is delivered to every page, including rules for
  sections that page does not use. Splitting per-page would save perhaps 30% of 5.9 KB
  gzipped, at the cost of losing the shared immutable cache across navigations. Not worth
  it at this size.
- **No Brotli measurement.** Azure applies it at the edge; the figures above are gzip.

## Re-measuring

```pwsh
dotnet run --project src/Portfolio.Generator -c Release -- --output dist
Get-ChildItem -Recurse -File dist | Sort-Object Length -Descending |
  Select-Object @{n='KB';e={[math]::Round($_.Length/1KB,1)}}, Name
```
