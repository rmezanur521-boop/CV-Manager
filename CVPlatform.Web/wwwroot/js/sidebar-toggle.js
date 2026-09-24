(function () {
    var sidebar = document.getElementById("appSidebar");
    var backdrop = document.getElementById("sidebarBackdrop");
    if (!sidebar) return;

    var toggles = document.querySelectorAll("[data-sidebar-toggle]");
    var desktopQuery = window.matchMedia("(min-width: 992px)");
    var storageKey = "cvplatform-sidebar-open";

    function isDesktop() {
        return desktopQuery.matches;
    }

    function toggleDesktop() {
        var collapsed = document.documentElement.classList.toggle("sidebar-collapsed");
        localStorage.setItem(storageKey, collapsed ? "false" : "true");
    }

    function toggleMobile() {
        var opening = !sidebar.classList.contains("sidebar-open");
        sidebar.classList.toggle("sidebar-open", opening);
        if (backdrop) backdrop.classList.toggle("show", opening);
    }

    function toggle() {
        if (isDesktop()) {
            toggleDesktop();
        } else {
            toggleMobile();
        }
    }

    toggles.forEach(function (btn) {
        btn.addEventListener("click", toggle);
    });

    if (backdrop) {
        backdrop.addEventListener("click", function () {
            sidebar.classList.remove("sidebar-open");
            backdrop.classList.remove("show");
        });
    }
})();