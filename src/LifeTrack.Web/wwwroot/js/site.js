(function () {
    "use strict";

    const SELECTORS = {
        SHELL: "[data-app-shell]",
        OPEN_BUTTON: "[data-app-nav-open]",
        CLOSE_BUTTON: "[data-app-nav-close]",
        SIDEBAR: "[data-app-sidebar]",
        SIDEBAR_LINK: "[data-app-sidebar] a",
        THEME_TOGGLE: "[data-app-theme-toggle]"
    };

    const CSS_CLASSES = {
        NAVIGATION_OPEN: "app-shell--nav-open"
    };

    const CONSTANTS = {
        ESCAPE_KEY: "Escape",
        MOBILE_QUERY: "(max-width: 991.98px)",
        EVENT_CLICK: "click",
        EVENT_KEYDOWN: "keydown",
        EVENT_RESIZE: "resize",
        ARIA_EXPANDED: "aria-expanded",
        ARIA_HIDDEN: "aria-hidden",
        INERT_ATTRIBUTE: "inert",
        TRUE: "true",
        FALSE: "false",
        INERT_VALUE: "",
        OPEN: true,
        CLOSED: false,
        THEME_ATTRIBUTE: "data-theme",
        THEME_STORAGE_KEY: "lifetrack-theme",
        THEME_LIGHT: "light",
        THEME_DARK: "dark",
        ARIA_LABEL: "aria-label",
        ARIA_PRESSED: "aria-pressed",
        THEME_LIGHT_LABEL: "Activar tema claro",
        THEME_DARK_LABEL: "Activar tema oscuro"
    };

    function _getStoredTheme() {
        try {
            return window.localStorage.getItem(CONSTANTS.THEME_STORAGE_KEY);
        } catch (error) {
            return null;
        }
    }

    function _setTheme(theme, toggle) {
        document.documentElement.setAttribute(CONSTANTS.THEME_ATTRIBUTE, theme);
        toggle.setAttribute(CONSTANTS.ARIA_LABEL, theme === CONSTANTS.THEME_DARK ? CONSTANTS.THEME_LIGHT_LABEL : CONSTANTS.THEME_DARK_LABEL);
        toggle.setAttribute(CONSTANTS.ARIA_PRESSED, theme === CONSTANTS.THEME_DARK ? CONSTANTS.TRUE : CONSTANTS.FALSE);

        try {
            window.localStorage.setItem(CONSTANTS.THEME_STORAGE_KEY, theme);
        } catch (error) {
            return;
        }
    }

    function _initializeTheme() {
        const toggle = document.querySelector(SELECTORS.THEME_TOGGLE);

        if (!toggle) {
            return;
        }

        const storedTheme = _getStoredTheme();
        const theme = storedTheme === CONSTANTS.THEME_DARK ? CONSTANTS.THEME_DARK : CONSTANTS.THEME_LIGHT;
        _setTheme(theme, toggle);
        toggle.addEventListener(CONSTANTS.EVENT_CLICK, function () {
            const nextTheme = document.documentElement.getAttribute(CONSTANTS.THEME_ATTRIBUTE) === CONSTANTS.THEME_DARK
                ? CONSTANTS.THEME_LIGHT
                : CONSTANTS.THEME_DARK;
            _setTheme(nextTheme, toggle);
        });
    }

    function _setNavigation(shell, openButton, sidebar, isOpen) {
        const isMobile = window.matchMedia(CONSTANTS.MOBILE_QUERY).matches;
        const isClosedMobileNavigation = isMobile && !isOpen;

        shell.classList.toggle(CSS_CLASSES.NAVIGATION_OPEN, isOpen);
        openButton.setAttribute(CONSTANTS.ARIA_EXPANDED, isOpen ? CONSTANTS.TRUE : CONSTANTS.FALSE);
        sidebar.setAttribute(CONSTANTS.ARIA_HIDDEN, isMobile && isOpen ? CONSTANTS.FALSE : isMobile ? CONSTANTS.TRUE : CONSTANTS.FALSE);

        if (isClosedMobileNavigation) {
            sidebar.setAttribute(CONSTANTS.INERT_ATTRIBUTE, CONSTANTS.INERT_VALUE);
            return;
        }

        sidebar.removeAttribute(CONSTANTS.INERT_ATTRIBUTE);
    }

    function _closeNavigation(shell, openButton, sidebar, shouldRestoreFocus) {
        _setNavigation(shell, openButton, sidebar, CONSTANTS.CLOSED);

        if (shouldRestoreFocus) {
            openButton.focus();
        }
    }

    function _initialize() {
        _initializeTheme();
        const shell = document.querySelector(SELECTORS.SHELL);
        const openButton = document.querySelector(SELECTORS.OPEN_BUTTON);
        const sidebar = document.querySelector(SELECTORS.SIDEBAR);

        if (!shell || !openButton || !sidebar) {
            return;
        }

        _setNavigation(shell, openButton, sidebar, CONSTANTS.CLOSED);

        document.querySelectorAll(SELECTORS.CLOSE_BUTTON).forEach(function (button) {
            button.addEventListener(CONSTANTS.EVENT_CLICK, function () {
                _closeNavigation(shell, openButton, sidebar, CONSTANTS.OPEN);
            });
        });

        document.querySelectorAll(SELECTORS.SIDEBAR_LINK).forEach(function (link) {
            link.addEventListener(CONSTANTS.EVENT_CLICK, function () {
                _closeNavigation(shell, openButton, sidebar, CONSTANTS.CLOSED);
            });
        });

        openButton.addEventListener(CONSTANTS.EVENT_CLICK, function () {
            _setNavigation(shell, openButton, sidebar, CONSTANTS.OPEN);
        });
        document.addEventListener(CONSTANTS.EVENT_KEYDOWN, function (event) {
            if (event.key === CONSTANTS.ESCAPE_KEY && shell.classList.contains(CSS_CLASSES.NAVIGATION_OPEN)) {
                _closeNavigation(shell, openButton, sidebar, CONSTANTS.OPEN);
            }
        });
        window.addEventListener(CONSTANTS.EVENT_RESIZE, function () {
            _setNavigation(shell, openButton, sidebar, shell.classList.contains(CSS_CLASSES.NAVIGATION_OPEN));
        });
    }

    _initialize();
}());
