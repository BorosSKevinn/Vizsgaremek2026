window.startObserver = () => {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting && !entry.target.dataset.animated) {
                const delay = entry.target.dataset.delay || 400;
                setTimeout(() => {
                    entry.target.classList.add('animate');
                    entry.target.dataset.animated = true;
                    observer.unobserve(entry.target);
                }, parseInt(delay));
            }   
        });
    }, {
        root: null,
        rootMargin: "0px 0px -300px 0px",
        threshold: 0
    });

    const elements = document.querySelectorAll('.observe-animate');
    elements.forEach(el => observer.observe(el));
};