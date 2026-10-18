(function () {
    "use strict";

    const SELECTORS = {
        SUBMIT_BUTTON: "button[type='submit']",
        SUBMITTING_FORM: "form[data-app-submitting]",
        LOADING_BUTTON: "button[data-app-submit-loading]",
        ORIGINAL_CONTENT: "[data-app-submit-content]",
        SUBMITTING_DATA: "appSubmitting",
        LOADING_BUTTON_DATA: "appSubmitLoading",
        ORIGINAL_CONTENT_DATA: "appSubmitContent",
        ORIGINAL_ARIA_LABEL_DATA: "appSubmitOriginalAriaLabel",
        ORIGINAL_ARIA_LABEL_PRESENT_DATA: "appSubmitOriginalAriaLabelPresent",
        ARIA_BUSY: "aria-busy",
        ARIA_DISABLED: "aria-disabled",
        ARIA_HIDDEN: "aria-hidden",
        ARIA_LABEL: "aria-label"
    };

    const CSS_CLASSES = {
        SPINNER: "spinner-border spinner-border-sm me-2"
    };

    const CONSTANTS = {
        EVENT_SUBMIT: "submit",
        EVENT_PAGESHOW: "pageshow",
        POST_METHOD: "post",
        TRUE: "true",
        FALSE: "false",
        LOADING_TEXT: "Cargando...",
        EMPTY: "",
        HIDDEN: true,
        ENABLED: false,
        DISABLE_DELAY_MS: 0
    };

    function _handleSubmit(event) {
        const form = event.target;

        if (event.defaultPrevented || !(form instanceof HTMLFormElement) || form.method !== CONSTANTS.POST_METHOD) {
            return;
        }

        if (form.dataset[SELECTORS.SUBMITTING_DATA] === CONSTANTS.TRUE) {
            event.preventDefault();
            return;
        }

        const button = event.submitter instanceof HTMLButtonElement
            ? event.submitter
            : form.querySelector(SELECTORS.SUBMIT_BUTTON);

        if (!(button instanceof HTMLButtonElement) || button.disabled) {
            return;
        }

        _setLoadingState(form, button);
    }

    function _setLoadingState(form, button) {
        form.dataset[SELECTORS.SUBMITTING_DATA] = CONSTANTS.TRUE;
        form.setAttribute(SELECTORS.ARIA_BUSY, CONSTANTS.TRUE);
        button.dataset[SELECTORS.LOADING_BUTTON_DATA] = CONSTANTS.TRUE;
        button.dataset[SELECTORS.ORIGINAL_ARIA_LABEL_PRESENT_DATA] = button.hasAttribute(SELECTORS.ARIA_LABEL)
            ? CONSTANTS.TRUE
            : CONSTANTS.FALSE;
        button.dataset[SELECTORS.ORIGINAL_ARIA_LABEL_DATA] = button.getAttribute(SELECTORS.ARIA_LABEL) ?? CONSTANTS.EMPTY;
        button.setAttribute(SELECTORS.ARIA_DISABLED, CONSTANTS.TRUE);
        button.setAttribute(SELECTORS.ARIA_LABEL, CONSTANTS.LOADING_TEXT);
        _setLoadingContent(button);

        window.setTimeout(function () {
            button.disabled = CONSTANTS.HIDDEN;
        }, CONSTANTS.DISABLE_DELAY_MS);
    }

    function _setLoadingContent(button) {
        const originalContent = _preserveButtonContent(button);
        button.replaceChildren(originalContent, _createSpinner(), _createLoadingLabel());
    }

    function _preserveButtonContent(button) {
        const originalContent = document.createElement("span");
        originalContent.dataset[SELECTORS.ORIGINAL_CONTENT_DATA] = CONSTANTS.TRUE;
        originalContent.hidden = CONSTANTS.HIDDEN;

        while (button.firstChild) {
            originalContent.appendChild(button.firstChild);
        }

        return originalContent;
    }

    function _createSpinner() {
        const spinner = document.createElement("span");
        spinner.className = CSS_CLASSES.SPINNER;
        spinner.setAttribute(SELECTORS.ARIA_HIDDEN, CONSTANTS.TRUE);
        return spinner;
    }

    function _createLoadingLabel() {
        const label = document.createElement("span");
        label.textContent = CONSTANTS.LOADING_TEXT;
        return label;
    }

    function _restoreLoadingState() {
        document.querySelectorAll(SELECTORS.SUBMITTING_FORM).forEach(function (form) {
            form.removeAttribute(SELECTORS.ARIA_BUSY);
            delete form.dataset[SELECTORS.SUBMITTING_DATA];

            const button = form.querySelector(SELECTORS.LOADING_BUTTON);

            if (!button) {
                return;
            }

            _restoreButton(button);
        });
    }

    function _restoreButton(button) {
        const originalContent = button.querySelector(SELECTORS.ORIGINAL_CONTENT);

        if (!originalContent) {
            return;
        }

        button.disabled = CONSTANTS.ENABLED;
        button.removeAttribute(SELECTORS.ARIA_DISABLED);
        button.replaceChildren(...originalContent.childNodes);
        _restoreAriaLabel(button);
        delete button.dataset[SELECTORS.LOADING_BUTTON_DATA];
    }

    function _restoreAriaLabel(button) {
        if (button.dataset[SELECTORS.ORIGINAL_ARIA_LABEL_PRESENT_DATA] === CONSTANTS.TRUE) {
            button.setAttribute(SELECTORS.ARIA_LABEL, button.dataset[SELECTORS.ORIGINAL_ARIA_LABEL_DATA]);
        } else {
            button.removeAttribute(SELECTORS.ARIA_LABEL);
        }

        delete button.dataset[SELECTORS.ORIGINAL_ARIA_LABEL_DATA];
        delete button.dataset[SELECTORS.ORIGINAL_ARIA_LABEL_PRESENT_DATA];
    }

    document.addEventListener(CONSTANTS.EVENT_SUBMIT, _handleSubmit);
    window.addEventListener(CONSTANTS.EVENT_PAGESHOW, _restoreLoadingState);
}());
