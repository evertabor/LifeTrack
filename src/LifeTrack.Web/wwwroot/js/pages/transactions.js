(function () {
    "use strict";

    const SELECTORS = {
        CREATE_MODAL: "[data-transactions-create-modal]",
        BUDGET_MODAL: "[data-transactions-budget-modal]",
        VALIDATION_ERROR: ".field-validation-error",
        BUDGET_FILL: "[data-transactions-percent]"
    };

    const CSS_CLASSES = {};

    const CONSTANTS = {
        EMPTY: "",
        DATA_PERCENT: "data-transactions-percent",
        PERCENT_SUFFIX: "%"
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

    function _renderBudgetBars() {
        const fills = document.querySelectorAll(SELECTORS.BUDGET_FILL);

        for (let index = 0; index < fills.length; index++) {
            const percent = Number(fills[index].getAttribute(CONSTANTS.DATA_PERCENT));

            if (Number.isFinite(percent)) {
                fills[index].style.width = percent + CONSTANTS.PERCENT_SUFFIX;
            }
        }
    }

    function _openModalWithErrors(selector) {
        const modal = document.querySelector(selector);

        if (modal && _hasErrors(modal)) {
            Modal.open(modal.id);
        }
    }

    function _initialize() {
        _renderBudgetBars();
        _openModalWithErrors(SELECTORS.CREATE_MODAL);
        _openModalWithErrors(SELECTORS.BUDGET_MODAL);
    }

    _initialize();
}());
