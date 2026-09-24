(function () {
    const rows = document.querySelectorAll(".cv-row");
    const viewBtn = document.getElementById("view-cv-btn");
    const likeBtn = document.getElementById("toolbar-like-btn");
    const likeIcon = document.getElementById("toolbar-like-icon");
    const likeLabel = document.getElementById("toolbar-like-label");

    let selectedRow = null;

    function updateToolbarLikeState(row) {
        if (!likeBtn || !row) return;
        likeBtn.classList.remove("disabled");
        const isLiked = row.dataset.liked === "true";
        if (isLiked) {
            likeBtn.className = "btn btn-primary btn-sm";
            if (likeIcon) likeIcon.className = "bi bi-hand-thumbs-up-fill";
            if (likeLabel) likeLabel.textContent = "Liked";
        } else {
            likeBtn.className = "btn btn-outline-primary btn-sm";
            if (likeIcon) likeIcon.className = "bi bi-hand-thumbs-up";
            if (likeLabel) likeLabel.textContent = "Like";
        }
    }

    rows.forEach(row => {
        row.addEventListener("click", () => {
            selectedRow = row;
            const radio = row.querySelector("input[type=radio]");
            if (radio) radio.checked = true;

            if (viewBtn) {
                viewBtn.href = row.dataset.viewUrl;
                viewBtn.classList.remove("disabled");
            }

            updateToolbarLikeState(row);
        });
    });

    if (likeBtn) {
        likeBtn.addEventListener("click", async () => {
            if (!selectedRow || likeBtn.classList.contains("disabled")) return;

            const cvId = selectedRow.dataset.id;
            try {
                const response = await fetch(`/RecruiterCvs/ToggleLike?cvId=${cvId}`, { method: "POST" });
                if (!response.ok) return;

                const result = await response.json();
                const currentlyLiked = selectedRow.dataset.liked === "true";
                const nextLiked = !currentlyLiked;

                selectedRow.dataset.liked = nextLiked ? "true" : "false";
                selectedRow.dataset.likesCount = result.likesCount;

                // Update row badge
                const badge = document.getElementById(`row-like-badge-${cvId}`);
                if (badge) {
                    badge.className = `badge ${nextLiked ? "bg-primary" : "bg-light text-dark border"}`;
                    const icon = badge.querySelector("i");
                    if (icon) icon.className = `bi bi-hand-thumbs-up${nextLiked ? "-fill" : ""}`;
                    const countSpan = badge.querySelector(".row-like-count");
                    if (countSpan) countSpan.textContent = result.likesCount;
                }

                // Update toolbar button
                updateToolbarLikeState(selectedRow);
            } catch (err) {
                console.error("Like toggle failed", err);
            }
        });
    }
})();