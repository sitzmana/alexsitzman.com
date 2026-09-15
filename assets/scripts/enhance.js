// Progressive enhancement only. The site is complete and readable without this file.
// Three jobs: reveal-on-scroll, a shadow under the masthead once scrolled, and
// marking the nav link for the section currently in view.

(() => {
  "use strict";

  const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)");

  function revealOnScroll() {
    const targets = document.querySelectorAll(".reveal");
    if (targets.length === 0) return;

    // Without IntersectionObserver, or with reduced motion, show everything now.
    if (reducedMotion.matches || !("IntersectionObserver" in window)) {
      targets.forEach((el) => el.classList.add("is-visible"));
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue;
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      },
      { rootMargin: "0px 0px -12% 0px", threshold: 0.05 }
    );

    targets.forEach((el) => observer.observe(el));

    // Anything already on screen at load should not wait for a scroll event.
    requestAnimationFrame(() => {
      targets.forEach((el) => {
        if (el.getBoundingClientRect().top < innerHeight) {
          el.classList.add("is-visible");
        }
      });
    });
  }

  function stickyMasthead() {
    const masthead = document.querySelector(".masthead");
    if (!masthead || !("IntersectionObserver" in window)) return;

    const sentinel = document.createElement("div");
    sentinel.setAttribute("aria-hidden", "true");
    masthead.before(sentinel);

    new IntersectionObserver(
      ([entry]) => masthead.classList.toggle("is-stuck", !entry.isIntersecting)
    ).observe(sentinel);
  }

  function activeSection() {
    const sections = document.querySelectorAll("[data-section]");
    const links = document.querySelectorAll(".nav__link");
    if (sections.length === 0 || links.length === 0) return;
    if (!("IntersectionObserver" in window)) return;

    // Only applies to the single-page section flow on the home page.
    const home = [...links].find((a) => new URL(a.href).pathname === "/");
    if (!home || location.pathname !== "/") return;

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            home.setAttribute("data-active-section", entry.target.dataset.section);
          }
        }
      },
      { rootMargin: "-45% 0px -45% 0px" }
    );

    sections.forEach((section) => observer.observe(section));
  }

  function run() {
    revealOnScroll();
    stickyMasthead();
    activeSection();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", run, { once: true });
  } else {
    run();
  }
})();
