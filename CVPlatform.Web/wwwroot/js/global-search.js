(function () {
    const input = document.getElementById("global-search");
    const box = document.getElementById("search-results");
    if (!input) return;

    let timer = null;

    input.addEventListener("input", () => {
        clearTimeout(timer);
        const term = input.value.trim();

        if (term.length < 2) {
            box.innerHTML = "";
            return;
        }

        timer = setTimeout(async () => {
            const response = await fetch(`/Search/Quick?term=${encodeURIComponent(term)}`);
            const results = await response.json();

            box.innerHTML = results.map(r =>
                `<a href="${r.url}" class="list-group-item list-group-item-action">
                    <span class="badge bg-secondary">${r.type}</span> ${r.title} <small class="text-muted">${r.subtitle}</small>
                </a>`
            ).join("");
        }, 250);
    });

    document.addEventListener("click", (e) => {
        if (e.target !== input) box.innerHTML = "";
    });
})();