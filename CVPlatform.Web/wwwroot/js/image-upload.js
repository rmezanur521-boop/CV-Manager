function initImageUploads(containerSelector) {
    const container = document.querySelector(containerSelector);
    if (!container) return;

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const antiForgeryToken = tokenInput ? tokenInput.value : "";

    container.querySelectorAll(".image-upload-input").forEach(fileInput => {
        const row = fileInput.closest(".attribute-row");

        async function handleUpload(file) {
            if (!file) return;

            const ownerId = fileInput.dataset.ownerId || "";
            const category = fileInput.dataset.category || "ProfileAttribute";

            const formData = new FormData();
            formData.append("file", file);
            formData.append("category", category);
            if (ownerId) formData.append("ownerId", ownerId);

            try {
                const response = await fetch("/Upload/Image", {
                    method: "POST",
                    headers: { "RequestVerificationToken": antiForgeryToken },
                    body: formData
                });

                if (!response.ok) {
                    const error = await response.json().catch(() => ({ message: "Upload failed." }));
                    alert(error.message || "Upload failed.");
                    return;
                }

                const result = await response.json();
                const hiddenInput = row.querySelector(".attribute-input");
                if (hiddenInput) {
                    hiddenInput.value = result.objectKey;
                    hiddenInput.dispatchEvent(new Event("blur"));
                }

                let img = row.querySelector("img");
                if (!img) {
                    img = document.createElement("img");
                    img.style.maxWidth = "150px";
                    img.className = "mt-2 d-block rounded border";
                    row.appendChild(img);
                }
                img.src = result.url;
            } catch (err) {
                alert("Upload failed. Please check connection.");
            }
        }

        fileInput.addEventListener("change", () => {
            if (fileInput.files && fileInput.files[0]) {
                handleUpload(fileInput.files[0]);
            }
        });

        if (row) {
            row.addEventListener("dragover", (e) => {
                e.preventDefault();
                e.stopPropagation();
                row.classList.add("border-primary", "bg-light");
            });

            row.addEventListener("dragleave", (e) => {
                e.preventDefault();
                e.stopPropagation();
                row.classList.remove("border-primary", "bg-light");
            });

            row.addEventListener("drop", (e) => {
                e.preventDefault();
                e.stopPropagation();
                row.classList.remove("border-primary", "bg-light");
                if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files.length > 0) {
                    fileInput.files = e.dataTransfer.files;
                    handleUpload(e.dataTransfer.files[0]);
                }
            });
        }
    });
}