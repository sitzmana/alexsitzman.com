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

## 3. Music restored; snapshot wording is intentional

The initial audit saw an empty region. A rendered revisit on September 17, 2026
showed twelve actual tracks with artists and Spotify links. The earlier claim
that this feature was non-functional was too broad.

**Carried over as:** twelve files in `content/listening/`, presented as a native
record shelf with original CSS artwork. Titles, artists, and links are preserved.
There is no Spotify embed, album-art download, token store, or runtime API.

The old fact saying the site uses the Spotify API to maintain current songs is
still omitted because it would be false for this implementation.

**Action:** update the files and dated section note when curating the shelf.
Automated refresh is not implemented or implied.

---

## 4. GitHub features restored as verified public snapshots

The initial audit saw a loading placeholder, not evidence that the feature never
worked. The September 17, 2026 rendered revisit and public GitHub data confirmed
five repositories. They now live in `content/repositories/`; GoMud and Terraform
are explicitly labelled as forks.

`content/activity/010-github-2026.md` records the verified public contribution
calendar from January 1 through September 17, 2026: one on February 19, four on
August 25, and one on September 15. No future dates or synthetic activity were added.
The calendar has a visible dated/count list as well as the visual grid.

**Action:** keep snapshot notes consistent when updating the data. Counts are
public GitHub activity, not a complete measure of work or productivity.

---

## 5. The greeting was clock-based

The old home page rendered "Good morning," / "Good afternoon," based on the visitor's
browser clock.

**Carried over as:** a static greeting, now editable as `hero.greeting` in `content/site.yml`.

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
- "I rowed for 7 years in high school and college"

## 10. Converter terminology needs clarification

The original DAS Driver summary called the device a "digital analog converter"
while describing voltage sampling. The shortened card now describes the PCI
driver and timestamped samples without choosing a conversion direction.
Confirm the device terminology before expanding this into a technical case study.

## 11. Additional credentials supplied by Alex

On September 17, 2026, Alex supplied CKS, CKAD, KCNA, KCSA, and Kubestronaut,
confirming that "KCNS" was a typo for KCSA. These are now listed alongside the
existing credentials. Kubestronaut is presented as recognition, not a separate exam.

Award dates, expiry dates, credential IDs, and personal verification links were
not supplied. Leave them unset until provided; do not use a generic program page
as if it verified Alex's individual credential.

## 12. Current learning goal confirmed

On September 18, 2026, Alex confirmed that the Mastodon server project was
abandoned and that he is working towards Golden Kubestronaut. Now and Explorer
show that goal as **In progress**; it is not added to the earned credentials.

An exam-by-exam plan, target date, and further completion details were not
supplied. Do not infer those from the existing credential list.
