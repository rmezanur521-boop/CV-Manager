(function () {
    const typeSelect = document.getElementById("attribute-type");
    const optionsGroup = document.getElementById("options-group");

    if (!typeSelect || !optionsGroup) return;

    function toggleOptions() {
        optionsGroup.hidden = typeSelect.value !== "Dropdown";
    }

    typeSelect.addEventListener("change", toggleOptions);
    toggleOptions();
})();