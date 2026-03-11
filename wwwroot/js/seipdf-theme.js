document.addEventListener("DOMContentLoaded", () => {

    const toggle = document.getElementById("themeToggle");
    if (!toggle) return;

    const saved = localStorage.getItem("theme") || "light";
    applyTheme(saved);

    toggle.addEventListener("click", () => {
        const next = document.body.classList.contains("dark") ? "light" : "dark";
        applyTheme(next);
    });

    function applyTheme(theme) {
        document.body.classList.toggle("dark", theme === "dark");
        localStorage.setItem("theme", theme);
        toggle.textContent = theme === "dark" ? "☀️" : "🌙";
    }
});
