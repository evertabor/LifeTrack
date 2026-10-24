(function () {
    "use strict";

    const SELECTORS = {
        CREATE_MODAL: "[data-categories-create-modal]",
        SEARCH_FORM: "[data-categories-search]",
        SEARCH_INPUT: "[data-categories-search-input]",
        SEARCH_CLEAR: "[data-categories-search-clear]",
        TYPE_FILTER: "[data-categories-type-filter]",
        LIST: "[data-categories-list]",
        ROW: "[data-categories-row]",
        NAME: "[data-categories-name]",
        NO_RESULTS: "[data-categories-no-results]",
        VALIDATION_ERROR: ".field-validation-error",
        CATEGORY_TYPE_ATTRIBUTE: "data-category-type"
    };

    const CSS_CLASSES = {};

    const CONSTANTS = {
        EMPTY: "",
        NULL: null,
        ZERO: 0,
        EVENT_INPUT: "input",
        EVENT_SUBMIT: "submit",
        EVENT_CHANGE: "change",
        EVENT_CLICK: "click",
        DECIMAL_RADIX: 10,
        NORMALIZE_FORM: "NFD",
        DIACRITICS_PATTERN: /\p{Diacritic}/gu
    };

    function _normalize(text) {
        return text
            .trim()
            .toLowerCase()
            .normalize(CONSTANTS.NORMALIZE_FORM)
            .replace(CONSTANTS.DIACRITICS_PATTERN, CONSTANTS.EMPTY);
    }

    function _filterCategories() {
        const input = document.querySelector(SELECTORS.SEARCH_INPUT);
        const typeFilter = document.querySelector(SELECTORS.TYPE_FILTER);
        const list = document.querySelector(SELECTORS.LIST);
        const message = document.querySelector(SELECTORS.NO_RESULTS);

        if (!input || !list || !typeFilter) {
            return;
        }

        const term = _normalize(input.value);
        const typeValue = parseInt(typeFilter.value, CONSTANTS.DECIMAL_RADIX);
        const rows = list.querySelectorAll(SELECTORS.ROW);
        let visible = CONSTANTS.ZERO;

        for (let index = CONSTANTS.ZERO; index < rows.length; index++) {
            const name = rows[index].querySelector(SELECTORS.NAME);
            const typeAttr = rows[index].getAttribute(SELECTORS.CATEGORY_TYPE_ATTRIBUTE);
            const categoryType = typeAttr ? parseInt(typeAttr, CONSTANTS.DECIMAL_RADIX) : CONSTANTS.NULL;

            const nameMatches = term === CONSTANTS.EMPTY ||
                (name && _normalize(name.textContent).includes(term));

            const typeMatches = typeValue === CONSTANTS.ZERO ||
                (categoryType !== CONSTANTS.NULL && categoryType === typeValue);

            const matches = nameMatches && typeMatches;
            rows[index].hidden = !matches;

            if (matches) {
                visible++;
            }
        }

        list.hidden = visible === CONSTANTS.ZERO;

        if (message) {
            message.hidden = visible !== CONSTANTS.ZERO;
        }
    }

    function _handleInput(event) {
        if (event.target.matches(SELECTORS.SEARCH_INPUT)) {
            _filterCategories();
        }
    }

    function _handleTypeChange(event) {
        if (event.target.matches(SELECTORS.TYPE_FILTER)) {
            _filterCategories();
        }
    }

    function _handleSearchClear(event) {
        if (event.target.matches(SELECTORS.SEARCH_CLEAR)) {
            const input = document.querySelector(SELECTORS.SEARCH_INPUT);
            if (input) {
                input.value = CONSTANTS.EMPTY;
                input.focus();
                _filterCategories();
            }
        }
    }

    function _handleSubmit(event) {
        if (event.target.matches(SELECTORS.SEARCH_FORM)) {
            event.preventDefault();
            _filterCategories();
        }
    }

    function _openCreateModalOnErrors() {
        const modal = document.querySelector(SELECTORS.CREATE_MODAL);

        if (!modal) {
            return;
        }

        const validationError = modal.querySelector(SELECTORS.VALIDATION_ERROR);

        if (validationError && validationError.textContent.trim() !== CONSTANTS.EMPTY) {
            Modal.open(modal.id);
        }
    }

    function _initialize() {
        _filterCategories();
        _openCreateModalOnErrors();
        document.addEventListener(CONSTANTS.EVENT_INPUT, _handleInput);
        document.addEventListener(CONSTANTS.EVENT_SUBMIT, _handleSubmit);
        document.addEventListener(CONSTANTS.EVENT_CHANGE, _handleTypeChange);
        document.addEventListener(CONSTANTS.EVENT_CLICK, _handleSearchClear);
    }

    _initialize();
}());
