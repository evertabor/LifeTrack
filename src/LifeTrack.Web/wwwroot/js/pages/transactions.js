(function () {
    "use strict";

    const SELECTORS = {
        CREATE_MODAL: "[data-transactions-create-modal]",
        BUDGET_MODAL: "[data-transactions-budget-modal]",
        TYPE_FIELD: "[data-transactions-type]",
        CATEGORY_FIELD: "[data-transactions-category]",
        KIND_KEY: "transactionsKind",
        VALIDATION_ERROR: ".field-validation-error",
        BUDGET_FILL: "[data-transactions-percent]"
    };


    const CONSTANTS = {
        EMPTY: "",
        EVENT_CHANGE: "change",
        DATA_PERCENT: "data-transactions-percent",
        PERCENT_SUFFIX: "%",
        KIND_INCOME: "income",
        KIND_EXPENSE: "expense",
        TYPE_INCOME_VALUE: "true"
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

    function _filterCategoriesByType(form) {
        const typeField = form.querySelector(SELECTORS.TYPE_FIELD);
        const categoryField = form.querySelector(SELECTORS.CATEGORY_FIELD);

        if (!typeField || !categoryField) {
            return;
        }

        const kind = typeField.value === CONSTANTS.TYPE_INCOME_VALUE
            ? CONSTANTS.KIND_INCOME
            : CONSTANTS.KIND_EXPENSE;

        for (let index = 0; index < categoryField.options.length; index++) {
            const option = categoryField.options[index];

            if (option.value === CONSTANTS.EMPTY) {
                option.hidden = false;
                continue;
            }

            option.hidden = option.dataset[SELECTORS.KIND_KEY] !== kind;
        }

        if (categoryField.selectedOptions.length > 0 &&
            categoryField.selectedOptions[0].hidden) {
            categoryField.value = CONSTANTS.EMPTY;
        }
    }

    function _filterAllForms() {
        const typeFields = document.querySelectorAll(SELECTORS.TYPE_FIELD);

        for (let index = 0; index < typeFields.length; index++) {
            if (typeFields[index].form) {
                _filterCategoriesByType(typeFields[index].form);
            }
        }
    }

    function _handleChange(event) {
        if (event.target.matches(SELECTORS.TYPE_FIELD) && event.target.form) {
            _filterCategoriesByType(event.target.form);
        }
    }

    function _initialize() {
        _renderBudgetBars();
        _filterAllForms();
        _openModalWithErrors(SELECTORS.CREATE_MODAL);
        _openModalWithErrors(SELECTORS.BUDGET_MODAL);
        document.addEventListener(CONSTANTS.EVENT_CHANGE, _handleChange);
    }

    _initialize();
}());
