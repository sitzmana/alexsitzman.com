(() => {
  "use strict";

  const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)");
  const finePointer = matchMedia("(hover: hover) and (pointer: fine)");

  function revealOnScroll() {
    if (reducedMotion.matches || !("IntersectionObserver" in window)) return;

    const targets = [...document.querySelectorAll(".reveal")];
    const observer = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue;
        entry.target.classList.remove("is-pending");
        observer.unobserve(entry.target);
      }
    }, { threshold: 0 });

    function revealAll() {
      observer.disconnect();
      targets.forEach((el) => el.classList.remove("is-pending"));
    }

    try {
      // Read all geometry before changing styles; never hide first-paint content.
      const offscreen = targets.filter((el) => el.getBoundingClientRect().top >= innerHeight);
      offscreen.forEach((el) => {
        observer.observe(el);
        el.classList.add("is-pending");
      });
    } catch (error) {
      revealAll();
      console.warn("Scroll enhancement unavailable; content remains visible.", error);
    }

    reducedMotion.addEventListener("change", revealAll, { once: true });
    window.addEventListener("beforeprint", revealAll, { once: true });
    window.addEventListener("pageshow", (event) => {
      if (event.persisted) revealAll();
    });
  }

  function stickyMasthead() {
    const masthead = document.querySelector(".masthead");
    if (!masthead || !("IntersectionObserver" in window)) return;

    const sentinel = document.createElement("div");
    sentinel.setAttribute("aria-hidden", "true");
    masthead.before(sentinel);
    new IntersectionObserver(([entry]) => {
      masthead.classList.toggle("is-stuck", !entry.isIntersecting);
    }).observe(sentinel);
  }

  function depthCards() {
    const cards = [...document.querySelectorAll("[data-depth]")];
    if (!cards.length || !CSS.supports("transform-style", "preserve-3d")) return;

    let listeners;
    let frame = 0;
    let pointer;
    let active;

    function reset() {
      cancelAnimationFrame(frame);
      frame = 0;
      active?.style.removeProperty("--pointer-x");
      active?.style.removeProperty("--pointer-y");
      active = null;
    }

    function render() {
      frame = 0;
      if (document.hidden || !pointer || !active) return;
      // Measure the stationary frame, not the tilted surface, to avoid feedback jitter.
      const rect = active.getBoundingClientRect();
      if (!rect.width || !rect.height) return;
      const x = Math.max(-1, Math.min(1, (pointer.x - rect.left) / rect.width * 2 - 1));
      const y = Math.max(-1, Math.min(1, (pointer.y - rect.top) / rect.height * 2 - 1));
      active.style.setProperty("--pointer-x", x.toFixed(3));
      active.style.setProperty("--pointer-y", (-y).toFixed(3));
    }

    function configure() {
      listeners?.abort();
      reset();
      const enabled = finePointer.matches && !reducedMotion.matches;
      cards.forEach((card) => card.classList.toggle("is-interactive", enabled));
      if (!enabled) return;
      listeners = new AbortController();
      const options = { signal: listeners.signal, passive: true };
      cards.forEach((card) => {
        card.addEventListener("pointermove", (event) => {
          if (event.pointerType !== "mouse" && event.pointerType !== "pen") return;
          if (active !== card) reset();
          active = card;
          pointer = { x: event.clientX, y: event.clientY };
          if (!frame) frame = requestAnimationFrame(render);
        }, options);
        card.addEventListener("pointerleave", reset, options);
        card.addEventListener("pointercancel", reset, options);
      });
    }
    reducedMotion.addEventListener("change", configure);
    finePointer.addEventListener("change", configure);
    document.addEventListener("visibilitychange", reset);
    window.addEventListener("blur", reset);
    window.addEventListener("pagehide", reset);
    window.addEventListener("scroll", reset, { passive: true });
    configure();
  }

  function anchorNavigation() {
    const links = [...document.querySelectorAll("[data-project-jump], [data-page-jump]")];
    function update() {
      links.forEach((link) => {
        const url = new URL(link.href);
        const selected = url.pathname === location.pathname && url.hash === location.hash;
        if (selected) {
          link.setAttribute("aria-current", "location");
          const target = document.getElementById(decodeURIComponent(url.hash.slice(1)));
          target?.classList.remove("is-pending");
          target?.querySelectorAll(".is-pending").forEach((element) => element.classList.remove("is-pending"));
        } else {
          link.removeAttribute("aria-current");
        }
      });
    }
    window.addEventListener("hashchange", update);
    update();
  }

  function recordShelves() {
    document.querySelectorAll("[data-shelf]").forEach((section) => {
      const shelf = section.querySelector(".record-shelf");
      const controls = section.querySelector(".shelf-controls");
      const previous = section.querySelector("[data-shelf-prev]");
      const next = section.querySelector("[data-shelf-next]");
      if (!shelf || !controls || !previous || !next) return;
      function update() {
        const max = shelf.scrollWidth - shelf.clientWidth;
        controls.hidden = max <= 1;
        previous.disabled = shelf.scrollLeft <= 1;
        next.disabled = shelf.scrollLeft >= max - 1;
      }
      function move(direction) {
        shelf.scrollBy({
          left: direction * shelf.clientWidth * 0.85,
          behavior: reducedMotion.matches ? "instant" : "smooth",
        });
      }
      previous.addEventListener("click", () => move(-1));
      next.addEventListener("click", () => move(1));
      shelf.addEventListener("scroll", update, { passive: true });
      window.addEventListener("resize", update, { passive: true });
      reducedMotion.addEventListener("change", () => {
        if (reducedMotion.matches) shelf.scrollTo({ left: shelf.scrollLeft, behavior: "instant" });
      });
      update();
    });
  }

  function evidenceExplorer() {
    const root = document.querySelector("[data-explorer]");
    if (!root) return;
    const form = root.querySelector("form");
    const controls = root.querySelector("fieldset");
    const query = root.querySelector("[name=q]");
    const topic = root.querySelector("[name=topic]");
    const kinds = [...root.querySelectorAll("button[data-kind]")];
    const status = root.querySelector("[data-explorer-status]");
    const notice = root.querySelector("[data-explorer-notice]");
    const empty = root.querySelector("[data-explorer-empty]");
    const shareField = root.querySelector(".explorer__share-field");
    const shareUrl = root.querySelector("[data-share-url]");
    const normalize = (text) => text.normalize("NFKD").replace(/\p{M}/gu, "").toLowerCase();
    const entries = [...root.querySelectorAll("[data-evidence]")].map((element) => ({
      element,
      kind: element.dataset.kind,
      text: normalize(element.dataset.search),
      topics: JSON.parse(element.dataset.topics).map(normalize),
    }));
    let selectedKind = "";
    let urlTimer = 0;

    function viewUrl() {
      const url = new URL(location.href);
      for (const [key, value] of [["q", query.value.trim()], ["topic", topic.value], ["kind", selectedKind]]) {
        if (value) url.searchParams.set(key, value);
        else url.searchParams.delete(key);
      }
      return url;
    }

    function writeUrl(mode) {
      clearTimeout(urlTimer);
      const url = viewUrl();
      if (url.href === location.href) return;
      try {
        history[mode === "push" ? "pushState" : "replaceState"](null, "", url);
      } catch (error) {
        if (!(error instanceof DOMException)) throw error;
        notice.textContent = root.dataset.historyFailedMessage;
        console.warn("Explorer filters could not update the address bar.", error);
      }
    }

    function render() {
      const terms = normalize(query.value).trim().split(/\s+/u).filter(Boolean);
      const selectedTopic = normalize(topic.value);
      const counts = new Map(kinds.map((button) => [button.dataset.kind, 0]));
      let shown = 0;
      for (const entry of entries) {
        const matches = terms.every((term) => entry.text.includes(term))
          && (!selectedTopic || entry.topics.includes(selectedTopic));
        if (matches) counts.set(entry.kind, counts.get(entry.kind) + 1);
        const visible = matches && (!selectedKind || entry.kind === selectedKind);
        if (!visible && entry.element.contains(document.activeElement)) query.focus({ preventScroll: true });
        entry.element.hidden = !visible;
        if (visible) shown++;
      }
      for (const button of kinds) {
        button.setAttribute("aria-pressed", String(button.dataset.kind === selectedKind));
        button.querySelector("[data-kind-count]").textContent = counts.get(button.dataset.kind);
      }
      status.textContent = root.dataset.resultsTemplate
        .replaceAll("{shown}", shown).replaceAll("{total}", entries.length);
      empty.hidden = shown > 0;
    }

    function readUrl() {
      clearTimeout(urlTimer);
      const params = new URL(location.href).searchParams;
      query.value = params.get("q") || "";
      const requestedTopic = params.get("topic") || "";
      topic.value = [...topic.options].find((option) => normalize(option.value) === normalize(requestedTopic))?.value || "";
      const requestedKind = params.get("kind") || "";
      selectedKind = kinds.some((button) => button.dataset.kind === requestedKind) ? requestedKind : "";
      notice.textContent = (requestedTopic && !topic.value) || (requestedKind && !selectedKind)
        ? root.dataset.invalidMessage : "";
      shareField.hidden = true;
      render();
    }

    function changed(mode) {
      notice.textContent = "";
      shareField.hidden = true;
      render();
      writeUrl(mode);
    }

    query.addEventListener("input", () => {
      clearTimeout(urlTimer);
      notice.textContent = "";
      shareField.hidden = true;
      render();
      urlTimer = setTimeout(() => writeUrl("replace"), 180);
    });
    topic.addEventListener("change", () => changed("push"));
    kinds.forEach((button) => button.addEventListener("click", () => {
      selectedKind = selectedKind === button.dataset.kind ? "" : button.dataset.kind;
      changed("push");
    }));
    form.addEventListener("submit", (event) => {
      event.preventDefault();
      changed("push");
    });
    root.querySelector("[data-explorer-reset]").addEventListener("click", () => {
      query.value = "";
      topic.value = "";
      selectedKind = "";
      changed("push");
    });

    function manualCopy(url) {
      shareUrl.value = url;
      shareField.hidden = false;
      notice.textContent = root.dataset.copyFailedMessage;
      shareUrl.focus();
      shareUrl.select();
    }
    root.querySelector("[data-explorer-share]").addEventListener("click", async () => {
      const url = viewUrl().href;
      if (!navigator.clipboard?.writeText) {
        manualCopy(url);
        return;
      }
      try {
        await navigator.clipboard.writeText(url);
        notice.textContent = root.dataset.copiedMessage;
        shareField.hidden = true;
      } catch (error) {
        manualCopy(url);
        console.warn("Explorer link could not be copied automatically.", error);
      }
    });
    window.addEventListener("popstate", readUrl);
    window.addEventListener("pagehide", () => clearTimeout(urlTimer));
    window.addEventListener("pageshow", (event) => { if (event.persisted) readUrl(); });
    readUrl();
    root.querySelector("[data-explorer-hint]").textContent = root.dataset.readyMessage;
    controls.disabled = false;
  }

  revealOnScroll();
  stickyMasthead();
  depthCards();
  anchorNavigation();
  recordShelves();
  evidenceExplorer();
})();
