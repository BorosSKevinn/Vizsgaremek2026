(function () {
    window.navbarScrollInit = () => {
        const header = document.querySelector('header');
        const hero = document.querySelector('.hero')
            || document.querySelector('.main-banner')
            || document.querySelector('#top');
        if (!header || !hero) return;

        const apply = () => {

            const header = document.querySelector('header');
            const hero = document.querySelector('.hero')
                || document.querySelector('.main-banner')
                || document.querySelector('#top');
            if (!header || !hero) return;

            const heroBottom = hero.getBoundingClientRect().bottom;
            if (heroBottom <= 0 && !window.location.pathname.includes("/rental") && !window.location.pathname.includes("/about-page")) {
                header.classList.add('background-header');
            } else {
                header.classList.remove('background-header');
            }
        };

        apply();
        window.addEventListener('scroll', apply, { passive: true });
        window.addEventListener('resize', apply);
    };
})();