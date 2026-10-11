(function () {
    "use strict";

    const SELECTORS = {
        CREATE_MODAL: "[data-categories-create-modal]",
        VALIDATION_ERROR: ".field-validation-error"
    };

    const CSS_CLASSES = {};

    const CONSTANTS = {
        EMPTY: ""
    };

    function _initialize() {
        const modal = document.querySelector(SELECTORS.CREATE_MODAL);

        if (!modal) {
            return;
        }

        const validationError = modal.querySelector(SELECTORS.VALIDATION_ERROR);

        if (validationError && validationError.textContent.trim() !== CONSTANTS.EMPTY) {
            Modal.open(modal.id);
        }
    }

    _initialize();
}());
