# Release readiness

Status as of the current commit. Every claim below is backed by a command that was run and
whose output is quoted. Anything unverified is listed as unverified.

## Verdict

**Ready to deploy, pending one human action:** create the Azure Static Web App and set the
`AZURE_STATIC_WEB_APPS_API_TOKEN` secret. That step creates an Azure resource and was not
performed without authorisation. Commands are in [deployment.md](deployment.md).

No Azure resource was created. No deployment was performed. No billable action was taken.

---

## Evidence

### Build

```
dotnet restore Portfolio.slnx --locked-mode
  All projects are up-to-date for restore.

dotnet build Portfolio.slnx -c Release --no-restore
  Build succeeded.
  0 Warning(s), 0 Error(s)
```

`TreatWarningsAsErrors` is on, so zero warnings is enforced rather than incidental.

### Tests

```
dotnet test Portfolio.slnx -c Release --no-build
  Passed!  - Failed: 0, Passed: 54, Skipped: 0, Total: 54, Duration: 351 ms
```

Breakdown in [testing.md](testing.md): 5 front-matter, 10 slug, 17 loader, 9 real-content,
13 end-to-end.

### Generation

```
dotnet run --project src/Portfolio.Generator -c Release --no-build -- --output dist
  Built 3 routes, 6 files, 63.9 KB in 260 ms
    /
    /about/
    /now/
```

Output assertions, matching the CI step:

```
  OK    index.html            OK    404.html
  OK    about\index.html      OK    sitemap.xml
  OK    now\index.html        OK    robots.txt
  OK    favicon.svg           OK    staticwebapp.config.json
  OK    og.svg
  OK    assets\site.a80bc155a4.css
  OK    assets\enhance.5389674b26.js

  Home page payload: 43.8 KB (budget 150 KB)
```

### Secrets

```
pwsh ./scripts/check-secrets.ps1
  Scanned 105 files. No credential-like strings found.
```

Runs in CI on every push and pull request.

---

## Definition of done

| Requirement | Status | Evidence |
| --- | --- | --- |
| Site builds | Met | `Build succeeded. 0 Warning(s), 0 Error(s)` |
| Automated tests pass | Met | 54/54 |
| Production publish succeeds | Met | Release generation, 63.9 KB output |
| Content preserved or flagged | Met | [content-inventory.md](content-inventory.md), [content-review-needed.md](content-review-needed.md) |
| Visual design original and cohesive | Met | [design-system.md](design-system.md), [design-research.md](design-research.md) |
| Desktop and mobile validated | Met | 0 overflow at 320/375/390/768/1024/1440/1920 px × 3 routes |
| Navigation by mouse, touch, keyboard | Met | Plain links throughout; 8 tab stops verified with visible focus |
| Reduced motion works | Met | 0/18 hidden, progress bar `display: none` |
| Accessibility reviewed | Met | [accessibility.md](accessibility.md) — 0 contrast, 0 target-size violations |
| Performance reviewed | Met | [performance.md](performance.md) — 9.9 KB gzipped home page |
| No database required | Met | Static files only |
| Azure deployment config present | Met | [deploy.yml](../.github/workflows/deploy.yml), [staticwebapp.config.json](../static/staticwebapp.config.json) |
| GitHub Actions validation present | Met | [ci.yml](../.github/workflows/ci.yml) |
| No secrets committed | Met | Scan clean, enforced in CI |
| No known critical defects | Met | See below |
| Review findings fixed or justified | Met | 5 defects found and fixed; see below |

## Defects found during review and fixed

Each was found by a check, not by inspection, and each is covered by the thing that found
it or by a test.

| # | Defect | Found by | Fix |
| --- | --- | --- | --- |
| 1 | `order:` and the filename prefix sorted on independent scales, so folder order did not predict page order | `Explicit_order_wins_over_the_file_prefix` | Unified onto one number line (D4) |
| 2 | Gradient text rendered invisible where `background-clip: text` is unsupported — including the site's own name | Contrast measurement | Opaque fallback outside `@supports` (D12) |
| 3 | Email address link was 161×15 px, below the 24×24 AA minimum | Target-size measurement | `min-height: 2.75rem` |
| 4 | Every rebuild after the first failed with "access is denied" on OneDrive-synced folders | Running `--serve` twice | Clear `ReadOnly`, delete bottom-up, bounded retry (D14) |
| 5 | Preview server returned 404 for `/` and `/about/` while `/index.html` worked | Probing routes with `curl` | Terminal middleware instead of `MapFallback` (D15) |

Defects 1 and 5 would both have been easy to misdiagnose later; both are now documented
with the reasoning in [decisions.md](decisions.md).

---

## Requires human input

| # | Item | Why it cannot be resolved here |
| --- | --- | --- |
| 1 | Create the Static Web App, set `AZURE_STATIC_WEB_APPS_API_TOKEN` | Creates an Azure resource; needs authorisation |
| 2 | Confirm `sitzmaa` vs `sitzmana` for the DAS Driver repository | Both appear on the old site; guessing would break a link |
| 3 | Decide on the Spotify and GitHub-repositories panels | Both were non-functional; rebuilding needs a product decision |
| 4 | Add `role`, `period`, and body prose to projects | Inventing engineering detail is not acceptable |
| 5 | Confirm title, location, email, and current focus are still accurate | Only you know |
| 6 | Add certification verification links | Credential URLs are not public on the old site |

Items 2–6 are detailed in [content-review-needed.md](content-review-needed.md). Item 4 is
the highest-value content improvement available.

---

## Known limitations

Carried from [README.md](../README.md) and the individual documents:

- **No Lighthouse or WebPageTest run.** Payload is measured and enforced in CI; no score is
  claimed.
- **Chromium only.** No Firefox or WebKit verification.
- **No screen-reader testing.** Structure was confirmed through the accessibility tree,
  which is not the same thing.
- **Browser checks are not automated.** Contrast, overflow, reduced-motion, and no-JS
  checks were run manually and recorded; they do not gate a pull request.
- **CSP retains `'unsafe-inline'` for one inline script.** Tightening it to a hash requires
  build-time hash injection. Recorded in [SECURITY.md](../SECURITY.md).
- **Actions pinned to major tags, not SHAs.** Command to pin is in [deployment.md](deployment.md).
- **No forced-colors / High Contrast Mode testing.**
- **Alt text on author-supplied images is not enforced by a test.**
- **One theme, no webfont** — both deliberate, recorded as D7 and D6.

## Post-deployment checklist

After the first deploy, before pointing the domain:

1. Every route returns 200 on the `*.azurestaticapps.net` URL.
2. An unknown path returns a genuine 404 status, not 200 with 404 content.
3. `Strict-Transport-Security`, `Content-Security-Policy`, and `X-Content-Type-Options`
   are present on a response.
4. `/sitemap.xml` and `/robots.txt` resolve and contain the production hostname.
5. `baseUrl` in `content/site.yml` matches the final canonical hostname.
6. Browser console is clean — no CSP violations from the inline bootstrap script.

Commands for 1–4 are in [deployment.md](deployment.md).
