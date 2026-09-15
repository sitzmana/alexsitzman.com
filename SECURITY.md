# Security policy

## Reporting a vulnerability

Email **alexander@sitzman.net** with the details and a way to reproduce. Please do not
open a public issue for anything exploitable.

## Threat surface

This is a static site. There is no database, no API, no authentication, no user input, no
session, and no server-side code running in production. The deployed artifact is HTML,
CSS, SVG, and one JavaScript file.

That removes most of the OWASP Top 10 by construction. What remains:

| Risk | Control |
| --- | --- |
| Cross-site scripting through content | Markdig runs with `.DisableHtml()`. Raw HTML in a content file is escaped, not rendered. Asserted by `Raw_html_in_content_is_not_passed_through`. |
| Injected or third-party scripts | `Content-Security-Policy` with `default-src 'self'`, `object-src 'none'`, `base-uri 'self'`, `form-action 'none'` |
| Clickjacking | `frame-ancestors 'none'` |
| MIME sniffing | `X-Content-Type-Options: nosniff` |
| Downgrade to HTTP | `Strict-Transport-Security` with `includeSubDomains; preload` |
| Referrer leakage | `Referrer-Policy: strict-origin-when-cross-origin` |
| Tab-nabbing on outbound links | Every external link carries `rel="noopener noreferrer"` |
| Unused browser capability | `Permissions-Policy` denies camera, microphone, geolocation |
| Supply chain | Two build-time packages, central version management, `packages.lock.json`, CI restores with `--locked-mode` |
| Credential exposure in CI | `permissions: contents: read` by default; `pull-requests: write` only on the jobs that need it; `persist-credentials: false` on checkout |

Headers are configured in
[static/staticwebapp.config.json](../static/staticwebapp.config.json).

## Known weakness

The CSP includes `script-src 'self' 'unsafe-inline'`. One inline script is required: the
`no-js` → `js` class swap in `<head>`, which must run synchronously before first paint so
that content is never hidden when scripting is unavailable (see
[decisions.md](decisions.md) D10).

This can be tightened to a `'sha256-...'` source. Doing so requires the generator to hash
the inline script at build time and write the value into `staticwebapp.config.json`. **Not
currently implemented.**

The practical exposure is low — there is no user input, no query-string handling, and no
third-party script — but it is a real gap and is recorded rather than glossed over.

## Secrets

**No secret belongs in this repository.** Everything in `content/`, `assets/`, `static/`,
and the generated output is public by definition; it is served to anyone who visits the
site.

The only credential involved is `AZURE_STATIC_WEB_APPS_API_TOKEN`, stored as a GitHub
repository secret and referenced only by the deploy workflow. It is never echoed, never
written to the artifact, and never placed in `staticwebapp.config.json`.

If it is ever exposed, rotate it:

```bash
az staticwebapp secrets reset-api-key \
  --name swa-alexsitzman \
  --resource-group rg-alexsitzman-web

gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN
```

## Dependencies

| Package | Scope | Purpose |
| --- | --- | --- |
| Markdig | Build only | Markdown → HTML |
| YamlDotNet | Build only | Front-matter parsing |

Neither reaches the browser. The site has zero runtime dependencies.

Versions are pinned centrally in
[Directory.Packages.props](../Directory.Packages.props) and locked in
`packages.lock.json`. CI restores with `--locked-mode`, so a dependency cannot change
without a reviewable diff to a lock file.

## Hardening not yet applied

- GitHub Actions are pinned to major version tags, not commit SHAs. Command to pin is in
  [deployment.md](deployment.md).
- No Dependabot or Renovate configuration.
- No `Subresource-Integrity` attributes — not applicable, since all assets are same-origin.
