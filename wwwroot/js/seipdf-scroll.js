document.addEventListener("DOMContentLoaded", () => {
    const sections = document.querySelectorAll(".reveal");

    const observer = new IntersectionObserver(
        entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add("visible");
                } else {
                    //permette la ri-animazione
                    entry.target.classList.remove("visible");
                }
            });
        },
        {
            threshold: 0.35
        }
    );

    sections.forEach(section => observer.observe(section));
});
