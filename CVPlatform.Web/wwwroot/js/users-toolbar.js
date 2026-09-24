(function () {
    const rows = document.querySelectorAll(".user-row");
    const form = document.getElementById("user-actions-form");

    if (!rows.length || !form) return;

    const hint = document.getElementById("selection-hint");
    const selectedUserId = document.getElementById("selected-user-id");
    const roleSelect = document.getElementById("role-select");
    const assignBtn = document.getElementById("assign-btn");
    const blockBtn = document.getElementById("block-btn");
    const unblockBtn = document.getElementById("unblock-btn");
    const deleteBtn = document.getElementById("delete-btn");
    const viewProfileBtn = document.getElementById("view-profile-btn");
    const viewCvsBtn = document.getElementById("view-cvs-btn");

    function enableLink(link, url) {
        link.href = url;
        link.classList.remove("disabled");
    }

    function selectRow(row) {
        const radio = row.querySelector("input[type=radio]");
        if (radio) radio.checked = true;

        const isBlocked = row.dataset.blocked === "true";
        const isSelf = row.dataset.self === "true";

        selectedUserId.value = row.dataset.id;
        roleSelect.value = row.dataset.role;
        roleSelect.disabled = false;
        assignBtn.disabled = false;
        blockBtn.disabled = isBlocked || isSelf;
        unblockBtn.disabled = !isBlocked;
        deleteBtn.disabled = isSelf;

        enableLink(viewProfileBtn, row.dataset.profileUrl);
        enableLink(viewCvsBtn, row.dataset.cvsUrl);

        hint.textContent = isSelf ? hint.dataset.selfHint : hint.dataset.defaultHint;
    }

    rows.forEach(row => row.addEventListener("click", () => selectRow(row)));

    form.addEventListener("submit", event => {
        if (!selectedUserId.value) {
            event.preventDefault();
            return;
        }

        const message = event.submitter && event.submitter.dataset.confirm;
        if (message && !confirm(message)) {
            event.preventDefault();
        }
    });
})();