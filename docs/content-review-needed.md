# Content review needed

Items carried over from the previous alexsitzman.com that need a human decision.
Nothing here was invented, changed, or removed silently — where the old site was
ambiguous or broken, the ambiguity is recorded rather than resolved by guessing.

---

## 1. Two different GitHub usernames appear on the old site

| Location on the old site | URL |
| --- | --- |
| Home page profile icon | `https://github.com/sitzmana` |
| About page profile icon | `https://github.com/sitzmana` |
| Footer "Thanks for visiting" | `https://github.com/sitzmaa` |
| DAS Driver project link | `https://github.com/sitzmaa/DASDriver` |
| About page heading | "Username: sitzmana" |

**Carried over as:** `sitzmana` for profile links (it appears most often and matches the
stated username), and the DAS Driver link left exactly as it was — `sitzmaa/DASDriver`.

**Action:** confirm which account is correct. If `sitzmaa` is wrong, fix the link in
`content/projects/030-das-driver.md`. If it is right, nothing to do.

---

## 2. The old site's logo linked to a misspelled domain

The header wordmark linked to `https://www.alexstzman.com` — missing the `i` in
"sitzman". That is a dead link on every page of the old site.

**Carried over as:** the wordmark now links to `/`, the site root.

**Action:** none required. Noting it because it may also be wrong elsewhere, such as in
a résumé or email signature.

---

## 3. "What I'm Listening To" was non-functional

The old About page had a Spotify section with previous/next buttons and no content
between them. It rendered as an empty region with two unlabelled controls.

**Carried over as:** not rebuilt. A Spotify integration needs a runtime API call, a
refresh-token store, and a server or scheduled job — all of which conflict with a static
site with no backend.

The old "Did you know" list contained the line *"The music section below uses the spotify
api to maintain current songs"*, which only makes sense alongside a working music section.
It was **not** carried over. The other three facts were.

**Action:** decide whether you want this back. If yes, the cheapest approach is a
scheduled GitHub Action that writes `content/now/listening.md` on a timer, so the site
stays static. Say the word and it can be built.

---

## 4. The GitHub repository list never loaded

The old About page showed "Latest Repositories" followed by a permanent
"Loading repositories..." message — a client-side fetch that did not complete.

**Carried over as:** not rebuilt, for the same reason as item 3.

**Action:** decide whether you want it. Same scheduled-Action approach would work, and
would additionally remove the loading flash and the rate-limit risk.

---

## 5. The greeting was clock-based

The old home page rendered "Good morning," / "Good afternoon," based on the visitor's
browser clock.

**Carried over as:** a static "Hello," set in `HeroSection.razor`.

A statically generated page cannot know the reader's local time, and baking the build
server's time into the HTML would show "Good morning" to someone reading at midnight.
The alternative — a script that rewrites the greeting after load — causes visible text
flicker on the largest heading on the page.

**Action:** if you want the time-based greeting back, it can be done without flicker by
rendering all three variants and selecting one with CSS before paint. Currently judged
not worth the complexity for the value.

---

## 6. Projects have no engineering detail

The old site gave each project a title, one description sentence, three tags, and a link.
The new design supports a full detail page per project — add a body below the front matter
in any file under `content/projects/` and a page appears at `/projects/<slug>/`, linked
from the card automatically.

The following fields are available and currently unset, because the information was not
on the old site and inventing it is not acceptable:

- `role:` — what you actually did (e.g. "Sole engineer", "Team of 3, owned networking")
- `period:` — when (e.g. "2023", "2021–2022")
- Body prose — the problem, the constraints, the decisions, what you would do differently

**Action:** this is the single highest-value content improvement available. Hiring
managers read the *why* and the *tradeoffs*, not the tag list. Three short paragraphs per
project would materially strengthen the site.

---

## 7. Skills were regrouped, not changed

The old site listed twelve skills in one flat row. They are now grouped into three
columns: Cloud & Platform, Operations & Delivery, and Languages.

All twelve were preserved exactly. A test (`Every_skill_from_the_previous_site_survived_the_regrouping`)
fails the build if any goes missing.

Only spelling was normalised: "Javascript" → "JavaScript".

**Action:** none required. Review the grouping if you disagree with the split.

---

## 8. The interests carousel repeated its contents

The old About page carousel showed the same three interests two to three times each in
its DOM, cycling through duplicates.

**Carried over as:** three interests, shown once each, in a plain responsive grid. A
carousel hides most of its content behind controls, needs keyboard and reduced-motion
handling, and adds motion for three short items that fit on screen together.

**Action:** none required.

---

## 9. Unverified but preserved as-is

Carried over verbatim; confirm they are still accurate:

- Job title: "Senior Technical Support Engineer at Microsoft Azure"
- Location: "Redmond, WA"
- Email: `alexander@sitzman.net`
- Certifications: CKA (CNCF), Certified Linux Systems Administrator (Linux Foundation)
  — neither had an issue date, expiry, or verification link on the old site. Both
  certifications expire; adding `links:` with a credential verification URL would
  strengthen them considerably.
- Current focus: "AKS Mastodon Server" — still current?
- "I rowed for 7 years in high school and college"
