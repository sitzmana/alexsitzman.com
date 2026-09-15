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
| `projects/` | Project cards + a detail page when the file has a body | `summary`, `tags`, `links`, `featured` |
| `certifications/` | Certification cards | `issuer`, `abbreviation`, `tags` |
| `now/` | Now-page entries | `group`, `status`, `summary`, `tags` |
| `skills/` | One column per file | `items` |
| `interests/` | "How I spend my time" cards | body text |
| `facts/` | "Did you know" lines | `title` is the line |

## Adding things

**A project**

```markdown
---
title: My New Project
featured: true
summary: One sentence for the card.
tags: [Kubernetes, Go]
links:
  - label: Source code
    url: https://github.com/sitzmana/example
    icon: github
---

Anything below the fence is the project's detail page. Omit it and the card
simply does not link anywhere.
```

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

`sections` pulls in shared blocks. Valid keys: `hero`, `specialties`, `projects`,
`certifications`, `skills`, `now`, `interests`, `facts`, `connect`, `contact`.
An unknown key fails the build and lists the valid ones.

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

## Preview

```pwsh
dotnet run --project src/Portfolio.Generator -- --serve
```
