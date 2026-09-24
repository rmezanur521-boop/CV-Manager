(function () {
    document.querySelectorAll(".like-btn").forEach(btn => {
        btn.addEventListener("click", async () => {
            const cvId = btn.dataset.cvId;

            const response = await fetch(`/RecruiterCvs/ToggleLike?cvId=${cvId}`, { method: "POST" });
            const result = await response.json();

            btn.querySelector(".like-count").textContent = result.likesCount;
            btn.classList.toggle("btn-primary");
            btn.classList.toggle("btn-outline-primary");
        });
    });
})();