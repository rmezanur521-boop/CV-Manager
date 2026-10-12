(function () {
    'use strict';

    // Theme Management
    function getTheme() {
        var match = document.cookie.match(/cvplatform-theme=(dark|light)/);
        return match ? match[1] : (document.documentElement.getAttribute('data-theme') || 'light');
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        document.cookie = 'cvplatform-theme=' + theme + '; path=/; max-age=31536000; SameSite=Lax';

        var footerSwitch = document.getElementById('footerThemeSwitch');
        if (footerSwitch) {
            footerSwitch.checked = (theme === 'dark');
        }

        var headerThemeIcon = document.getElementById('headerThemeIcon');
        if (headerThemeIcon) {
            headerThemeIcon.className = theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-stars-fill';
        }

        // Notify server asynchronously to keep session in sync
        fetch('/Settings/SetTheme?theme=' + theme + '&returnUrl=' + encodeURIComponent(window.location.pathname), {
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).catch(function () {});
    }

    // Initialize Theme Switch Controls
    var currentTheme = getTheme();
    var footerSwitch = document.getElementById('footerThemeSwitch');
    if (footerSwitch) {
        footerSwitch.checked = (currentTheme === 'dark');
        footerSwitch.addEventListener('change', function () {
            setTheme(this.checked ? 'dark' : 'light');
        });
    }

    var headerThemeBtn = document.getElementById('headerThemeBtn');
    if (headerThemeBtn) {
        headerThemeBtn.addEventListener('click', function (e) {
            e.preventDefault();
            var nextTheme = getTheme() === 'dark' ? 'light' : 'dark';
            setTheme(nextTheme);
        });
    }

    // Mobile Navigation Toggle
    var mobileToggle = document.getElementById('publicMobileToggle');
    var mobileMenu = document.getElementById('publicMobileMenu');
    if (mobileToggle && mobileMenu) {
        mobileToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            var isOpen = mobileMenu.classList.toggle('is-open');
            mobileToggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
            var icon = mobileToggle.querySelector('i');
            if (icon) {
                icon.className = isOpen ? 'bi bi-x-lg' : 'bi bi-list';
            }
        });

        document.addEventListener('click', function (e) {
            if (!mobileMenu.contains(e.target) && !mobileToggle.contains(e.target)) {
                if (mobileMenu.classList.contains('is-open')) {
                    mobileMenu.classList.remove('is-open');
                    mobileToggle.setAttribute('aria-expanded', 'false');
                    var icon = mobileToggle.querySelector('i');
                    if (icon) icon.className = 'bi bi-list';
                }
            }
        });
    }

    // Job Search & Filtering
    var searchInput = document.getElementById('jobSearchInput');
    var levelSelect = document.getElementById('jobLevelSelect');
    var locationSelect = document.getElementById('jobLocationSelect');
    var techSelect = document.getElementById('jobTechSelect');
    var searchBtn = document.getElementById('jobSearchBtn');
    var jobCards = document.querySelectorAll('.job-card');
    var noJobsNotice = document.getElementById('noJobsNotice');

    function filterJobs() {
        if (!jobCards.length) return;

        var term = (searchInput ? searchInput.value : '').toLowerCase().trim();
        var selectedLevel = (levelSelect ? levelSelect.value : '').toLowerCase().trim();
        var selectedLoc = (locationSelect ? locationSelect.value : '').toLowerCase().trim();
        var selectedTech = (techSelect ? techSelect.value : '').toLowerCase().trim();

        var visibleCount = 0;

        jobCards.forEach(function (card) {
            var title = (card.getAttribute('data-title') || '').toLowerCase();
            var company = (card.getAttribute('data-company') || '').toLowerCase();
            var level = (card.getAttribute('data-level') || '').toLowerCase();
            var location = (card.getAttribute('data-location') || '').toLowerCase();
            var tech = (card.getAttribute('data-tech') || '').toLowerCase();
            var fullText = card.innerText.toLowerCase();

            var matchesTerm = !term || title.includes(term) || company.includes(term) || fullText.includes(term);
            var matchesLevel = !selectedLevel || level.includes(selectedLevel);
            var matchesLoc = !selectedLoc || location.includes(selectedLoc) || fullText.includes(selectedLoc);
            var matchesTech = !selectedTech || tech.includes(selectedTech) || fullText.includes(selectedTech);

            if (matchesTerm && matchesLevel && matchesLoc && matchesTech) {
                card.style.display = '';
                visibleCount++;
            } else {
                card.style.display = 'none';
            }
        });

        if (noJobsNotice) {
            noJobsNotice.style.display = visibleCount === 0 ? 'block' : 'none';
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', filterJobs);
    }
    if (levelSelect) {
        levelSelect.addEventListener('change', filterJobs);
    }
    if (locationSelect) {
        locationSelect.addEventListener('change', filterJobs);
    }
    if (techSelect) {
        techSelect.addEventListener('change', filterJobs);
    }
    if (searchBtn) {
        searchBtn.addEventListener('click', function (e) {
            e.preventDefault();
            filterJobs();
        });
    }

    // Smooth Anchor Scroll
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener('click', function (e) {
            var targetId = this.getAttribute('href');
            if (targetId && targetId !== '#') {
                var targetEl = document.querySelector(targetId);
                if (targetEl) {
                    e.preventDefault();
                    if (mobileMenu && mobileMenu.classList.contains('is-open')) {
                        mobileMenu.classList.remove('is-open');
                        if (mobileToggle) {
                            var icon = mobileToggle.querySelector('i');
                            if (icon) icon.className = 'bi bi-list';
                        }
                    }
                    targetEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            }
        });
    });
})();
