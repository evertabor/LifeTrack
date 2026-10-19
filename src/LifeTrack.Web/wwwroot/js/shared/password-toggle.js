(function () {
    "use strict";

    const SELECTORS = {
        PASSWORD_FIELD: "[data-password-field]",
        PASSWORD_TOGGLE: "[data-password-toggle]",
        PASSWORD_INPUT: "[data-password-input]",
        TOGGLE_ICON_SHOW: "[data-password-toggle-show]",
        TOGGLE_ICON_HIDE: "[data-password-toggle-hide]"
    };

    const CSS_CLASSES = {
        PASSWORD_FIELD: "app-password-field",
        PASSWORD_INPUT: "form-control",
        PASSWORD_TOGGLE: "app-password-toggle"
    };

    const CONSTANTS = {
        EVENT_CLICK: "click",
        INPUT_TYPE_PASSWORD: "password",
        INPUT_TYPE_TEXT: "text",
        ARIA_PRESSED: "aria-pressed",
        ARIA_LABEL: "aria-label",
        SHOW_PASSWORD_LABEL: "Mostrar contraseña",
        HIDE_PASSWORD_LABEL: "Ocultar contraseña",
        HIDDEN_ATTRIBUTE: "hidden",
        TRUE: "true",
        FALSE: "false"
    };

    function _synchronizeToggle(toggle, input, showIcon, hideIcon) {
        const isPasswordHidden = input.type === CONSTANTS.INPUT_TYPE_PASSWORD;
        showIcon.toggleAttribute(CONSTANTS.HIDDEN_ATTRIBUTE, isPasswordHidden);
        hideIcon.toggleAttribute(CONSTANTS.HIDDEN_ATTRIBUTE, !isPasswordHidden);
        toggle.setAttribute(CONSTANTS.ARIA_PRESSED, isPasswordHidden ? CONSTANTS.FALSE : CONSTANTS.TRUE);
        toggle.setAttribute(CONSTANTS.ARIA_LABEL, isPasswordHidden ? CONSTANTS.SHOW_PASSWORD_LABEL : CONSTANTS.HIDE_PASSWORD_LABEL);
    }

    function _initializeToggle(toggle) {
        const field = toggle.closest(SELECTORS.PASSWORD_FIELD);
        const input = field?.querySelector(SELECTORS.PASSWORD_INPUT);
        const showIcon = toggle.querySelector(SELECTORS.TOGGLE_ICON_SHOW);
        const hideIcon = toggle.querySelector(SELECTORS.TOGGLE_ICON_HIDE);

        if (!input || !showIcon || !hideIcon) {
            return;
        }

        _synchronizeToggle(toggle, input, showIcon, hideIcon);

        toggle.addEventListener(CONSTANTS.EVENT_CLICK, function () {
            const isPasswordHidden = input.type === CONSTANTS.INPUT_TYPE_PASSWORD;
            input.type = isPasswordHidden ? CONSTANTS.INPUT_TYPE_TEXT : CONSTANTS.INPUT_TYPE_PASSWORD;
            _synchronizeToggle(toggle, input, showIcon, hideIcon);
        });
    }

    function _initialize() {
        const toggles = document.querySelectorAll(SELECTORS.PASSWORD_TOGGLE);

        for (let index = 0; index < toggles.length; index++) {
            _initializeToggle(toggles[index]);
        }
    }

    _initialize();
}());
