(function () {
    const input = document.getElementById("TagsCsv");
    const box = document.getElementById("tag-suggestions");
    if (!input) return;

    let timer = null;

    input.addEventListener("input", () => {
        clearTimeout(timer);
        const parts = input.value.split(",");
        const current = parts[parts.length - 1].trim();

        if (current.length < 1) {
            box.innerHTML = "";
            return;
        }

        timer = setTimeout(async () => {
            const response = await fetch(`/Profile/SearchTags?prefix=${encodeURIComponent(current)}`);
            const tags = await response.json();

            box.innerHTML = tags.map(t =>
                `<button type="button" class="list-group-item list-group-item-action">${t}</button>`
            ).join("");

            box.querySelectorAll("button").forEach(btn => {
                btn.addEventListener("click", () => {
                    parts[parts.length - 1] = " " + btn.textContent;
                    input.value = parts.join(",").replace(/^,\s*/, "");
                    box.innerHTML = "";
                });
            });
        }, 250);
    });

    document.addEventListener("click", (e) => {
        if (e.target !== input) box.innerHTML = "";
    });
})();