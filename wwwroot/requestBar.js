(function () {
    "use strict";

    function toNodes(sel) {
        if (typeof sel === "string") return document.querySelectorAll(sel);
        if (sel instanceof Element) return [sel];
        if (sel && typeof sel.length === "number") return sel;
        return [];
    }
    function init(options = {}) {
        const {
            selector = ".request-bar",
            threshold = 0.25,
            rootMargin = "0px",
            once = true,
            className = "is-inview",
        } = options;

        const targets = toNodes(selector);
        if (!targets.length) return;

        const io = new IntersectionObserver((entries) => {
            for (const entry of entries) {
                const el = entry.target;
                if (entry.isIntersecting) {
                    el.classList.add(className);
                    if (once) io.unobserve(el);
                } else if (!once) {
                    el.classList.remove(className);
                }
            }
        }, { threshold, rootMargin });

        targets.forEach(el => io.observe(el));
    }

    window.requestBar = { init };
})();