(function () {
    const container = document.getElementById("attributes-container");
    if (!container) return;

    const cvId = container.dataset.cvId;

    container.querySelectorAll(".attribute-row").forEach(row => {
        const input = row.querySelector(".attribute-input");

        input.addEventListener("blur", async () => {
            const attributeId = row.dataset.attributeId;
            const version = row.dataset.version;

            const payload = {
                attributeId: parseInt(attributeId, 10),
                textValue: input.value,
                version: parseInt(version, 10) || 0
            };

            const response = await fetch(`/Cvs/SaveAttribute?cvId=${cvId}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (result.success) {
                row.dataset.version = result.version;
                row.classList.toggle("border-danger", result.isMissing);
                input.classList.toggle("text-danger", result.isMissing);
            } else {
                alert(result.message);
                location.reload();
            }
        });
    });
})();