(function () {
    const container = document.getElementById("posts-container");
    const form = document.getElementById("composer");

    if (!container || !form) return;

    const positionId = Number(container.dataset.positionId);
    const currentUserId = container.dataset.currentUserId;
    const isRecruiterOrAdmin = container.dataset.isRecruiterOrAdmin === "true";
    const sinceUrl = container.dataset.sinceUrl;
    const textarea = form.querySelector("textarea");
    const submitBtn = form.querySelector("button[type=submit]");
    const errorBox = document.getElementById("composer-error");
    let lastId = Number(container.dataset.lastId) || 0;

    function formatTimes(root) {
        root.querySelectorAll("time[datetime]").forEach(element => {
            const date = new Date(element.getAttribute("datetime"));
            if (!isNaN(date)) {
                element.textContent = date.toLocaleString([], { dateStyle: "medium", timeStyle: "short" });
            }
        });
    }

    function scrollToBottom() {
        container.scrollTop = container.scrollHeight;
    }

    function isNearBottom() {
        return container.scrollHeight - container.scrollTop - container.clientHeight < 80;
    }

    function hasPost(id) {
        return container.querySelector(`[data-post-id="${id}"]`) !== null;
    }

    function buildPost(post) {
        const wrapper = document.createElement("div");
        wrapper.className = "discussion-post" + (post.authorId === currentUserId ? " is-own" : "");
        wrapper.dataset.postId = post.id;

        const avatar = document.createElement("div");
        avatar.className = "discussion-avatar";
        avatar.textContent = (post.authorName || "?").trim().charAt(0).toUpperCase();

        const bubble = document.createElement("div");
        bubble.className = "discussion-bubble";

        const meta = document.createElement("div");
        meta.className = "discussion-meta";

        let author;
        if (isRecruiterOrAdmin && post.authorId) {
            author = document.createElement("a");
            author.href = `/Profile/ViewProfile?id=${encodeURIComponent(post.authorId)}`;
            author.className = "discussion-author text-decoration-none fw-semibold";
        } else {
            author = document.createElement("span");
            author.className = "discussion-author";
        }
        author.textContent = post.authorName;

        const time = document.createElement("time");
        time.className = "discussion-time";
        time.setAttribute("datetime", post.createdAt);
        time.textContent = post.createdAt;

        const body = document.createElement("div");
        body.className = "discussion-body";
        body.innerHTML = post.contentHtml;

        meta.appendChild(author);
        meta.appendChild(time);
        bubble.appendChild(meta);
        bubble.appendChild(body);
        wrapper.appendChild(avatar);
        wrapper.appendChild(bubble);

        return wrapper;
    }

    function appendPost(post) {
        if (hasPost(post.id)) return;

        const stickToBottom = isNearBottom() || post.authorId === currentUserId;

        const emptyState = document.getElementById("empty-state");
        if (emptyState) emptyState.remove();

        const element = buildPost(post);
        container.appendChild(element);
        formatTimes(element);

        if (post.id > lastId) lastId = post.id;
        if (stickToBottom) scrollToBottom();
    }

    async function catchUp() {
        try {
            const response = await fetch(`${sinceUrl}?positionId=${positionId}&afterId=${lastId}`);
            if (!response.ok) return;

            const posts = await response.json();
            posts.forEach(appendPost);
        } catch (err) {
            console.error(err);
        }
    }

    function showError(message) {
        errorBox.textContent = message;
        errorBox.hidden = false;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(container.dataset.hubUrl)
        .withAutomaticReconnect()
        .build();

    connection.on("ReceivePost", appendPost);

    connection.onreconnected(async () => {
        await connection.invoke("JoinPositionGroup", positionId);
        await catchUp();
    });

    connection.start()
        .then(() => connection.invoke("JoinPositionGroup", positionId))
        .then(catchUp)
        .catch(err => console.error(err));

    setInterval(() => {
        if (connection.state !== signalR.HubConnectionState.Connected) {
            catchUp();
        }
    }, 5000);

    form.addEventListener("submit", async event => {
        event.preventDefault();

        if (!textarea.value.trim()) return;

        submitBtn.disabled = true;
        errorBox.hidden = true;

        try {
            const response = await fetch(form.action, { method: "POST", body: new FormData(form) });
            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                showError(data.message || errorBox.dataset.defaultMessage);
                return;
            }

            textarea.value = "";
            appendPost(data);
        } catch (err) {
            showError(errorBox.dataset.defaultMessage);
        } finally {
            submitBtn.disabled = false;
            textarea.focus();
        }
    });

    textarea.addEventListener("keydown", event => {
        if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
            event.preventDefault();
            form.requestSubmit();
        }
    });

    window.addEventListener("beforeunload", () => {
        connection.invoke("LeavePositionGroup", positionId).catch(() => { });
    });

    formatTimes(container);
    scrollToBottom();
})();