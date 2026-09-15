# Testing

```pwsh
dotnet test Portfolio.slnx
```

**54 tests, all passing.** Runtime ~370 ms.

## What is covered

| Suite | Tests | Covers |
| --- | --- | --- |
| `FrontMatterTests` | 5 | Fence parsing, BOM handling, missing and unterminated front matter |
| `SlugTests` | 10 | File name → URL slug, sort-prefix extraction |
| `ContentLoaderTests` | 17 | Ordering, drafts, typed binding, asset collection, every failure mode |
| `RealContentTests` | 9 | The actual `content/` tree |
| `SiteBuilderTests` | 13 | End to end: content files in, HTML out |

## The three layers

### 1. Unit — parsing rules

`FrontMatterTests` and `SlugTests` pin the rules a content author relies on:
`020-hiking.md` sorts at 20 and slugs to `hiking`; a UTF-8 BOM does not hide the opening
fence; front matter opened and never closed is an error rather than a file silently
treated as prose.

### 2. Loader — behaviour against temporary trees

`ContentLoaderTests` builds throwaway content folders and asserts on the result. Half the
suite is failure behaviour, because silent failure is the main risk of a file-driven site:

- `A_misspelled_front_matter_key_fails_the_build` — `featuerd: true` is an error, not a
  shrug that leaves the project off the home page
- `A_missing_title_fails_with_the_file_name`
- `Colliding_slugs_fail_rather_than_overwrite`
- `Unterminated_front_matter_names_the_offending_file`
- `A_content_tree_without_a_home_page_fails`
- `Raw_html_in_content_is_not_passed_through`

### 3. End to end — the real generator

`SiteBuilderTests` runs `SiteBuilder` against temporary content and asserts on the HTML on
disk. These are the tests that back the "add a file, get content" promise:

- `Adding_a_page_file_creates_a_route_and_a_nav_entry` — and checks the nav updated on
  *other* pages too, not just the new one
- `A_project_with_a_body_also_gets_its_own_page`
- `A_project_without_a_body_gets_no_page_and_no_link`
- `Draft_entries_stay_out_of_the_output`
- `Rebuilding_removes_files_whose_content_was_deleted`
- `Images_beside_a_content_file_are_copied_next_to_its_page`
- `An_unknown_section_key_fails_the_build_and_lists_the_valid_ones`
- `Every_page_carries_the_accessibility_and_metadata_scaffolding` — `lang`, skip link,
  `#main`, canonical, Open Graph, JSON-LD
- `Stylesheet_and_script_are_content_hashed_for_immutable_caching`

### The regression guard on real content

`RealContentTests` loads the repository's actual `content/` folder at class-init. If any
content file is malformed, **every test in the class fails** with the offending path.
This is deliberate: it makes CI the safety net for content edits, so a typo in a Markdown
file is caught in a pull request rather than after deployment.

It also asserts content-level invariants:

- `Every_skill_from_the_previous_site_survived_the_regrouping` — names all twelve skills
  from the old site and fails if any is dropped during a reorganisation
- `Every_link_is_absolute_https_or_a_mailto` — no `http://`, no relative outbound links
- `Every_link_has_a_label` — no icon-only links can be introduced
- `Every_listed_entry_has_a_summary_or_a_body` — no blank cards
- `Every_page_section_key_is_known`

## Browser verification

Run manually against a live preview, not in CI. Results are in
[accessibility.md](accessibility.md) and [performance.md](performance.md).

```pwsh
dotnet run --project src/Portfolio.Generator -- --serve --port 5173
```

| Check | Method | Result |
| --- | --- | --- |
| Horizontal overflow | `scrollWidth - clientWidth` at 320/375/390/768/1024/1440/1920 px × 3 routes | 0 everywhere |
| Colour contrast | Computed foreground vs. resolved background, all text nodes | 0 violations |
| Target size | Bounding box of every `a`/`button` at 390 px | 0 under 24×24 |
| Reduced motion | `emulateMedia({ reducedMotion: 'reduce' })` | 0/18 hidden, progress bar off |
| No JavaScript | Browser context with `javaScriptEnabled: false` | 0/18 hidden, 2,036 chars of text |
| Keyboard focus | Eight `Tab` presses, checking `outlineStyle`/`outlineWidth` | Logical order, all visible |
| File-driven routing | Added `content/pages/040-uses.md` with the watcher running | `/uses/` served, nav updated sitewide, no restart |

## CI

[.github/workflows/ci.yml](../.github/workflows/ci.yml) runs on every push and pull
request:

1. `dotnet restore --locked-mode` — fails if `packages.lock.json` does not match
2. `dotnet build -c Release`
3. `dotnet test -c Release`
4. Generate the site
5. Assert each expected output file exists and is non-empty
6. Enforce the 150 KB home-page payload budget

Warnings are errors (`TreatWarningsAsErrors`), so a build warning fails CI.

## Gaps

Stated rather than implied.

- **No component-level tests.** No bUnit. Components are covered indirectly through the
  end-to-end HTML assertions, which is thinner than testing render logic directly.
- **Browser checks are not automated.** They were run manually and recorded above. They do
  not run in CI, so a regression in contrast or overflow would not fail a pull request.
- **No visual regression baseline.** No screenshot comparison exists.
- **Chromium only.** No Firefox or WebKit run.
- **External links are not reachability-checked.** Tests assert the *shape* of a URL
  (`https://`, has a label), not that it returns 200. A link-checking job would need
  network access in CI and would be flaky against rate-limited hosts.
- **No alt-text enforcement** on author-supplied Markdown images.
