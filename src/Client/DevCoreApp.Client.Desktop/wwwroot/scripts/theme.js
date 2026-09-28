"use strict";

(function () {
    const STORAGE_KEY = "theme-preference";

    function getSystemTheme() {
        return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function applyTheme(preference) {
        var resolved;
        if (!preference || preference === "System") {
            resolved = getSystemTheme();
        } else {
            resolved = preference.toLowerCase();
        }
        document.documentElement.setAttribute("data-bs-theme", resolved);
    }

    function setTheme(preference) {
        localStorage.setItem(STORAGE_KEY, preference);
        applyTheme(preference);
    }

    // Listen for OS theme changes — re-apply if current preference is "System"
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function () {
        var stored = localStorage.getItem(STORAGE_KEY);
        if (!stored || stored === "System") {
            applyTheme("System");
        }
    });

    // Expose to Blazor JS interop
    window.themeInterop = {
        setTheme: setTheme,
        getSystemTheme: getSystemTheme,
        applyTheme: applyTheme,
        getStoredPreference: function () {
            return localStorage.getItem(STORAGE_KEY);
        }
    };
})();
