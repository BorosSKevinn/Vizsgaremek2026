window.navSpyInit = function () {
    const header = document.querySelector('#main-header') || document.querySelector('header');
    const links = Array.from(document.querySelectorAll('.navbar .nav-link[href^="#"]'));
    if (!links.length) return;

    const sectionMap = new Map();
    links.forEach(link => {
        const href = link.getAttribute('href').trim();
        if (href.length > 1) {
            const sec = document.querySelector(href);
            if (sec) sectionMap.set(sec, link);
        }
    });
    if (!sectionMap.size) return;

    function setActive(link) {
        document.querySelectorAll('.navbar .nav-item').forEach(li => li.classList.remove('active'));
        if (link) {
            const li = link.closest('.nav-item');
            if (li) li.classList.add('active');
        }
    }

    let observer;
    function initObserver() {
        if (observer) observer.disconnect();
        const headerH = header ? header.getBoundingClientRect().height : 0;

        observer = new IntersectionObserver((entries) => {
            entries.forEach(e => {
                if (e.isIntersecting) setActive(sectionMap.get(e.target));
            });
        }, {
            root: null,
            threshold: 0.25,
            rootMargin: `-${headerH + 10}px 0px -60% 0px`
        });

        sectionMap.forEach((_, sec) => observer.observe(sec));
    }

    initObserver();
    window.addEventListener('resize', initObserver);

    links.forEach(link => {
        link.addEventListener('click', (ev) => {
            const href = link.getAttribute('href');
            const target = document.querySelector(href);
            if (!target) return;

            ev.preventDefault();
            const headerH = header ? header.getBoundingClientRect().height : 0;
            const top = target.getBoundingClientRect().top + window.scrollY - headerH - 8;
            window.scrollTo({ top, behavior: 'smooth' });
            setActive(link);

            const collapse = document.getElementById('navbar-menu');
            if (collapse) collapse.classList.remove('open');
        });
    });
};

window.scrollToId = (id) => { const el = document.getElementById(id); if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' }); };
