(function () {
    "use strict";

    const SELECTORS = {
        SHELL: "[data-app-shell]",
        OPEN_BUTTON: "[data-app-nav-open]",
        CLOSE_BUTTON: "[data-app-nav-close]",
        SIDEBAR: "[data-app-sidebar]",
        SIDEBAR_LINK: "[data-app-sidebar] a"
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
        CLOSED: false
    };

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
