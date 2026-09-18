# Content

Everything on the site comes from this folder. **Add a file, get content** — no code changes.

## Rules

1. Every entry is one Markdown file with YAML front matter between `---` fences.
2. The file name becomes the URL slug. A numeric prefix (`020-`) controls order and is stripped from the slug, so `020-hiking.md` becomes `hiking`.
3. `draft: true` removes an entry from the build.
4. A typo in a front-matter key **fails the build** with the file name and line number. It never silently disappears.

## Folders

| Folder | What it produces | Key fields |
| --- | --- | --- |
| `site.yml` | Header, hero, footer, metadata | `name`, `title`, `location`, `email`, `social` |
| `pages/` | A route per file | `navLabel`, `sections`, `heading`, `eyebrow` |
| `projects/` | Hero index, matching project cards, detail pages for entries with bodies | `summary`, `tags`, `links`, `featured`, `visual`, `category` |
| `certifications/` | Full credential page and optional home highlight | `issuer`, `abbreviation`, `tags`, `featured`, `summary` |
| `now/` | Now-page entries | `group`, `status`, `summary`, `tags` |
| `skills/` | One column per file | `items` |
| `interests/` | "How I spend my time" cards | body text |
| `facts/` | "Did you know" lines | `title` is the line |
| `repositories/` | Public repository cards | `language`, `fork`, `summary`, `links` |
| `activity/` | Dated contribution calendar and accessible date/count list | `start`, `end`, `days`, `links` |
| `listening/` | One Spotify-linked record per file | `artist`, `links` |

## Adding things

**A project**

```markdown
---
title: My New Project
featured: true
visual: network
category: Networks
summary: One sentence for the card.
tags: [Kubernetes, Go]
links:
  - label: Source code
    url: https://github.com/sitzmana/example
    icon: github
---

Anything below the fence is the project's detail page. Omit it and the card
title opens the first existing `links` entry instead. With neither body nor links,
the card remains plain content.
```

The hero index and project cards use the same featured selection, or all projects
when none are featured. Adding, removing, ordering, or drafting a project updates
both. Index links follow the first page containing `sections: [projects]`; if
there is no such page, the hero omits the index instead of creating broken links.

**A page**

Drop `pages/040-uses.md` and `/uses/` exists. Give it a `navLabel` to put it in the nav.

```markdown
---
title: Uses
navLabel: Uses
order: 40
sections: [contact]
---

Prose goes here.
```

`sections` pulls in shared blocks. Valid keys: `hero`, `projects`,
`certifications`, `skills`, `now`, `interests`, `facts`, `connect`, `contact`,
`activity`, `repositories`, `listening`, `explorer`, `credential-highlight`.
An unknown key fails the build and lists the valid ones.
Repeating a section also fails, rather than producing duplicate element IDs.
Both `home` and `index` mean `/`; only one can exist.

**Long pages and project write-ups**

Use `##` headings for the main parts of a Markdown body and `###` for subsections.
The loader creates unique, focusable heading anchors and derives the contents
labels from the rendered heading text, including inline formatting. Do not add
a second list of headings to front matter.

Pages with at least three destinations get an **On this page** panel. It combines
top-level `##` headings with the page's declared sections, in reading order.
Section labels follow their titles in `site.yml`; the hero is omitted. Project
detail pages use their `##` headings. Short pages omit the panel.

```yaml
pageNavigation:
  contentsLabel: On this page
  backToTopLabel: Back to top
```

These optional site-wide settings also enable the footer's native return link.
When the object is present, both labels must be non-empty. Missing it disables
these navigation controls, not the heading anchors.

Heading IDs include the collection and entry slug to avoid clashes with sections
or other entries. For example, `## Decisions` in `projects/guide.md` becomes
`projects-guide--decisions`; repeated headings get numeric suffixes. The double
hyphen separates the entry slug from the heading, so similar names cannot
produce the same anchor. Within the
same Markdown body, `[Read the decisions](#decisions)` is rewritten automatically.
Cross-page links must use the complete generated fragment. Renaming a heading
changes its anchor, so update incoming links when changing published headings.

`pages/040-site.md` is the site's implementation tour. Like any other page, its
copy, route, and navigation label live entirely in its content file.

**Hero and section copy**

Edit `hero` and `sections` in `site.yml`. All editorial headings and introductions
are file-driven. A page can reuse any configured section without a code change.

```yaml
hero:
  greeting: Hello, I'm
  summary: A short introduction.
  sceneLabel: Pick a project. Go a layer deeper.
  sceneHint: Each card opens the work below.
  links:
    - label: Explore my work
      url: /#projects
sections:
  projects:
    title: Selected work
    lead: A short introduction to this collection.
  listening:
    title: The other kind of playlist.
    previousLabel: Previous records
    nextLabel: Next records
    note: Explain the source and snapshot date here.
```

Every section used by a page needs a lowercase entry under `site.yml`'s `sections`,
except `hero`, which uses its own configuration. Missing copy and duplicate YAML
keys fail with the file name. Hero links allow root-relative paths; entry and
profile links require HTTPS or `mailto:`. Every link needs a text label.

Sections may set `label` for a reader-friendly eyebrow instead of their registry
key. The credential highlight uses its section's `links` for labelled calls to
action; these links may be root-relative.

**Evidence explorer**

`pages/015-explore.md` includes `sections: [explorer, contact]`. All control labels,
empty/error messages, and helper text come from the top-level `explorer` object in
`site.yml`; every field is required. `resultsTemplate` must include `{shown}` and
`{total}`. Missing copy fails at `content/site.yml`.

There is no separate catalog to maintain. Projects, certifications, repositories,
and now entries contribute their actual titles, summaries, and tags. A repository's
declared language is also a topic. No topic aliases or proficiency ratings are
inferred. Empty tags fail the build.

Existing skill names link to Explorer only when the same topic is present in
the catalog, ignoring letter case. Adding an entry or changing its tags updates
both the catalog and those links. Source links follow the pages declaring the
relevant sections, not hard-coded route names. An unlisted project uses its detail
page or existing outbound link rather than pointing at a missing home-page card.

Views are bookmarkable, for example `/explore/?topic=Kubernetes&kind=project`.
`q` searches the content; `topic` narrows by a declared topic; `kind` accepts
`project`, `credential`, `repository`, or `now`. With scripts blocked, controls
remain disabled and the complete, linked catalog is still readable.

**Credential highlight**

Set `featured: true` on a credential file to include it in the home page's
`credential-highlight` section. Its title, issuer, summary, and tags still come
from that file. The full collection remains at the page containing `certifications`
(currently `/credentials/`). Recognition is not relabelled as a separate exam.

**Personal snapshots**

These collections are curated snapshots, not live integrations. Keep their
`sections.<key>.note` current when updating the data. Do not imply that a track
is currently playing, a repository is newly updated, or contributions measure
all engineering work.

A listening entry requires `title`, `artist`, and at least one labelled link;
the first link opens from its record. `sections.listening` requires both control
labels even though native scrolling also works without JavaScript. A repository
entry requires a labelled link; mark inherited projects with `fork: true`.

```yaml
title: Public contributions
start: 2026-01-01
end: 2026-09-17
days:
  - date: 2026-02-19
    count: 1
links:
  - label: View activity
    url: https://github.com/sitzmana
```

Activity periods are inclusive, date-only, and cover at most 366 calendar days.
Each nonzero day must occur once, lie within the period, and have a positive count.
Only omit zero-count days when the source actually establishes they were zero.
Unknown or unverified activity must not be filled in.

**Project artwork**

`visual: network`, `visual: stack`, or `visual: signal` selects original decorative
artwork. Omit `visual` for a text-only card. Unsupported names fail the build.
Artwork is an abstract illustration, not a factual architecture diagram.

**Images**

Use the folder form when an entry needs images:

```
projects/
  040-my-project/
    index.md
    diagram.avif
```

Reference it as `![Topology](diagram.avif)`. Files beside `index.md` are copied to
`/projects/my-project/`.
Page-folder assets instead follow the page route (`pages/040-uses/` becomes
`/uses/`); home-page assets are copied to `/`. Relative Markdown links and images
are resolved against their entry's output directory, so shared interests or now
items also work when rendered on different pages.

## Preview

```pwsh
dotnet run --project src/Portfolio.Generator -- --serve
```
