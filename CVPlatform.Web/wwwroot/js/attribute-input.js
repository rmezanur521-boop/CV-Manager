function initAttributeInputs(containerSelector, saveUrlBuilder) {
    const container = document.querySelector(containerSelector);
    if (!container) return;

    const changeTypes = ["Boolean", "Dropdown", "Date", "DateRange"];

    container.querySelectorAll(".attribute-row").forEach(row => {
        const type = row.dataset.type;
        const inputs = row.querySelectorAll(".attribute-input");
        if (inputs.length === 0) return;

        const eventName = changeTypes.includes(type) ? "change" : "blur";

        inputs.forEach(input => {
            input.addEventListener(eventName, () => {
                row._queue = (row._queue || Promise.resolve()).then(() => save(row, type, inputs));
            });
        });
    });

    function buildPayload(row, type, inputs) {
        const input = inputs[0];
        const payload = {
            attributeId: parseInt(row.dataset.attributeId, 10),
            version: parseInt(row.dataset.version, 10) || 0
        };

        switch (type) {
            case "Numeric":
                payload.numericValue = input.value === "" ? null : parseFloat(input.value);
                break;
            case "Date":
                payload.dateValue = input.value || null;
                break;
            case "DateRange":
                payload.dateRangeStart = inputs[0].value || null;
                payload.dateRangeEnd = inputs[1].value || null;
                break;
            case "Boolean":
                payload.booleanValue = input.checked;
                break;
            case "Dropdown":
                payload.selectedOptionId = input.value === "" ? null : parseInt(input.value, 10);
                break;
            default:
                payload.textValue = input.value;
        }
        return payload;
    }

    async function save(row, type, inputs) {
        try {
            const response = await fetch(saveUrlBuilder(), {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(buildPayload(row, type, inputs))
            });
            const result = await response.json();

            if (result.success) {
                row.dataset.version = result.version;
                if (typeof result.isMissing === "boolean") {
                    row.classList.toggle("border-danger", result.isMissing);
                }
            } else {
                alert(result.message);
                location.reload();
            }
        } catch {
            alert("Save failed. Please check your connection.");
        }
    }
}