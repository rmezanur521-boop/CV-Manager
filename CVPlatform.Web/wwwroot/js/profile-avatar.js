(function () {
    const fileInput = document.getElementById("avatar-file-input");
    const selectBtn = document.getElementById("btn-select-photo");
    const saveBtn = document.getElementById("btn-save-photo");
    const cancelBtn = document.getElementById("btn-cancel-photo");
    const deleteBtn = document.getElementById("btn-delete-photo");
    const previewImg = document.getElementById("avatar-preview-img");
    const previewInitials = document.getElementById("avatar-preview-initials");
    const statusMsg = document.getElementById("avatar-status-msg");
    const photoUrlInput = document.getElementById("PhotoUrl");
    const avatarForm = document.getElementById("avatar-form");

    if (!fileInput || !selectBtn) return;

    let originalSrc = previewImg ? (previewImg.getAttribute("src") || "") : "";
    let originalDisplay = previewImg ? previewImg.style.display : "none";
    let originalInitialsDisplay = previewInitials ? previewInitials.style.display : "inline";

    function showStatus(text, isError) {
        if (!statusMsg) return;
        statusMsg.textContent = text;
        statusMsg.className = "small mt-1 " + (isError ? "text-danger" : "text-success");
    }

    function clearStatus() {
        if (!statusMsg) return;
        statusMsg.textContent = "";
        statusMsg.className = "small mt-1";
    }

    selectBtn.addEventListener("click", () => {
        fileInput.value = "";
        fileInput.click();
    });

    fileInput.addEventListener("change", () => {
        const file = fileInput.files && fileInput.files[0];
        if (!file) return;

        if (file.size > 5 * 1024 * 1024) {
            showStatus("File size must not exceed 5 MB.", true);
            fileInput.value = "";
            return;
        }

        const validTypes = ["image/jpeg", "image/png", "image/webp"];
        if (!validTypes.includes(file.type.toLowerCase())) {
            showStatus("Only JPG, PNG, or WebP files are allowed.", true);
            fileInput.value = "";
            return;
        }

        clearStatus();
        const reader = new FileReader();
        reader.onload = function (e) {
            previewImg.src = e.target.result;
            previewImg.style.display = "block";
            previewInitials.style.display = "none";

            saveBtn.style.display = "inline-block";
            cancelBtn.style.display = "inline-block";
            selectBtn.style.display = "none";
            if (deleteBtn) deleteBtn.style.display = "none";
        };
        reader.readAsDataURL(file);
    });

    cancelBtn.addEventListener("click", () => {
        fileInput.value = "";
        previewImg.src = originalSrc;
        previewImg.style.display = originalDisplay;
        previewInitials.style.display = originalInitialsDisplay;

        saveBtn.style.display = "none";
        cancelBtn.style.display = "none";
        selectBtn.style.display = "inline-block";
        if (originalSrc && originalSrc.trim().length > 0) {
            if (deleteBtn) deleteBtn.style.display = "inline-block";
        }
        clearStatus();
    });

    saveBtn.addEventListener("click", async () => {
        const file = fileInput.files && fileInput.files[0];
        if (!file) {
            showStatus("Please select an image file.", true);
            return;
        }

        const formData = new FormData(avatarForm);
        saveBtn.disabled = true;
        cancelBtn.disabled = true;
        showStatus("Uploading photo...", false);

        try {
            const resp = await fetch("/Profile/UploadPhoto", {
                method: "POST",
                body: formData
            });

            const data = await resp.json();
            if (data.success) {
                showStatus("Profile photo updated successfully!", false);
                originalSrc = data.photoUrl;
                originalDisplay = "block";
                originalInitialsDisplay = "none";

                previewImg.src = data.photoUrl;
                previewImg.style.display = "block";
                previewInitials.style.display = "none";

                if (photoUrlInput) {
                    photoUrlInput.value = data.photoUrl;
                    photoUrlInput.dispatchEvent(new Event("input", { bubbles: true }));
                }

                // Update left summary card avatar
                const summaryAvatar = document.querySelector(".profile-avatar");
                if (summaryAvatar) {
                    summaryAvatar.innerHTML = `<img src="${data.photoUrl}" alt="Profile Photo" />`;
                }

                saveBtn.style.display = "none";
                cancelBtn.style.display = "none";
                selectBtn.style.display = "inline-block";
                selectBtn.innerHTML = '<i class="bi bi-upload me-1"></i>Change Photo';
                if (deleteBtn) deleteBtn.style.display = "inline-block";
            } else {
                showStatus(data.message || "Failed to upload photo.", true);
            }
        } catch (err) {
            showStatus("Network error occurred while uploading.", true);
        } finally {
            saveBtn.disabled = false;
            cancelBtn.disabled = false;
        }
    });

    if (deleteBtn) {
        deleteBtn.addEventListener("click", async () => {
            if (!confirm("Are you sure you want to delete your profile photo?")) return;

            const candidateIdInput = avatarForm.querySelector('input[name="candidateId"]');
            const candidateId = candidateIdInput ? candidateIdInput.value : "";
            const tokenInput = avatarForm.querySelector('input[name="__RequestVerificationToken"]');
            const token = tokenInput ? tokenInput.value : "";

            deleteBtn.disabled = true;
            showStatus("Deleting photo...", false);

            const formData = new FormData();
            if (candidateId) formData.append("candidateId", candidateId);
            if (token) formData.append("__RequestVerificationToken", token);

            try {
                const resp = await fetch("/Profile/DeletePhoto", {
                    method: "POST",
                    body: formData
                });

                const data = await resp.json();
                if (data.success) {
                    showStatus("Profile photo removed.", false);
                    originalSrc = "";
                    originalDisplay = "none";
                    originalInitialsDisplay = "inline";

                    previewImg.src = "";
                    previewImg.style.display = "none";
                    previewInitials.style.display = "inline";

                    if (photoUrlInput) {
                        photoUrlInput.value = "";
                        photoUrlInput.dispatchEvent(new Event("input", { bubbles: true }));
                    }

                    // Update left summary card avatar
                    const summaryAvatar = document.querySelector(".profile-avatar");
                    if (summaryAvatar) {
                        summaryAvatar.textContent = previewInitials.textContent;
                    }

                    deleteBtn.style.display = "none";
                    selectBtn.innerHTML = '<i class="bi bi-upload me-1"></i>Upload Photo';
                } else {
                    showStatus(data.message || "Failed to delete photo.", true);
                }
            } catch (err) {
                showStatus("Network error occurred while deleting.", true);
            } finally {
                deleteBtn.disabled = false;
            }
        });
    }
})();
