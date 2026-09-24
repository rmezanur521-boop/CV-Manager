(function() {
    const rows = document.querySelectorAll(".selectable-row");
    const editBtn = document.getElementById("edit-btn");
    const deleteBtn = document.getElementById("delete-btn");
    const deleteId = document.getElementById("delete-id");
    const hint = document.getElementById("selection-hint");

    if (!hint) return;

    const defaultHint = hint.textContent;
    const builtInHint = hint.dataset.builtinHint || defaultHint;

    function selectRow(row)
    {
        const radio = row.querySelector("input[type=radio]");
        if (radio) radio.checked = true;

        const id = row.dataset.id;
        const isBuiltIn = row.dataset.builtin === "true";

        if (editBtn)
        {
            if (isBuiltIn)
            {
                editBtn.classList.add("disabled");
                editBtn.removeAttribute("href");
            }
            else
            {
               editBtn.href = row.dataset.editUrl;
                editBtn.classList.remove("disabled");
            }
        }

        if (deleteId) deleteId.value = id;
        if (deleteBtn) deleteBtn.disabled = isBuiltIn;

        hint.textContent = isBuiltIn ? builtInHint : defaultHint;
        }

        rows.forEach(row => {
            row.addEventListener("click", () => selectRow(row));
        });
    })();