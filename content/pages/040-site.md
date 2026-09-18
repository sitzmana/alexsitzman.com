---
title: Inside this site
navLabel: This site
order: 40
eyebrow: Under the hood
heading: A small site, by design.
metaDescription: How this portfolio turns Markdown and Razor components into static HTML, with local search, accessible navigation, and no browser framework.
sections: [contact]
---

This portfolio is written in C# and Razor, but your browser receives ordinary
HTML, CSS, and a small enhancement script. There is no .NET runtime to download,
no database, and no application server running the site.

## A file becomes a page

Content and presentation have different jobs. Project descriptions, credentials,
page text, and interface labels live in Markdown and YAML files. Razor components
decide how to display them.

1. **Load the content.** The build reads the files, checks required fields, and
   reports mistakes with the source filename.
2. **Render the pages.** C# passes that content into Razor components and renders
   them to HTML.
3. **Package the site.** The generator writes the pages, combines the styles, and
   gives the shared assets content-based filenames.
4. **Serve the files.** The published site needs static hosting, not a running
   .NET application.

Adding a page is a content change. A Markdown file with a navigation label creates
both its route and its place in the menu. This page follows that same path.

## What reaches your browser

The page arrives with its content already in the HTML. One shared stylesheet sets
the layout, and one deferred script adds the optional interactions.

- **System fonts**, rather than a font download.
- **Original SVG and CSS artwork**, rather than third-party image or logo requests.
- **Local search and filtering**, rather than a search service.
- **Links to GitHub and Spotify**, rather than embedded feeds or players.

The homepage has an enforced budget of less than **150 KiB for HTML, CSS, and
JavaScript combined, before compression**. That is a build limit, not a promised
loading time or a performance score.

## Useful without JavaScript

Navigation, project links, page contents, and the record shelf's native scrolling
remain available when scripts are disabled or blocked. The
[complete Explorer catalog](/explore/) stays readable; its search controls only
become usable once their enhancement is ready.

The script adds filters, copyable Explorer views, pointer depth, and explicit
record-shelf controls. None of those interactions is required to read the site.
Reduced-motion preferences remove the motion rather than hiding the content.

## Connections, not skill scores

The [Explorer](/explore/) connects projects, credentials, repositories, and current
work through the topics declared in their content files. Selecting Kubernetes
shows the entries tagged with Kubernetes; it does not calculate a proficiency
rating.

Filter selections live in the URL, so a focused view can be bookmarked or shared.
The results are already on the page, and filtering does not send the search
to a server.

## Snapshots, not live feeds

The repositories, contribution activity, and listening collection on
[About](/about/) are dated snapshots. Their dates describe when the information
was captured, not what is happening right now.

Updating those sections means updating their files. There is no background
polling, autoplay, embedded music player, or visitor analytics.

## The tradeoffs

Static files keep the production setup small, but content changes still require
a rebuild. Snapshots can become dated. The dark-only design and system font stack
also mean the site deliberately has fewer presentation options.

Those boundaries keep the focus on the work itself. Browse
[the projects](/#projects), [the credentials](/credentials/), or
[what is on the workbench](/now/).
