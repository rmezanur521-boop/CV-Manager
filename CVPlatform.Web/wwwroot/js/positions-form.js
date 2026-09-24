(function () {
    "use strict";

    const dataScript = document.getElementById("position-form-data");
    if (!dataScript) return;

    const data = JSON.parse(dataScript.textContent);
    const catalog = data.attributeCatalog || [];
    const strings = data.strings || {};
    let selectedIds = (data.selectedAttributeIds || []).slice();
    let requiredIds = new Set(data.requiredAttributeIds || []);

    const catalogById = new Map(catalog.map(a => [a.id, a]));

    const OPERATORS = {
        Numeric: ["Equals", "NotEquals", "GreaterThan", "LessThan", "GreaterThanOrEqual", "LessThanOrEqual"],
        Date: ["Equals", "NotEquals", "GreaterThan", "LessThan", "GreaterThanOrEqual", "LessThanOrEqual"],
        Boolean: ["Equals", "NotEquals"],
        Dropdown: ["Equals"]
    };
    const DEFAULT_OPERATORS = ["Equals", "NotEquals", "Contains"];
    const OPERATOR_LABELS = {
        Equals: "=",
        NotEquals: "\u2260",
        GreaterThan: ">",
        LessThan: "<",
        GreaterThanOrEqual: ">=",
        LessThanOrEqual: "<=",
        Contains: "contains"
    };

    function escapeHtml(value) {
        const div = document.createElement("div");
        div.textContent = value ?? "";
        return div.innerHTML;
    }

    const pickList = document.getElementById("attribute-pick-list");
    const selectedList = document.getElementById("selected-attribute-list");
    const searchInput = document.getElementById("attribute-search");

    function renderPickList(filter) {
        if (!pickList) return;
        const term = (filter || "").trim().toLowerCase();

        const matches = catalog.filter(a => !term || a.name.toLowerCase().startsWith(term));

        pickList.innerHTML = matches.length
            ? matches.map(a => `
                <label class="attribute-pick-row">
                    <input type="checkbox" class="attribute-toggle" value="${a.id}" ${selectedIds.includes(a.id) ? "checked" : ""} />
                    <span class="attribute-pick-info">
                        <span class="attribute-pick-name">${escapeHtml(a.name)}</span>
                        <small>${escapeHtml(a.categoryName)} &middot; ${escapeHtml(a.type)}</small>
                    </span>
                </label>
            `).join("")
            : `<div class="empty-state"><p class="mb-0">No matching attributes.</p></div>`;

        pickList.querySelectorAll(".attribute-toggle").forEach(cb => {
            cb.addEventListener("change", () => toggleAttribute(parseInt(cb.value, 10), cb.checked));
        });
    }

    function toggleAttribute(id, checked) {
        if (checked && !selectedIds.includes(id)) {
            selectedIds.push(id);
        } else if (!checked) {
            selectedIds = selectedIds.filter(x => x !== id);
            requiredIds.delete(id);
        }
        renderSelectedList();
    }

    function removeAttribute(id) {
        selectedIds = selectedIds.filter(x => x !== id);
        requiredIds.delete(id);
        renderSelectedList();
        renderPickList(searchInput ? searchInput.value : "");
    }

    function renderSelectedList() {
        if (!selectedList) return;

        if (selectedIds.length === 0) {
            selectedList.innerHTML = `<div class="empty-state"><p class="mb-0">${escapeHtml(strings.noAttributesSelected || "No attributes selected yet.")}</p></div>`;
            return;
        }

        selectedList.innerHTML = selectedIds.map((id, index) => {
            const attribute = catalogById.get(id);
            if (!attribute) return "";
            return `
                <div class="selected-attribute-row">
                    <input type="hidden" name="SelectedAttributeIds" value="${id}" />
                    <span class="selected-attribute-index">${index + 1}.</span>
                    <span class="selected-attribute-name">${escapeHtml(attribute.name)}</span>
                    <span class="badge-soft-info">${escapeHtml(attribute.type)}</span>
                    <label class="required-toggle">
                        <input type="checkbox" name="RequiredAttributeIds" value="${id}" ${requiredIds.has(id) ? "checked" : ""} />
                        ${escapeHtml(strings.required || "Required")}
                    </label>
                    <button type="button" class="icon-btn icon-btn-sm remove-attribute" data-id="${id}" title="${escapeHtml(strings.remove || "Remove")}">
                        <i class="bi bi-x-lg"></i>
                    </button>
                </div>
            `;
        }).join("");

        selectedList.querySelectorAll(".required-toggle input").forEach(cb => {
            cb.addEventListener("change", () => {
                const id = parseInt(cb.value, 10);
                if (cb.checked) requiredIds.add(id); else requiredIds.delete(id);
            });
        });

        selectedList.querySelectorAll(".remove-attribute").forEach(btn => {
            btn.addEventListener("click", () => removeAttribute(parseInt(btn.dataset.id, 10)));
        });
    }

    if (pickList) {
        renderPickList("");
        renderSelectedList();
    }

    if (searchInput) {
        searchInput.addEventListener("input", () => renderPickList(searchInput.value));
    }

    const rulesContainer = document.getElementById("rules-container");
    const addRuleBtn = document.getElementById("add-rule");
    let ruleIndex = 0;

    function operatorsFor(type) {
        return OPERATORS[type] || DEFAULT_OPERATORS;
    }

    function buildValueControl(rowIndex, attribute, rule) {
        const name = `AccessRules[${rowIndex}]`;

        if (attribute.type === "Boolean") {
            const current = rule ? rule.comparisonValue : "true";
            return `
                <select name="${name}.ComparisonValue" class="form-select rule-value">
                    <option value="true" ${current === "true" ? "selected" : ""}>True</option>
                    <option value="false" ${current === "false" ? "selected" : ""}>False</option>
                </select>`;
        }

        if (attribute.type === "Dropdown") {
            const options = (attribute.options || []).map(o =>
                `<option value="${o.id}" ${rule && rule.comparisonOptionId === o.id ? "selected" : ""}>${escapeHtml(o.value)}</option>`
            ).join("");
            return `<select name="${name}.ComparisonOptionId" class="form-select rule-value">${options}</select>`;
        }

        if (attribute.type === "Numeric") {
            return `<input type="number" step="any" name="${name}.ComparisonValue" class="form-control rule-value" value="${rule ? escapeHtml(rule.comparisonValue) : ""}" />`;
        }

        if (attribute.type === "Date") {
            return `<input type="date" name="${name}.ComparisonValue" class="form-control rule-value" value="${rule ? escapeHtml(rule.comparisonValue) : ""}" />`;
        }

        return `<input type="text" name="${name}.ComparisonValue" class="form-control rule-value" value="${rule ? escapeHtml(rule.comparisonValue) : ""}" />`;
    }

    function buildOperatorSelect(rowIndex, attribute, rule) {
        const options = operatorsFor(attribute.type).map(op =>
            `<option value="${op}" ${rule && rule.operator === op ? "selected" : ""}>${OPERATOR_LABELS[op]}</option>`
        ).join("");
        return `<select name="AccessRules[${rowIndex}].Operator" class="form-select rule-operator">${options}</select>`;
    }

    function addRuleRow(rule) {
        if (catalog.length === 0) return;

        const rowIndex = ruleIndex++;
        const selectedAttributeId = rule ? rule.attributeId : catalog[0].id;
        const attribute = catalogById.get(selectedAttributeId) || catalog[0];

        const row = document.createElement("div");
        row.className = "rule-row";

        const attributeOptions = catalog.map(a =>
            `<option value="${a.id}" ${a.id === selectedAttributeId ? "selected" : ""}>${escapeHtml(a.name)}</option>`
        ).join("");

        row.innerHTML = `
            <select name="AccessRules[${rowIndex}].AttributeId" class="form-select rule-attribute">${attributeOptions}</select>
            <span class="rule-operator-slot">${buildOperatorSelect(rowIndex, attribute, rule)}</span>
            <span class="rule-value-slot">${buildValueControl(rowIndex, attribute, rule)}</span>
            <button type="button" class="icon-btn icon-btn-sm remove-rule" title="${escapeHtml(strings.remove || "Remove")}">
                <i class="bi bi-x-lg"></i>
            </button>
        `;

        row.querySelector(".rule-attribute").addEventListener("change", (e) => {
            const newAttribute = catalogById.get(parseInt(e.target.value, 10));
            row.querySelector(".rule-operator-slot").innerHTML = buildOperatorSelect(rowIndex, newAttribute, null);
            row.querySelector(".rule-value-slot").innerHTML = buildValueControl(rowIndex, newAttribute, null);
        });

        row.querySelector(".remove-rule").addEventListener("click", () => row.remove());

        rulesContainer.appendChild(row);
    }

    if (rulesContainer) {
        (data.accessRules || []).forEach(addRuleRow);
        if (addRuleBtn) {
            addRuleBtn.addEventListener("click", () => addRuleRow(null));
        }
    }

    const accessModeSelect = document.getElementById("access-mode-select");
    const accessRulesSection = document.getElementById("access-rules-section");

    function syncAccessRulesVisibility() {
        if (!accessModeSelect || !accessRulesSection) return;
        accessRulesSection.style.display = accessModeSelect.value === "Restricted" ? "" : "none";
    }

    if (accessModeSelect) {
        syncAccessRulesVisibility();
        accessModeSelect.addEventListener("change", syncAccessRulesVisibility);
    }

    const tagInput = document.getElementById("project-tags-input");
    const tagBox = document.getElementById("project-tag-suggestions");
    let tagTimer = null;

    if (tagInput && tagBox) {
        tagInput.addEventListener("input", () => {
            clearTimeout(tagTimer);
            const parts = tagInput.value.split(",");
            const current = parts[parts.length - 1].trim();

            if (current.length < 1) {
                tagBox.innerHTML = "";
                return;
            }

            tagTimer = setTimeout(async () => {
                const response = await fetch(`/Profile/SearchTags?prefix=${encodeURIComponent(current)}`);
                const tags = await response.json();

                tagBox.innerHTML = tags.map(t =>
                    `<button type="button" class="list-group-item list-group-item-action">${escapeHtml(t)}</button>`
                ).join("");

                tagBox.querySelectorAll("button").forEach(btn => {
                    btn.addEventListener("click", () => {
                        parts[parts.length - 1] = " " + btn.textContent;
                        tagInput.value = parts.join(",").replace(/^,\s*/, "");
                        tagBox.innerHTML = "";
                    });
                });
            }, 250);
        });

        document.addEventListener("click", (e) => {
            if (e.target !== tagInput) tagBox.innerHTML = "";
        });
    }
})();