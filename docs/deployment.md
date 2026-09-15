# Deployment

The deployed artifact is a folder of static files. Azure Static Web Apps serves it.
No database, no compute, no storage account.

## Azure resources

| Resource | Tier | Cost |
| --- | --- | --- |
| Static Web App | Free | $0 |

The Free tier includes 100 GB bandwidth/month, custom domains, managed TLS certificates,
and pull-request preview environments. This site is under 70 KB per full page load, so
the free allowance is not a practical constraint.

No other Azure resource is required. If a resource group does not already exist, one is
needed, but it costs nothing.

## One-time setup

These commands create the Static Web App. **They create a billable-capable resource** —
review before running. The Free SKU costs nothing, but the subscription and region are
your choice.

```bash
az login
az account set --subscription "<subscription-id>"

az group create \
  --name rg-alexsitzman-web \
  --location westus2

az staticwebapp create \
  --name swa-alexsitzman \
  --resource-group rg-alexsitzman-web \
  --location westus2 \
  --sku Free
```

`--location` for a Static Web App selects the control-plane region; content is served
from a global edge network regardless.

Retrieve the deployment token:

```bash
az staticwebapp secrets list \
  --name swa-alexsitzman \
  --resource-group rg-alexsitzman-web \
  --query "properties.apiKey" -o tsv
```

Store it as a repository secret named `AZURE_STATIC_WEB_APPS_API_TOKEN`:

```bash
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN
```

This token is a deployment credential. It must never be committed, echoed in workflow
logs, or added to `staticwebapp.config.json`.

## How deployment works

[.github/workflows/deploy.yml](../.github/workflows/deploy.yml) runs on push to `main`
and on pull requests.

```
build job          restore (locked) -> build -> test -> generate -> upload artifact
                                    |
                                    v  only if the build job succeeded
deploy job         download artifact -> Azure/static-web-apps-deploy
```

Deployment is a separate job that `needs: build`. A failing test cannot deploy.

The artifact is pre-built, so the deploy step sets `skip_app_build: true` and
`output_location: ''`. Without those, Azure's Oryx build system would try to detect and
rebuild the project in its own container, which would either fail or silently produce
different output.

**Permissions.** The workflow declares `contents: read` at the top level. Only the deploy
and teardown jobs add `pull-requests: write`, which the deploy action needs to comment
the preview URL on a PR.

**Pull requests** deploy to a temporary preview environment. The `close-preview` job tears
it down when the PR closes.

**Action pinning.** Actions are pinned to major version tags (`@v4`, `@v1`). For stricter
supply-chain control, pin to full commit SHAs:

```bash
gh api repos/actions/checkout/commits/v4 --jq .sha
```

Then replace `actions/checkout@v4` with `actions/checkout@<sha> # v4`. This is a
deliberate tradeoff: SHAs stop a compromised tag from being re-pointed, at the cost of
manual updates. Currently not applied.

## Custom domain migration

The domain currently points at the existing host. Migrate with no downtime:

1. **Deploy and verify first.** Let the workflow run and confirm the site works on the
   generated `*.azurestaticapps.net` URL. Check every route, the 404 page, and
   `/sitemap.xml`.
2. **Lower the TTL** on the existing DNS records to 300 seconds. Wait for the old TTL to
   expire so resolvers pick up the short one.
3. **Add the apex domain** in Azure:
   ```bash
   az staticwebapp hostname set \
     --name swa-alexsitzman \
     --resource-group rg-alexsitzman-web \
     --hostname alexsitzman.com
   ```
   Azure returns a TXT validation record. Add it at the DNS provider and wait for
   validation.
4. **Add the `www` subdomain** as a CNAME to the `*.azurestaticapps.net` hostname.
5. **Switch the apex record** to the ALIAS/ANAME target Azure provides. Apex CNAMEs are
   not valid; if the DNS provider does not support ALIAS records, use Azure DNS.
6. **Wait for the managed certificate.** Azure issues it automatically after validation,
   usually within minutes. Do not proceed until HTTPS works.
7. **Verify** both `https://alexsitzman.com` and `https://www.alexsitzman.com`, then
   restore a longer DNS TTL.
8. **Confirm `baseUrl`** in `content/site.yml` matches the final canonical hostname. It
   feeds canonical tags, Open Graph URLs, and the sitemap. It is currently
   `https://www.alexsitzman.com` — if the apex becomes canonical instead, change it and
   redeploy.

Keep the previous host running until step 7 passes.

## Rollback

Deployments are immutable and identified by commit.

**Fastest — revert the commit:**

```bash
git revert <bad-commit-sha>
git push origin main
```

The workflow runs and redeploys the previous content. This is the recommended path: it
leaves an audit trail and cannot desynchronise the repository from what is live.

**Faster, manual — redeploy a known-good artifact:** re-run the last successful `Deploy`
workflow run from the Actions tab. Use this only if the repository state is fine and the
deployment itself failed.

**Emergency — take the site down:** delete the custom domain binding. DNS reverts to the
previous host if it is still running.

There is no database, so no rollback ever involves data loss or a migration.

## Response headers

Set in [static/staticwebapp.config.json](../static/staticwebapp.config.json), which is
copied to the output root.

| Header | Value | Purpose |
| --- | --- | --- |
| `Content-Security-Policy` | `default-src 'self'` and tight per-directive sources | Blocks injected and third-party scripts |
| `Strict-Transport-Security` | `max-age=63072000; includeSubDomains; preload` | Forces HTTPS |
| `X-Content-Type-Options` | `nosniff` | Stops MIME sniffing |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage |
| `Permissions-Policy` | camera, microphone, geolocation denied | Removes unused capability |
| `Cross-Origin-Opener-Policy` | `same-origin` | Isolates the browsing context |

The CSP includes `script-src 'self' 'unsafe-inline'`. The `'unsafe-inline'` is required by
one inline script: the `no-js` → `js` class swap in `<head>`, which must run synchronously
before first paint (see D10 in [decisions.md](decisions.md)). It can be tightened to a
hash-based `'sha256-...'` source; doing so requires the generator to compute the hash and
write it into the config at build time. Not currently implemented.

`frame-ancestors 'none'` replaces the older `X-Frame-Options` header.

## Verifying a deployment

```bash
# Every route returns 200
for p in / /about/ /now/; do
  curl -o /dev/null -s -w "%{http_code} $p\n" "https://www.alexsitzman.com$p"
done

# An unknown route returns a real 404, not a 200 with 404 content
curl -o /dev/null -s -w "%{http_code} (expect 404)\n" \
  "https://www.alexsitzman.com/definitely-not-a-page/"

# Security headers are present
curl -sI https://www.alexsitzman.com | grep -iE 'strict-transport|content-security|x-content-type'
```
