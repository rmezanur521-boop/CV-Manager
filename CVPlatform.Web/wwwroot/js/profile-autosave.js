(function () {
    const form = document.getElementById("me-form");
    if (!form) return;

    let dirty = false;
    let stamp = form.dataset.concurrencyStamp;
    const savedText = form.dataset.savedText || "Saved";
    const savingText = form.dataset.savingText || "Saving...";
    const status = document.getElementById("save-status");

    ["FirstName", "LastName", "Location", "PhotoUrl"].forEach(id => {
        document.getElementById(id).addEventListener("input", () => { dirty = true; });
    });

    async function save() {
        if (!dirty) return;
        dirty = false;
        status.textContent = savingText;

        const payload = {
            firstName: document.getElementById("FirstName").value,
            lastName: document.getElementById("LastName").value,
            location: document.getElementById("Location").value,
            photoUrl: document.getElementById("PhotoUrl").value,
            concurrencyStamp: stamp
        };

        const response = await fetch("/Profile/SaveMe", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const result = await response.json();

        if (result.success) {
            stamp = result.concurrencyStamp;
            status.textContent = savedText;
        } else {
            status.textContent = result.message;
        }
    }

    setInterval(save, 7000);
    window.addEventListener("beforeunload", save);
})();