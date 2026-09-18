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
| `/` | `010-home.md` | hero, projects, credential-highlight, skills, contact |
| New `/explore/` | `015-explore.md` | explorer, contact |
| `/about` | `020-about.md` | activity, repositories, interests, listening, facts, connect |
| New `/credentials/` | `025-credentials.md` | certifications, contact |
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

Project summaries were tightened while preserving their source facts. Link labels
changed from "live preview" / "source code" to "Read the write-up" /
"Source code", since the blog links go to articles rather than running demos.

## Certifications → `content/certifications/`

| Title | File | Issuer |
| --- | --- | --- |
| Certified Kubernetes Administrator | `010-certified-kubernetes-administrator.md` | Cloud Native Computing Foundation |
| Certified Linux Systems Administrator | `020-certified-linux-systems-administrator.md` | Linux Foundation |
| Certified Kubernetes Security Specialist | `030-certified-kubernetes-security-specialist.md` | Cloud Native Computing Foundation |
| Certified Kubernetes Application Developer | `040-certified-kubernetes-application-developer.md` | Cloud Native Computing Foundation |
| Kubernetes and Cloud Native Associate | `050-kubernetes-and-cloud-native-associate.md` | Cloud Native Computing Foundation |
| Kubernetes and Cloud Native Security Associate | `060-kubernetes-and-cloud-native-security-associate.md` | Cloud Native Computing Foundation |
| Kubestronaut | `070-kubestronaut.md` | Cloud Native Computing Foundation |

`abbreviation` is displayed as a visual badge. It is decorative and
`aria-hidden`; the full name remains the heading.
The full list lives on the Credentials page; `featured: true` selects Kubestronaut
for the compact home highlight. Explorer references the same entries, not copies.

CKS, CKAD, KCNA, KCSA, and Kubestronaut were supplied by Alex on September 17, 2026.
He confirmed that the initially supplied "KCNS" meant KCSA. Kubestronaut is
labelled as recognition rather than another exam. No award dates, expiry dates,
credential IDs, or personal verification links were supplied or inferred.

Official names and program context were checked against CNCF's certification
catalog and Kubestronaut FAQ:
`https://www.cncf.io/training/certification/` and
`https://www.cncf.io/training/kubestronaut/kubestronaut-faq/`.

## Now → `content/now/`

| Title | File |
| --- | --- |
| Golden Kubestronaut | `010-golden-kubestronaut.md` |

On September 18, 2026, Alex confirmed that the AKS Mastodon Server project was
abandoned and replaced it with working towards Golden Kubestronaut. The entry
keeps `status: In progress` and the "Currently Working On" group. It is a learning
goal, not an earned credential; the link describes the CNCF program rather than
verifying a personal award. Explorer derives the same update from this file.

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

Wording was lightly edited for a conversational tone without changing the interests,
tools, locations, or frequency described in the original content.

## Facts → `content/facts/`

| Fact | File |
| --- | --- |
| My go-to shell prompt is Starship. | `010-starship.md` |
| I enjoy cooking and trying new foods. | `020-cooking.md` |
| I rowed for seven years in high school and college. | `030-rowing.md` |

**Not carried over:** *"The music section below uses the spotify api to maintain current
songs."* The restored shelf is a dated snapshot, not a runtime API integration.
See [content-review-needed.md](content-review-needed.md) item 3.

## Restored personal features

Verified against the rendered original About page and public GitHub data on
September 17, 2026.

| Feature | Content | Presentation |
| --- | --- | --- |
| Music | Twelve files in `content/listening/`, preserving titles, artists, Spotify links | Native record shelf, original CSS art, no embed or autoplay |
| Repositories | Five files in `content/repositories/` | Labelled links, language, explicit GoMud/Terraform fork labels |
| Contributions | `content/activity/010-github-2026.md` | Inclusive dated calendar and visible nonzero date/count list |

Section notes identify snapshots; none claim to be live data.

## Not carried over

| Feature | Reason |
| --- | --- |
| Live Spotify/GitHub fetching and external contribution image | Replaced by dated local content, including accessible contribution data |
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
| Section lead sentences | Plain-language introductions based on existing content |

Hero and section editorial copy lives in `content/site.yml`, not the section
components. Introductions, summaries, and control messages have been edited for
a friendly, professional tone, without adding claims about experience.

The About introduction has been shortened using the existing interests and bio.
No employers, dates, outcomes, or project details were added. Explorer's topics
and counts are derived from existing entries and never imply a proficiency score.
