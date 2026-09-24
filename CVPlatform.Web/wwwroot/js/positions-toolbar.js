(function () {
    const rows = document.querySelectorAll(".selectable-row");
    const openBtn = document.getElementById("open-btn");
    const editBtn = document.getElementById("edit-btn");
    const duplicateBtn = document.getElementById("duplicate-btn");
    const deleteBtn = document.getElementById("delete-btn");
    const duplicateId = document.getElementById("duplicate-id");
    const deleteId = document.getElementById("delete-id");

    function enableLink(link, url) {
        if (!link || !url) return;
        link.href = url;
        link.classList.remove("disabled");
    }

    function selectRow(row) {
        const radio = row.querySelector("input[type=radio]");
        if (radio) radio.checked = true;

        const id = row.dataset.id;

        enableLink(openBtn, row.dataset.detailsUrl);
        enableLink(editBtn, row.dataset.editUrl);

        if (duplicateId) duplicateId.value = id;
        if (deleteId) deleteId.value = id;
        if (duplicateBtn) duplicateBtn.disabled = false;
        if (deleteBtn) deleteBtn.disabled = false;
    }

    rows.forEach(row => {
        row.addEventListener("click", () => selectRow(row));
        row.addEventListener("dblclick", () => {
            window.location.href = row.dataset.detailsUrl;
        });
    });
})();