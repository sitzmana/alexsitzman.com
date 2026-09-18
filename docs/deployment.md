# Deployment

The deployed artifact is a folder of static files. Azure Static Web Apps serves it.
No database, no compute, no storage account.

## Azure resources

| Resource | Tier | Cost |
| --- | --- | --- |
| Static Web App | Free | $0 |

The Free tier includes 100 GB bandwidth/month, custom domains, managed TLS certificates,
and pull-request preview environments. Current payload measurements and the enforced
budget are in [performance.md](performance.md).

No other Azure resource is required. If a resource group does not already exist, one is
needed, but it costs nothing.

## One-time setup

The portal-created app already has a GitHub repository secret named
`AZURE_STATIC_WEB_APPS_API_TOKEN_JOLLY_WATER_0E62D601E`. The upload and preview-cleanup
steps in `azure-static-web-apps-jolly-water-0e62d601e.yml` both use that exact name. No second generic
`AZURE_STATIC_WEB_APPS_API_TOKEN` secret, App Service publish profile, or Azure login
step is needed.

In Azure, **Settings > Configuration > Deployment configuration > Deployment
authorization policy** must be **Azure deployment token**. GitHub identity-token
authorization is a different configuration; this workflow does not request an
OIDC token. If Azure resets the deployment token, update the existing GitHub
secret's value rather than adding a token to any tracked file.

Keep the Azure-named workflow as the only deployment entry point. Its filename
is preserved from the portal, but its contents use the validated pipeline, not
the generated scaffold that tried to auto-build the repository root. The old
`deploy.yml` is removed. `ci.yml` remains reusable and generates the git-ignored
`dist/` artifact. Do not restore a parallel publisher or commit generated output.

### Creating a replacement app from the CLI

Skip these provisioning commands when using the existing portal-created app.
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

Store it using the repository secret name referenced by the deployment workflow:

```bash
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN_JOLLY_WATER_0E62D601E
```

This token is a deployment credential. It must never be committed, echoed in workflow
logs, or added to `staticwebapp.config.json`.
If a replacement app generates a differently named secret, update both upload and
preview-cleanup references together.

## How deployment works

The [Deploy workflow](../.github/workflows/azure-static-web-apps-jolly-water-0e62d601e.yml) runs on push to `main`
and on pull requests.

```
build job          restore (locked) -> build -> test -> generate -> upload artifact
                                    |
                                    v  only if the build job succeeded
deploy job         download artifact -> Azure/static-web-apps-deploy
```

Deployment is a separate job that `needs: build`. A failing test cannot deploy.

The artifact is pre-built, so the deploy step uses `app_location: dist`,
`skip_app_build: true`, and `output_location: ''`. The latter is an empty YAML
string, not the literal two-character string `"''"`. Without those settings,
Azure's Oryx build system would try to detect and rebuild the project in its own
container, which would either fail or silently produce different output.

`api_location: ''` explicitly leaves out a Functions API. There is no API to build
or upload, so no API-build flag is needed. The existing `staticwebapp.config.json`
is included inside the generated artifact.

**Permissions.** The workflow declares `contents: read` at the top level. Only the deploy
and teardown jobs add `pull-requests: write`, which the deploy action needs to comment
the preview URL on a PR.

**Pull requests** deploy to a temporary preview environment. The `close-preview` job tears
it down when the PR closes.

**Workflow regressions.** The .NET suite checks that only one workflow deploys,
that deployment depends on the reusable CI job and downloads its `site` artifact,
and that upload and preview cleanup share the same deployment-token reference
without an identity-token input. Secret values are never read by tests.

**Retrying a configuration fix.** Commit and push the corrected workflow to `main`
to trigger a fresh **Deploy** run. Inspect the build and deploy jobs separately;
a completed build does not imply that Azure accepted the deployment.

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
