# Current site audit

Read-only audit of `alexsitzman.com` as it existed before this rebuild, captured via the
public pages. No authentication was bypassed and no rate limits were exceeded.

**September 17, 2026 correction:** revisiting the rendered About page showed
working repositories, contributions, and twelve Spotify-linked tracks. The first
pass's loading/empty state was not proof that those features were permanently
broken. They have now been restored as dated file-backed snapshots.

## Stack

React single-page application, client-side routed, Poppins webfont, deployed on Vercel.
Accent colour `#2978b5`. A light/dark toggle was present in the header.

## Structure

| Route | Contents |
| --- | --- |
| `/` | Hero, Certifications, Projects, Skills, Contact |
| `/about` | Bio, GitHub panel, interests carousel, "Did you know", Spotify panel, Connect |
| `/now` | "Currently Working On", Contact |

Header: wordmark "AS.", nav (Home / About / Now), theme toggle.
Footer: a single link, "Thanks for visiting".

## Content captured

**Identity.** Alex Sitzman. "A Senior Technical Support Engineer at Microsoft Azure."
Redmond, WA. `alexander@sitzman.net`.

**Certifications.** Certified Kubernetes Administrator (Cloud Native Computing
Foundation); Certified Linux Systems Administrator (Linux Foundation). No issue dates,
expiry dates, or verification links.

**Projects.** Virtual Internet Project; Kubernetes Raspberry Pi Cluster; DAS Driver
Windows/NetBSD. Each had a title, one description sentence, three tags, and one link.

**Skills.** Twelve, in one flat row: Kubernetes, Azure, DevOps, Network Security, Go,
Python, AWS, Javascript, Git, CI/CD, C++, MicroServices.

**Now.** AKS Mastodon Server — "A personal mastodon server hosted on AKS" —
Kubernetes / Azure / Administration.

**About.** Two bio paragraphs. Three interests (guitar, hiking, home automation). Four
"Did you know" lines.

**Links.** GitHub `sitzmana`, LinkedIn `alexander-sitzman`, Dev.to `sitzmana`. Project
links to `blog.alexsitzman.com` and `github.com/sitzmaa/DASDriver`.

All of the above was carried over. Mapping in [content-inventory.md](content-inventory.md).

## Strengths

- Clear, honest positioning in the first heading. No inflated claims anywhere.
- Correct scope — three pages, no filler.
- A `/now` page, which is unusual and shows genuine engagement.
- Real projects with real technical substance.
- Consistent visual language and a working theme toggle.

## Weaknesses

**Broken or dead functionality**

1. The header wordmark linked to `https://www.alexstzman.com` — a typo, missing the `i`.
   Dead link on every page.
2. "Latest Repositories" showed a loading placeholder in the initial capture;
   the rendered revisit confirmed five repositories.
3. The Spotify panel was empty in the initial capture; the rendered revisit
   confirmed twelve tracks with artists and links.
4. One fact refers to live Spotify API updates. That claim does not apply to
   this rebuild's static snapshot and is not carried over.

**Inconsistency**

5. Two GitHub usernames appeared: `sitzmana` in profile links, `sitzmaa` in the footer and
   the DAS Driver repository URL.
6. The interests carousel repeated its three items two to three times in the DOM.

**Structural / accessibility**

7. React SPA: no meaningful HTML without JavaScript, and a blank frame before hydration.
8. No `robots.txt`, no `sitemap.xml`, no favicon, no Open Graph or Twitter card tags.
9. Carousel and theme-toggle controls relied on icons without text labels.
10. The theme toggle button's accessible name was "toggle theme" with an unlabelled image.
11. Skills were presented as an undifferentiated row of twelve, giving equal weight to
    "Kubernetes" and "Git".

**Content depth**

12. Projects stated *what* but never *why*, *how*, or *what role was played* — the
    information a hiring manager actually reads.
13. No content had dates, so nothing signalled recency.
14. The footer's only link was "Thanks for visiting", which gave a visitor reaching the
    bottom of the page nowhere useful to go.

**Presentation**

15. Time-based greeting derived from the visitor's clock — pleasant, but it makes the
    largest heading on the page non-deterministic.
16. Long single-column stacking with weak visual hierarchy between sections.

## What this rebuild changed

| Weakness | Resolution |
| --- | --- |
| 1 | Wordmark links to `/` |
| 2, 3, 4 | Repositories, activity, and music restored as dated local content; no live-API claim |
| 5 | Profile links use `sitzmana`; the DAS Driver URL is preserved exactly and flagged |
| 6 | Three interests, once each, in a responsive grid |
| 7 | Static HTML. Full content without JavaScript |
| 8 | `robots.txt`, `sitemap.xml`, SVG favicon, Open Graph, Twitter card, JSON-LD `Person` |
| 9, 10 | Every link is text-labelled. No icon-only controls exist |
| 11 | Skills grouped into three named clusters; all twelve preserved |
| 12 | `role`, `period`, and body prose are supported per project — unset, pending your input |
| 13 | `period` field available |
| 14 | Footer carries navigation, all profile links, and the colophon |
| 15 | Static greeting. Reasoning in [decisions.md](decisions.md) D-note under item 5 of the review doc |
| 16 | Numbered section frames, a fluid type scale, and a consistent vertical rhythm |
