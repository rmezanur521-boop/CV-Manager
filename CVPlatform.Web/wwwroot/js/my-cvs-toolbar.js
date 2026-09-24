(function () {
    const rows = document.querySelectorAll(".cv-row");
    const openBtn = document.getElementById("open-cv-btn");
    const deleteBtn = document.getElementById("delete-cv-btn");
    const deleteId = document.getElementById("delete-cv-id");

    function selectRow(row) {
        const radio = row.querySelector("input[type=radio]");
        if (radio) radio.checked = true;

        const id = row.dataset.id;
        const editUrl = row.dataset.editUrl;

        if (openBtn) {
            openBtn.href = editUrl;
            openBtn.classList.remove("disabled");
        }
        if (deleteId) deleteId.value = id;
        if (deleteBtn) deleteBtn.disabled = false;
    }

    rows.forEach(row => {
        row.addEventListener("click", () => selectRow(row));
    });
})();