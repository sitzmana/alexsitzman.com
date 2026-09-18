"""Browser regressions for the generated site; no browser code ships to visitors."""

import argparse
import json
import subprocess
import tempfile
import time
from pathlib import Path
from urllib.error import URLError
from urllib.request import urlopen
from urllib.parse import urlsplit, parse_qs
from xml.etree import ElementTree

from playwright.sync_api import sync_playwright


ROOT = Path(__file__).resolve().parents[2]


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def inspect_page(page, route, width):
    result = page.evaluate("""() => ({
        overflow: document.documentElement.scrollWidth - innerWidth,
        headings: document.querySelectorAll('h1').length,
        unnamed: [...document.querySelectorAll('a,button')].filter(
            el => !(el.textContent.trim() || el.getAttribute('aria-label'))).length,
        ids: [...document.querySelectorAll('[id]')].map(el => el.id),
        hidden: [...document.querySelectorAll('.reveal')].filter(
            el => getComputedStyle(el).opacity === '0').length,
        brokenContents: [...document.querySelectorAll('[data-page-jump], .colophon__top')].filter(link => {
            const target = document.getElementById(decodeURIComponent(new URL(link.href).hash.slice(1)));
            return !target || target.getAttribute('tabindex') !== '-1';
        }).map(link => link.getAttribute('href')),
        smallContents: [...document.querySelectorAll('[data-page-jump], .colophon__top')].filter(link => {
            const height = link.getBoundingClientRect().height;
            return height > 0 && height < 43.5;
        }).length
    })""")
    check(result["overflow"] <= 0, f"{route} at {width}: horizontal overflow")
    check(result["headings"] == 1, f"{route}: expected exactly one h1")
    check(result["unnamed"] == 0, f"{route}: unnamed interactive element")
    check(len(result["ids"]) == len(set(result["ids"])), f"{route}: duplicate IDs")
    check(not result["brokenContents"], f"{route}: missing or unfocusable contents destinations")
    check(result["smallContents"] == 0, f"{route}: reading navigation needs 44px touch targets")
    return result


def reveal_document(page):
    for target in page.locator(".reveal").all():
        target.evaluate("el => el.scrollIntoView({behavior: 'instant', block: 'center'})")
        page.wait_for_timeout(50)
    page.evaluate("scrollTo({top: 0, behavior: 'instant'})")
    page.wait_for_timeout(850)


def check_project_deck_hover(browser, base_url):
    checked = 0
    for width in (768, 959, 1024, 1440):
        for scripting in (True, False):
            context = browser.new_context(
                viewport={"width": width, "height": 1000}, java_script_enabled=scripting)
            page = context.new_page()
            page.goto(base_url + "/")
            for index, link in enumerate(page.locator("[data-project-jump]").all()):
                page.mouse.move(1, 1)
                link.scroll_into_view_if_needed()
                page.wait_for_timeout(700)
                box = link.bounding_box()
                label = f"Project {index + 1} at {width}, JS={scripting}"
                surface = link.locator(".project-deck__surface")
                visual = surface.bounding_box()
                check(visual["x"] >= box["x"] - 0.5 and visual["y"] >= box["y"] - 0.5
                      and visual["x"] + visual["width"] <= box["x"] + box["width"] + 0.5
                      and visual["y"] + visual["height"] <= box["y"] + box["height"] + 0.5,
                      f"{label}: the resting surface must fit inside its clickable frame")
                x, y = box["x"] + box["width"] / 2, box["y"] + box["height"] / 2
                page.mouse.move(x, y)
                samples = []
                for _ in range(24):
                    samples.append(link.evaluate("""(el, initial) => {
                        const rect = el.getBoundingClientRect();
                        const surface = el.querySelector('.project-deck__surface').getBoundingClientRect();
                        return {
                            moved: ['x', 'y', 'width', 'height'].some(
                                key => Math.abs(rect[key] - initial[key]) > 0.5),
                            hovered: el.matches(':hover'),
                            outside: surface.left < rect.left - 0.5 || surface.right > rect.right + 0.5 ||
                                surface.top < rect.top - 0.5 || surface.bottom > rect.bottom + 0.5
                        };
                    }""", box))
                    page.wait_for_timeout(35)
                check(all(not sample["moved"] and sample["hovered"] and not sample["outside"] for sample in samples),
                      f"{label}: animation must stay inside a stationary, continuously hovered frame")
                check(surface.evaluate("el => getComputedStyle(el).pointerEvents") == "none",
                      f"{label}: visual layers cannot steal pointer events")
                resting_target = link.get_attribute("href")
                for horizontal, vertical in ((0, 0), (0.5, 0), (1, 0), (1, 0.5),
                                             (1, 1), (0.5, 1), (0, 1), (0, 0.5)):
                    x = box["x"] + 12 + (box["width"] - 24) * horizontal
                    y = box["y"] + 12 + (box["height"] - 24) * vertical
                    page.mouse.move(x, y)
                    page.wait_for_timeout(40)
                    check(link.evaluate("""(el, point) =>
                        el.matches(':hover') &&
                        document.elementFromPoint(point.x, point.y)?.closest('[data-project-jump]') === el
                    """, {"x": x, "y": y}), f"{label}: edge or corner lost its link target")
                page.mouse.click(x, y)
                page.wait_for_timeout(1000)
                check(page.evaluate("location.hash") == resting_target[resting_target.index("#"):],
                      f"{label}: clicking an edge must open the correct project")
                checked += 1
            context.close()
    return checked


def generated_routes(base_url):
    with urlopen(base_url + "/sitemap.xml", timeout=10) as response:
        sitemap = ElementTree.fromstring(response.read())
    return [urlsplit(node.text).path for node in sitemap.findall(
        "s:url/s:loc", {"s": "http://www.sitemaps.org/schemas/sitemap/0.9"})]


def use_hosting_headers(context, pattern, headers):
    def apply_headers(route):
        response = route.fetch()
        route.fulfill(response=response, headers={**response.headers, **headers})
    context.route(pattern, apply_headers)


def check_page_navigation(browser, base_url, headers):
    checked = 0
    errors = []
    for width in (320, 1440):
        for mode in ("enhanced", "no-js", "blocked-script", "forced-colors"):
            context = browser.new_context(
                viewport={"width": width, "height": 960},
                reduced_motion="reduce", java_script_enabled=mode != "no-js",
                has_touch=width == 320, is_mobile=width == 320,
                forced_colors="active" if mode == "forced-colors" else "none",
            )
            for route in ("/", "/about/", "/site/"):
                use_hosting_headers(context, base_url + route, headers)
            if mode == "blocked-script":
                context.route("**/assets/enhance.*.js", lambda route: route.abort())
            page = context.new_page()
            page.on("pageerror", lambda error: errors.append(str(error)))
            for route in ("/", "/about/", "/site/"):
                label = f"{route} at {width}, {mode}"
                page.goto(base_url + route)
                links = page.locator("[data-page-jump]")
                check(links.count() >= 3, f"{label}: long pages need derived contents")
                targets = [links.first.get_attribute("href"), links.last.get_attribute("href")]
                for index, link in enumerate((links.first, links.last)):
                    if width == 320 and index == 0:
                        link.tap()
                    else:
                        link.focus()
                        page.keyboard.press("Enter")
                    page.wait_for_timeout(50)
                    check(urlsplit(page.url).fragment == targets[index][1:], f"{label}: native anchor navigation failed")
                    check(page.evaluate("document.activeElement.id") == targets[index][1:],
                          f"{label}: navigation must move focus to its destination")
                    target = page.locator(targets[index])
                    check(target.bounding_box()["y"] >= 0, f"{label}: the destination must be in view")
                    if mode in ("enhanced", "forced-colors"):
                        check(link.get_attribute("aria-current") == "location",
                              f"{label}: the selected destination must be announced")
                page.go_back()
                page.wait_for_timeout(50)
                check(urlsplit(page.url).fragment == targets[0][1:], f"{label}: Back must restore the previous section")
                if mode in ("enhanced", "forced-colors"):
                    check(links.first.get_attribute("aria-current") == "location", f"{label}: Back must update selection")
                page.go_forward()
                page.wait_for_timeout(50)
                check(urlsplit(page.url).fragment == targets[-1][1:], f"{label}: Forward must restore the next section")
                page.reload()
                check(page.locator(targets[-1]).is_visible(), f"{label}: a direct fragment must remain usable")
                inspect_page(page, route, width)
                top = page.locator(".colophon__top")
                top.focus()
                page.keyboard.press("Enter")
                page.wait_for_timeout(50)
                check(page.evaluate("document.activeElement.id") == "top", f"{label}: return link must focus the masthead")
                check(page.evaluate("scrollY") <= 1, f"{label}: return link must reach the top")
                if mode == "enhanced":
                    page.emulate_media(media="print")
                    check(not page.locator(".page-contents").is_visible(), "Print must omit contents controls")
                    check(not top.is_visible(), "Print must omit the return-to-top control")
                    check(page.locator(".prose h2").evaluate_all(
                        "els => els.every(el => getComputedStyle(el).display !== 'none')"),
                        "Print must retain article headings")
                    page.emulate_media(media="screen")
                checked += 1
            context.close()
    check(not errors, f"Reading navigation browser errors: {errors}")
    return checked


def check_explorer(browser, base_url, screenshots, headers):
    for width, height in ((320, 568), (375, 812), (1440, 960)):
        context = browser.new_context(viewport={"width": width, "height": height},
                                      reduced_motion="reduce", has_touch=width < 768, is_mobile=width < 768)
        use_hosting_headers(context, base_url + "/explore/**", headers)
        page = context.new_page()
        errors = []
        requests = []
        page.on("pageerror", lambda error: errors.append(str(error)))
        page.on("request", lambda request: requests.append(request.url))
        page.goto(base_url + "/explore/")
        query = page.locator("#evidence-query")
        topic = page.locator("#evidence-topic")
        visible = page.locator("[data-evidence]:visible")
        total = page.locator("[data-evidence]").count()
        check(total > 0 and visible.count() == total, "Explorer must initially expose every entry")
        check(query.is_enabled(), "Explorer controls must enable after initialization")
        destinations = page.locator(".evidence-card__link").evaluate_all("els => els.map(el => el.getAttribute('href'))")
        query.fill("CKS")
        page.wait_for_timeout(250)
        check(visible.count() == 1 and "Security Specialist" in visible.inner_text(), "Abbreviations must be searchable")
        check(query.evaluate("el => el === document.activeElement"), "Filtering cannot steal search focus")
        check(parse_qs(urlsplit(page.url).query).get("q") == ["CKS"], "Typing must update the shareable URL")
        query.press("Enter")
        check(visible.count() == 1, "Submitting search must work under the hosting form-action policy")
        query.fill("")
        topic.select_option("Kubernetes")
        expected = page.locator("[data-evidence]").evaluate_all(
            """els => els.filter(el => JSON.parse(el.dataset.topics).includes('Kubernetes')).length""")
        check(visible.count() == expected, "Topic filters must use actual content tags")
        project_filter = page.locator("button[data-kind=project]")
        if width < 768:
            project_filter.tap()
        else:
            project_filter.click()
        check(visible.count() > 0 and visible.evaluate_all("els => els.every(el => el.dataset.kind === 'project')"),
              "Evidence type and topic filters must combine")
        check(project_filter.get_attribute("aria-pressed") == "true", "Selected evidence type must be announced")
        selected_count = visible.count()
        page.go_back()
        check(visible.count() == expected, "Back must restore the previous filter view")
        page.go_forward()
        check(visible.count() == selected_count, "Forward must restore the selected evidence type")
        if screenshots:
            page.screenshot(path=str(screenshots / f"explore-filtered-{width}.png"), full_page=True)
        page.locator("[data-explorer-reset]").click()
        check(visible.count() == total and not urlsplit(page.url).query, "Reset must clear filters and their URL")
        query.fill('<img src=x onerror="window.injected=true">')
        check(visible.count() == 0 and page.locator("[data-explorer-empty]").is_visible(), "No matches need a useful empty state")
        check(page.evaluate("window.injected === undefined"), "Search input must never become markup")
        page.emulate_media(media="print")
        check(visible.count() == total, "Print must retain the complete evidence catalog")
        page.emulate_media(media="screen")
        page.goto(base_url + "/explore/?topic=unavailable&kind=unknown&q=GoMud")
        check(visible.count() == 1, "Valid search terms must survive invalid URL filters")
        check(page.locator("[data-explorer-notice]").inner_text()
              == page.locator("[data-explorer]").get_attribute("data-invalid-message"),
              "Discarded URL filters must be explained")
        context.grant_permissions(["clipboard-read", "clipboard-write"], origin=base_url)
        page.locator("[data-explorer-share]").click()
        page.wait_for_timeout(100)
        shared = page.evaluate("navigator.clipboard.readText()")
        check(parse_qs(urlsplit(shared).query) == {"q": ["GoMud"]}, "Sharing must copy the current validated view")
        page.evaluate("""() => Object.defineProperty(navigator, 'clipboard', {
            configurable: true, value: {writeText: () => Promise.reject(new DOMException('Denied', 'NotAllowedError'))}
        })""")
        page.locator("[data-explorer-share]").click()
        page.wait_for_timeout(100)
        check(page.locator("[data-share-url]").is_visible(), "Clipboard denial must expose a manual-copy link")
        check(page.locator("[data-share-url]").input_value() == shared, "Manual copy must preserve the same view")
        check(page.locator("[data-explorer-notice]").inner_text()
              == page.locator("[data-explorer]").get_attribute("data-copy-failed-message"),
              "Clipboard failure must not claim success")
        page.evaluate("""() => { history.replaceState = () => {
            throw new DOMException('Address updates blocked', 'SecurityError');
        }; }""")
        query.fill("Kubernetes")
        page.wait_for_timeout(250)
        check(page.locator("[data-explorer-notice]").inner_text()
              == page.locator("[data-explorer]").get_attribute("data-history-failed-message"),
              "Blocked history updates must be explained without breaking filtering")
        check(visible.count() > 0, "Local search must survive an address-bar failure")
        inspect_page(page, "/explore/", width)
        check(not errors, f"Explorer browser errors: {errors}")
        check(all(request.startswith(base_url + "/") for request in requests),
              "Explorer interactions cannot send third-party requests")
        if width == 1440:
            target_page = context.new_page()
            for destination in destinations:
                if not destination.startswith("/"):
                    continue
                response = target_page.goto(base_url + destination)
                if response is not None:
                    check(response.status == 200, f"Broken evidence destination: {destination}")
                check(urlsplit(target_page.url).path == urlsplit(destination).path,
                      f"Evidence navigation reached the wrong page: {destination}")
                fragment = urlsplit(destination).fragment
                if fragment:
                    check(target_page.locator("#" + fragment).count() == 1, f"Missing evidence anchor: {destination}")
            target_page.close()
        context.close()


def run_checks(browser, base_url, screenshots):
    hover_checks = check_project_deck_hover(browser, base_url)
    errors = []
    requests = set()
    global_headers = json.loads((ROOT / "static" / "staticwebapp.config.json").read_text())["globalHeaders"]
    routes = generated_routes(base_url)
    check(all(route in routes for route in ("/explore/", "/credentials/", "/site/")),
          "New pages must be discoverable in the sitemap")
    navigation_checks = check_page_navigation(browser, base_url, global_headers)
    check_explorer(browser, base_url, screenshots, global_headers)
    measurements = []
    for width, height in ((320, 568), (375, 812), (768, 960), (1024, 960), (1440, 960)):
        context = browser.new_context(viewport={"width": width, "height": height}, reduced_motion="reduce")
        page = context.new_page()
        page.on("pageerror", lambda error: errors.append(str(error)))
        page.on("request", lambda request: requests.add(request.url))
        for route in routes:
            response = page.goto(base_url + route, wait_until="networkidle")
            check(response.status == 200, f"{route}: HTTP {response.status}")
            result = inspect_page(page, route, width)
            check(result["hidden"] == 0, f"{route}: hidden reduced-motion content")
            check(page.locator(".scroll-progress").evaluate("el => getComputedStyle(el).display") == "none",
                  "Reduced motion must disable scroll progress")
            measurements.append({"route": route, "width": width, **result})
            if screenshots and width in (320, 1440):
                name = route.strip("/").replace("/", "-") or "home"
                page.screenshot(path=str(screenshots / f"{name}-{width}.png"), full_page=True)
                if route == "/about/":
                    page.locator("#listening").screenshot(path=str(screenshots / f"listening-{width}.png"))
                    check(page.locator(".record__sleeve").evaluate_all(
                        "els => els.every(el => el.scrollHeight <= el.clientHeight + 1)"),
                        "Record sleeve copy must fit without covering the full track title")
        response = page.goto(base_url + "/not-a-real-page/")
        check(response.status == 404, "Missing routes must return HTTP 404")
        context.close()

    for width in (320, 1440):
        for mode in ("no-js", "blocked-script", "no-observer", "forced-colors"):
            context = browser.new_context(
                viewport={"width": width, "height": 960},
                java_script_enabled=mode != "no-js",
                forced_colors="active" if mode == "forced-colors" else "none",
            )
            if mode == "blocked-script":
                context.route("**/assets/enhance.*.js", lambda route: route.abort())
            if mode == "no-observer":
                context.add_init_script("delete window.IntersectionObserver")
            page = context.new_page()
            page.on("pageerror", lambda error: errors.append(str(error)))
            for route in routes:
                page.goto(base_url + route)
                if mode == "forced-colors":
                    reveal_document(page)
                result = inspect_page(page, route, width)
                check(result["hidden"] == 0, f"{mode}: content hidden on {route} at {width}")
                if screenshots and mode == "no-js":
                    name = route.strip("/").replace("/", "-") or "home"
                    page.screenshot(path=str(screenshots / f"{name}-no-js-{width}.png"), full_page=True)
                if route == "/":
                    jump = page.locator("[data-project-jump]").first
                    destination = jump.get_attribute("href").split("#")[1]
                    jump.click()
                    page.wait_for_timeout(1000)
                    check(page.evaluate("location.hash") == "#" + destination, f"{mode}: project link must work")
                    check(page.locator("#" + destination).evaluate("el => getComputedStyle(el).opacity") == "1",
                          f"{mode}: project destination must be visible")
                if route == "/about/":
                    shelf = page.locator(".record-shelf")
                    shelf.focus()
                    for _ in range(8):
                        page.keyboard.press("ArrowRight")
                    page.wait_for_timeout(500)
                    check(shelf.evaluate("el => el.scrollLeft") > 0, f"{mode}: record shelf must scroll natively")
                    check(page.locator(".record__link").count() == 12, f"{mode}: records must not be duplicated")
                if route == "/explore/":
                    check(page.locator("[data-evidence]:visible").count() == page.locator("[data-evidence]").count(),
                          f"{mode}: all evidence must remain available")
                    check(page.locator("#evidence-query").is_disabled() == (mode in ("no-js", "blocked-script")),
                          f"{mode}: controls must honestly indicate enhancement availability")
            if mode == "forced-colors":
                page.goto(base_url + "/")
                check(page.locator(".hero__name").evaluate("el => getComputedStyle(el).color") != "rgba(0, 0, 0, 0)",
                      "Forced colors must leave the hero name readable")
            context.close()

    context = browser.new_context(viewport={"width": 1440, "height": 960})
    use_hosting_headers(context, base_url + "/", global_headers)
    context.add_init_script("""(() => {
        window.animationRequests = 0;
        window.layoutShift = 0;
        new PerformanceObserver(list => {
            for (const entry of list.getEntries()) {
                if (!entry.hadRecentInput) window.layoutShift += entry.value;
            }
        }).observe({type: 'layout-shift', buffered: true});
        const request = window.requestAnimationFrame;
        window.requestAnimationFrame = callback => {
            window.animationRequests++;
            return request.call(window, callback);
        };
    })()""")
    page = context.new_page()
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.goto(base_url + "/")
    page.wait_for_timeout(300)
    check(page.evaluate("layoutShift") < 0.01, "Enhancement initialization must not shift the layout")
    check(page.locator(".hero__title").evaluate("el => getComputedStyle(el).opacity") == "1",
          "Hero must be visible at first paint")
    check(page.locator(".hero .is-pending").count() == 0, "Hero cannot wait for a reveal")
    if screenshots:
        link = page.locator("[data-project-jump]").first
        box = link.bounding_box()
        page.mouse.move(box["x"] + 12, box["y"] + 12)
        page.wait_for_timeout(750)
        page.locator(".hero").screenshot(path=str(screenshots / "hero-edge-hover-1440.png"))
        page.mouse.move(1, 1)
        page.wait_for_timeout(700)
    page.keyboard.press("Tab")
    check(page.locator(".skip-link").evaluate("el => el === document.activeElement"), "Skip link must be first")
    page.keyboard.press("Enter")
    check(page.evaluate("document.activeElement.id") == "main", "Skip link must move keyboard focus")
    for jump in page.locator("[data-project-jump]").all():
        destination = jump.get_attribute("href").split("#")[1]
        jump.focus()
        page.keyboard.press("Enter")
        page.wait_for_timeout(1000)
        check(page.evaluate("document.activeElement.id") == destination,
              "Project navigation must move focus to the corresponding project")
        check(jump.get_attribute("aria-current") == "location", "Current project link must be identified")
        check(page.locator("#" + destination).bounding_box()["y"] >= page.locator(".masthead").bounding_box()["height"],
              "The project destination must clear the sticky header")
    contents_link = page.locator("[data-page-jump]").first
    contents_link.focus()
    page.keyboard.press("Enter")
    page.wait_for_timeout(1000)
    section_id = contents_link.get_attribute("href")[1:]
    check(page.evaluate("document.activeElement.id") == section_id, "Contents must move focus with motion enabled")
    check(page.locator("#" + section_id + " .is-pending").count() == 0,
          "A selected section must reveal its content immediately")
    card = page.locator("[data-depth]").first
    card.scroll_into_view_if_needed()
    page.wait_for_timeout(750)
    box = card.bounding_box()
    surface = card.locator(".depth-surface")
    resting = surface.evaluate("el => getComputedStyle(el).transform")
    page.mouse.move(box["x"] + box["width"] * 0.8, box["y"] + box["height"] * 0.4)
    page.wait_for_timeout(300)
    check(card.evaluate("el => !!el.style.getPropertyValue('--pointer-x')"), "Fine pointer must activate project depth")
    check(surface.evaluate("el => getComputedStyle(el).transform") != resting, "Project geometry must respond to pointer")
    idle_requests = page.evaluate("animationRequests")
    page.wait_for_timeout(350)
    check(page.evaluate("animationRequests") == idle_requests, "Project depth must not run an idle animation loop")
    if screenshots:
        page.screenshot(path=str(screenshots / "project-depth-1440.png"))
    page.mouse.move(5, 5)
    check(card.evaluate("el => !el.style.getPropertyValue('--pointer-x')"), "Leaving a card must reset depth")
    page.mouse.move(box["x"] + box["width"] * 0.8, box["y"] + box["height"] * 0.4)
    page.wait_for_timeout(100)
    page.emulate_media(reduced_motion="reduce")
    page.wait_for_timeout(50)
    check(card.evaluate("el => !el.style.getPropertyValue('--pointer-x')"), "Live reduced-motion change must reset depth")
    check(surface.evaluate("el => getComputedStyle(el).transitionDuration") == "0s", "Reduced motion cannot animate cards")
    check(inspect_page(page, "/", 1440)["hidden"] == 0, "Live reduced-motion change must reveal content")
    page.emulate_media(reduced_motion="no-preference")
    page.reload()
    reveal_document(page)
    check(inspect_page(page, "/", 1440)["hidden"] == 0, "Scrolling must reveal all content")
    if screenshots:
        page.screenshot(path=str(screenshots / "home-motion-1440.png"), full_page=True)
    page.emulate_media(media="print")
    check(inspect_page(page, "/", 1440)["hidden"] == 0, "Print must reveal all content")
    page.emulate_media(media="screen", reduced_motion="reduce")
    page.goto(base_url + "/about/")
    shelf = page.locator(".record-shelf")
    previous = page.locator("[data-shelf-prev]")
    next_record = page.locator("[data-shelf-next]")
    check(previous.is_disabled(), "Previous records must be disabled at the start")
    next_record.click()
    check(shelf.evaluate("el => el.scrollLeft") > 0, "Next records must browse the shelf")
    check(not previous.is_disabled(), "Previous records must become available")
    previous.click()
    check(shelf.evaluate("el => el.scrollLeft") == 0, "Previous records must return to the start")
    shelf.evaluate("el => el.scrollTo({left: el.scrollWidth, behavior: 'instant'})")
    page.wait_for_timeout(50)
    check(next_record.is_disabled(), "Next records must be disabled at the end")
    shelf.evaluate("el => el.scrollTo({left: 0, behavior: 'instant'})")
    page.wait_for_timeout(50)
    page.emulate_media(reduced_motion="no-preference")
    next_record.click()
    page.wait_for_timeout(60)
    page.emulate_media(reduced_motion="reduce")
    page.wait_for_timeout(50)
    stopped = shelf.evaluate("el => el.scrollLeft")
    page.wait_for_timeout(200)
    check(shelf.evaluate("el => el.scrollLeft") == stopped, "Live reduced motion must stop shelf animation")
    page.wait_for_timeout(300)
    idle_requests = page.evaluate("animationRequests")
    page.wait_for_timeout(350)
    check(page.evaluate("animationRequests") == idle_requests, "The record shelf must not autoplay or poll")
    page.emulate_media(media="print")
    check(shelf.evaluate("el => el.scrollWidth <= el.clientWidth"), "All records must fit the printed page")
    context.close()

    context = browser.new_context(viewport={"width": 320, "height": 960}, is_mobile=True, has_touch=True)
    page = context.new_page()
    page.goto(base_url + "/")
    check(page.locator("[data-depth].is-interactive").count() == 0,
          "Touch must not register pointer tilt")
    jump = page.locator("[data-project-jump]").first
    destination = jump.get_attribute("href").split("#")[1]
    jump.tap()
    page.wait_for_timeout(1000)
    check(page.evaluate("location.hash") == "#" + destination, "Touch must navigate to a real project")
    check(page.locator("#" + destination).evaluate("el => getComputedStyle(el).opacity") == "1",
          "Touch navigation must reveal the project")
    page.goto(base_url + "/about/")
    page.locator("[data-shelf-next]").tap()
    page.wait_for_timeout(750)
    check(page.locator(".record-shelf").evaluate("el => el.scrollLeft") > 0, "Touch controls must browse the records")
    shelf = page.locator(".record-shelf")
    shelf.evaluate("el => el.scrollTo({left: 0, behavior: 'instant'})")
    shelf.scroll_into_view_if_needed()
    box = shelf.bounding_box()
    client = context.new_cdp_session(page)
    x, y = box["x"] + box["width"] * 0.85, box["y"] + box["height"] * 0.5
    client.send("Input.dispatchTouchEvent", {"type": "touchStart", "touchPoints": [{"x": x, "y": y}]})
    for step in range(1, 7):
        client.send("Input.dispatchTouchEvent", {"type": "touchMove", "touchPoints": [{"x": x - step * 30, "y": y}]})
        page.wait_for_timeout(20)
    client.send("Input.dispatchTouchEvent", {"type": "touchEnd", "touchPoints": []})
    page.wait_for_timeout(750)
    check(shelf.evaluate("el => el.scrollLeft") > 0, "Native touch swiping must browse the shelf")
    client.detach()
    context.close()

    context = browser.new_context(viewport={"width": 320, "height": 960}, reduced_motion="reduce")
    page = context.new_page()
    page.goto(base_url + "/")
    page.locator(".nav__list").evaluate("""el => {
        for (let i = 0; i < 6; i++) {
            const li = document.createElement('li');
            li.innerHTML = '<a class="nav__link" href="/about/">Additional portfolio page</a>';
            el.append(li);
        }
    }""")
    page.locator(".tag").first.evaluate("el => el.textContent = 'VeryLongTechnicalTerm'.repeat(8)")
    check(inspect_page(page, "/", 320)["overflow"] == 0, "Growing content must still fit a phone")
    context.close()
    check(not errors, f"Browser errors: {errors}")
    check(all(url.startswith(base_url + "/") for url in requests), f"Unexpected external request: {requests}")
    print(json.dumps({"responsive_checks": len(measurements), "fallback_checks": len(routes) * 8,
                      "hover_checks": hover_checks,
                      "reading_navigation_checks": navigation_checks,
                      "browser_errors": errors, "third_party_requests": 0}, indent=2))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", help="Use an existing preview instead of starting one")
    parser.add_argument("--browser-channel", help="For example, msedge for an installed local browser")
    parser.add_argument("--screenshots", type=Path)
    args = parser.parse_args()
    if args.screenshots:
        args.screenshots.mkdir(parents=True, exist_ok=True)
    base_url = (args.base_url or "http://localhost:5173").rstrip("/")
    server = None
    with tempfile.TemporaryFile(mode="w+") as log:
        try:
            if not args.base_url:
                import socket
                with socket.socket() as listener:
                    listener.bind(("127.0.0.1", 0))
                    port = listener.getsockname()[1]
                base_url = f"http://localhost:{port}"
                assembly = ROOT / "src" / "Portfolio.Generator" / "bin" / "Release" / "net10.0" / "Portfolio.Generator.dll"
                server = subprocess.Popen(
                    ["dotnet", str(assembly), "--serve", "--port", str(port)],
                    cwd=ROOT, stdout=log, stderr=subprocess.STDOUT,
                )
                for _ in range(100):
                    if server.poll() is not None:
                        log.seek(0)
                        raise RuntimeError("Preview failed:\n" + log.read())
                    try:
                        with urlopen(base_url, timeout=1):
                            break
                    except URLError:
                        time.sleep(0.1)
                else:
                    raise RuntimeError("Preview did not become responsive")
            with sync_playwright() as playwright:
                browser = playwright.chromium.launch(channel=args.browser_channel, headless=True)
                try:
                    run_checks(browser, base_url, args.screenshots)
                finally:
                    browser.close()
        finally:
            if server:
                server.terminate()
                try:
                    server.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    server.kill()
                    server.wait()


if __name__ == "__main__":
    main()
