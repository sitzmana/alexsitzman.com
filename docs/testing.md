# Testing

## .NET regression suite

```pwsh
dotnet test Portfolio.slnx
dotnet build Portfolio.slnx -c Release
dotnet run --project src\Portfolio.Generator -c Release --no-build -- --output dist
```

**146 tests** cover the September 18, 2026 revision, including:

- Front-matter fences, slugs, ordering, drafts, and real repository content.
- Unknown and duplicate YAML keys, invalid origins/links, and unsupported artwork.
- Duplicate home routes and repeated sections.
- File-driven navigation, hero copy, section copy, and project routes.
- Project index links following project additions, removals, drafts, and page routes.
- Repository forks and listening entries, including required artists and control labels.
- Contribution date ranges, duplicate/invalid days, inclusive leap years, maximum dates,
  and totals larger than a 32-bit integer.
- Evidence destinations across renamed source pages, draft exclusion, real topic
  deduplication, missing source routes, and file-driven credential highlights.
- Required Explorer copy, invalid topics, and adding a file updating both results
  and topic options without editing templates.
- File-derived contents, section ordering, source-backed labels, short-page
  omission, hero placement, and project outlines updating with Markdown edits.
- Scoped heading anchors, duplicate headings, overlapping slug/heading names,
  Unicode fragments, inline formatting, empty headings, and native focus targets.
- Body content surviving a hidden page heading or a hero section.
- Folder assets following page routes, including `/`, and shared collection URLs.
- Output/source overlap rejection and generated-file collision detection.
- Preservation of the last good build on content/rendering failure.
- Unchanged-file timestamps and case-only destination filename changes.
- Safe JSON-LD escaping, theme metadata, sitemap metadata, and real 404 configuration.
- Watcher events arriving during a build producing a coalesced follow-up build.
- A single deployment workflow, the validated artifact handoff, prebuilt upload
  settings, and matching upload/preview-cleanup secret references.

The solution needs no Python or Node.js to build, test its .NET code, or generate.

## Browser regressions

Optional local tooling, pinned in `tests/browser/requirements.txt`; required by CI.
Nothing from this tooling is included in the deployed artifact.

```pwsh
python -m pip install -r tests\browser\requirements.txt
python -m playwright install chromium
python tests\browser\check_site.py
```

The script starts a Release preview on an available local port and stops its own
process afterward. Build Release first. To use an installed Edge browser:

```pwsh
python tests\browser\check_site.py --browser-channel msedge
```

An existing preview can be supplied with `--base-url http://localhost:5000`.
Use `--screenshots <directory>` to save local screenshots.

| Coverage | Assertions |
| --- | --- |
| 320, 375, 768, 1024, 1440 px across every sitemap route | No horizontal overflow, one h1, unique IDs, labelled controls |
| Reduced motion | All content visible; progress hidden |
| JS disabled, script blocked, IntersectionObserver absent | Content remains visible at 320 and 1440 px |
| Forced colors | Content visible, opaque hero text |
| Keyboard | Skip link focuses main; Enter on project index focuses the corresponding project |
| Reading navigation at 320 and 1440 px | Touch/keyboard contents links focus real destinations; Back/Forward restore sections; direct fragments and return-to-top work with scripts disabled/blocked and in forced colors |
| Reading navigation fallbacks | 44 px targets, immediately visible destinations, authored labels, no contents controls in print, article headings retained |
| Touch | No pointer tilt; project links work; record controls and native swiping scroll the shelf |
| Project geometry | Pointer transforms the actual project surface and resets on exit |
| Hero hover at 768, 959, 1024, 1440 px, with and without JS | Stationary link bounds throughout transitions; surfaces remain contained; all eight edge/corner points retain the same link; edge clicks navigate correctly |
| Record shelf | Start/end button state, native fallback scrolling, one copy of each record |
| Live preference changes | Tilt reset, content revealed, transitions disabled |
| Idle interactions | No continuously scheduled animation frames or autoplay |
| Initial enhancement | Layout shift below 0.01 |
| Hosting CSP | Interaction checks run with configured global headers |
| Growing content | Additional navigation items and long tags still fit 320 px |
| Routing | Real local HTTP 404 for unknown routes |
| Networking | No third-party requests in the responsive route sweep |
| Explorer at 320x568, 375x812, and 1440x960 | Keyword/topic/type filtering, touch controls, shareable URLs, Back/Forward, reset and empty states |
| Explorer failure modes | Invalid URL filters explained; clipboard denial offers manual copy; blocked history updates do not break search |
| Explorer safety and navigation | Search strings remain text; hosting CSP enforced; real destination anchors; all entries retained in print and without scripts |

Screenshots were visually inspected at 320 and 1440 px, including the project
depth state and restored About content. The suite also checks print visibility
and that the entire record collection fits the printed page.
The September 18 run covers six routes: 30 responsive checks, 48 fallback checks,
24 stationary-hover checks, and 24 reading-navigation scenarios under the hosting
headers, with no browser errors or third-party requests.
The responsive/fallback route list comes from the generated sitemap, so new page
files receive the same coverage automatically.

The hover regression reproduced the previous moving-target failure before the
fix. Sampling is driven from Playwright, not page timers, so the no-JavaScript
case tests real disabled-script behavior without hanging on suppressed callbacks.

## CI and deployment

`azure-static-web-apps-jolly-water-0e62d601e.yml` handles pushes and pull requests,
calls reusable `ci.yml`, and only
deploys the resulting artifact if validation succeeds. CI can also be run manually.
This avoids two independent build pipelines for the same change.

The shared pipeline scans for credentials, restores locked packages, builds
Release, runs .NET tests, generates the site, enforces the exact byte budget,
and runs Chromium browser regressions. Browser tests do not receive deployment
secrets. Workflows were checked locally with actionlint.

## Remaining limits

No screenshot-diff baseline, screen-reader session, axe-core/Lighthouse audit,
Firefox/WebKit coverage, physical-device testing, or external-link reachability
test is claimed. Markdown alt text is still the author's responsibility.
Actual cloud cache headers and 404 behavior require post-deployment verification.
