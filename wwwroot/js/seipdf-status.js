document.addEventListener("DOMContentLoaded", () => {

    fetch("/health")
        .then(r => {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        })
        .then(data => {
            updateBadge("pdfStatus", data.pdfService);
            updateBadge("zipStatus", data.zipService);
            updateBadge("mailStatus", data.mailService);
        })
        .catch(err => {
            console.error("Health fetch failed:", err);
            markError("pdfStatus");
            markError("zipStatus");
            markError("mailStatus");
        });


    function updateBadge(id, ok) {
        const el = document.getElementById(id);
        el.classList.remove("status-loading");

        if (ok === "OK") {
            el.classList.add("status-ok");
            el.textContent = el.textContent.split(":")[0] + ": OK";
        } else {
            el.classList.add("status-error");
            el.textContent = el.textContent.split(":")[0] + ": ERRORE";
        }
    }

    function markError(id) {
        const el = document.getElementById(id);
        el.classList.remove("status-loading");
        el.classList.add("status-error");
        el.textContent = "Sistema non raggiungibile";
    }
});
