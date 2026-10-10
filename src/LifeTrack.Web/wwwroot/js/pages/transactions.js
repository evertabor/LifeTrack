const transactionsPageScript = (function () {
    "use strict";

    const SELECTORS = {
        CREATE_MODAL: "[data-transactions-create-modal]",
        VALIDATION_ERROR: ".field-validation-error"
    };

    const CSS_CLASSES = {};

    const CONSTANTS = {
        EMPTY: ""
    };

    function _hasErrors(modal) {
        const errors = modal.querySelectorAll(SELECTORS.VALIDATION_ERROR);

        for (let index = 0; index < errors.length; index++) {
            if (errors[index].textContent.trim() !== CONSTANTS.EMPTY) {
                return true;
            }
        }

        return false;
    }

    function _initialize() {
        const modal = document.querySelector(SELECTORS.CREATE_MODAL);

        if (!modal) {
            return;
        }

        if (_hasErrors(modal)) {
            Modal.open(modal.id);
        }
    }

    _initialize();
}());
