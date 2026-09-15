# Content inventory

Every piece of content from the previous site, and the file that now holds it.

## Site-wide → `content/site.yml`

| Old site | Field | Value |
| --- | --- | --- |
| Header wordmark | `initials` | `AS.` |
| Hero name | `name` | Alex Sitzman |
| Hero subheading | `title` | Senior Technical Support Engineer at Microsoft Azure |
| Hero location | `location` | Redmond, WA |
| Contact button | `email` | alexander@sitzman.net |
| Profile icons | `social` | GitHub, LinkedIn, Dev.to |

## Pages → `content/pages/`

| Old route | File | Sections |
| --- | --- | --- |
| `/` | `010-home.md` | hero, projects, certifications, skills, contact |
| `/about` | `020-about.md` | interests, facts, connect |
| `/now` | `030-now.md` | now, contact |

Section order on the home page was changed: projects now precede certifications. Work
demonstrates capability more directly than credentials do, and the strongest material
should come first.

## Projects → `content/projects/`

| Title | File | Tags | Link |
| --- | --- | --- | --- |
| Virtual Internet Project | `010-virtual-internet-project.md` | Python, Cybersecurity, Networks | blog.alexsitzman.com write-up |
| Kubernetes Raspberry Pi Cluster | `020-kubernetes-raspberry-pi-cluster.md` | Kubernetes, Network Administration, Security | blog.alexsitzman.com write-up |
| DAS Driver Windows/NetBSD | `030-das-driver.md` | C/C#, Hardware, Windows Driver Development | github.com/sitzmaa/DASDriver |

Descriptions carried over verbatim, with `mastodon` → `Mastodon` and sentence-final
punctuation added. Link labels changed from "live preview" / "source code" to "Write-up" /
"Source code", since the blog links go to articles rather than running demos.

## Certifications → `content/certifications/`

| Title | File | Issuer |
| --- | --- | --- |
| Certified Kubernetes Administrator | `010-certified-kubernetes-administrator.md` | Cloud Native Computing Foundation |
| Certified Linux Systems Administrator | `020-certified-linux-systems-administrator.md` | Linux Foundation |

`abbreviation` (`CKA`, `LFCS`) was added as a visual badge. It is decorative and
`aria-hidden`; the full name remains the heading.

## Now → `content/now/`

| Title | File |
| --- | --- |
| AKS Mastodon Server | `010-aks-mastodon-server.md` |

`status: In progress` was added. The old heading "Currently Working On" is preserved as
the entry's `group`, so it still appears as the section subheading.

## Skills → `content/skills/`

Twelve skills, regrouped into three files. Nothing added, nothing removed.

| File | Group | Items |
| --- | --- | --- |
| `010-cloud-platform.md` | Cloud & Platform | Azure, Kubernetes, AWS, MicroServices |
| `020-operations-delivery.md` | Operations & Delivery | DevOps, CI/CD, Git, Network Security |
| `030-languages.md` | Languages | Go, Python, JavaScript, C++ |

Only spelling changed: "Javascript" → "JavaScript".

`RealContentTests.Every_skill_from_the_previous_site_survived_the_regrouping` hard-codes
the original twelve and fails if any disappears.

## Interests → `content/interests/`

| Title | File |
| --- | --- |
| Playing Guitar | `010-playing-guitar.md` |
| Hiking | `020-hiking.md` |
| Home Automation Projects | `030-home-automation-projects.md` |

Text carried over verbatim with sentence-final punctuation added.

## Facts → `content/facts/`

| Fact | File |
| --- | --- |
| My go-to shell prompt is Starship. | `010-starship.md` |
| I enjoy cooking and trying new foods. | `020-cooking.md` |
| I rowed for 7 years in high school and college. | `030-rowing.md` |

**Not carried over:** *"The music section below uses the spotify api to maintain current
songs."* It describes a panel that was non-functional on the old site and is not rebuilt
here. See [content-review-needed.md](content-review-needed.md) item 3.

## Not carried over

| Feature | Reason |
| --- | --- |
| Spotify "What I'm Listening To" | Needs a runtime API call and token refresh. Was non-functional. |
| GitHub "Latest Repositories" | Needs a runtime API call. Never loaded. |
| GitHub contribution image | Third-party image service; a runtime dependency and a privacy surface |
| Light/dark toggle | One theme shipped deliberately — see [decisions.md](decisions.md) D7 |
| Interests carousel | Replaced by a grid — D-note in [current-site-audit.md](current-site-audit.md) item 6 |
| Time-based greeting | Cannot be determined at build time — see [content-review-needed.md](content-review-needed.md) item 5 |

## Added

Not present on the old site; none of it asserts a new fact about you.

| Addition | Source |
| --- | --- |
| `robots.txt`, `sitemap.xml` | Generated from the route list |
| SVG favicon | Initials from `site.yml` |
| Open Graph image | Generated SVG using `name` and `title` from `site.yml` |
| JSON-LD `Person` | `name`, `title`, `baseUrl`, and `social` from `site.yml` |
| 404 page | Static |
| Footer navigation and profiles | Same links, repeated at the end of the page |
| Section lead sentences | Editorial framing only — e.g. "Systems I have designed, built, and operated end to end" |

Section leads are the only newly written prose. They describe the section, not your
experience, and can be edited in the corresponding `Sections/*.razor` file.
