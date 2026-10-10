const formValidationComponent = (function () {
    "use strict";

    const SELECTORS = {
        VALIDATED_FORM: "form[data-app-validate]",
        VALIDATION_MESSAGE_PREFIX: "[data-valmsg-for='",
        VALIDATION_MESSAGE_SUFFIX: "']"
    };

    const CSS_CLASSES = {};

    const CONSTANTS = {
        EVENT_INPUT: "input",
        EVENT_SUBMIT: "submit",
        FUNCTION_TYPE: "function",
        TYPE_HIDDEN: "hidden",
        ARIA_INVALID: "aria-invalid",
        TRUE: "true",
        FALSE: "false",
        EMPTY: "",
        NULL: null,
        VALID: true,
        INVALID: false,
        REQUIRED_MESSAGE: "Este campo es obligatorio.",
        EMAIL_MESSAGE: "Introduce un correo electrónico válido.",
        MIN_LENGTH_MESSAGE_PREFIX: "Introduce al menos ",
        MIN_LENGTH_MESSAGE_SUFFIX: " caracteres.",
        MAX_LENGTH_MESSAGE_PREFIX: "Introduce como máximo ",
        MAX_LENGTH_MESSAGE_SUFFIX: " caracteres.",
        DEFAULT_MESSAGE: "Revisa este campo."
    };

    function _getValidationMessage(field) {
        if (field.validity.valueMissing) {
            return CONSTANTS.REQUIRED_MESSAGE;
        }

        if (field.validity.typeMismatch) {
            return CONSTANTS.EMAIL_MESSAGE;
        }

        if (field.validity.tooShort) {
            return CONSTANTS.MIN_LENGTH_MESSAGE_PREFIX + field.minLength + CONSTANTS.MIN_LENGTH_MESSAGE_SUFFIX;
        }

        if (field.validity.tooLong) {
            return CONSTANTS.MAX_LENGTH_MESSAGE_PREFIX + field.maxLength + CONSTANTS.MAX_LENGTH_MESSAGE_SUFFIX;
        }

        return CONSTANTS.DEFAULT_MESSAGE;
    }

    function _getValidationMessageElement(form, field) {
        if (!field.name) {
            return CONSTANTS.NULL;
        }

        const selector = SELECTORS.VALIDATION_MESSAGE_PREFIX + CSS.escape(field.name) + SELECTORS.VALIDATION_MESSAGE_SUFFIX;
        return form.querySelector(selector);
    }

    function _clearFieldError(form, field) {
        const message = _getValidationMessageElement(form, field);

        field.setAttribute(CONSTANTS.ARIA_INVALID, CONSTANTS.FALSE);

        if (message) {
            message.textContent = CONSTANTS.EMPTY;
        }
    }

    function _showFieldError(form, field) {
        const message = _getValidationMessageElement(form, field);

        field.setAttribute(CONSTANTS.ARIA_INVALID, CONSTANTS.TRUE);

        if (message) {
            message.textContent = _getValidationMessage(field);
        }
    }

    function _getValidatedFields(form) {
        return Array.from(form.elements).filter(function (field) {
            return typeof field.checkValidity === CONSTANTS.FUNCTION_TYPE && !field.disabled && field.type !== CONSTANTS.TYPE_HIDDEN;
        });
    }

    function _validateForm(form) {
        let firstInvalidField = CONSTANTS.NULL;

        _getValidatedFields(form).forEach(function (field) {
            if (field.checkValidity()) {
                _clearFieldError(form, field);
                return;
            }

            _showFieldError(form, field);
            firstInvalidField ??= field;
        });

        if (!firstInvalidField) {
            return CONSTANTS.VALID;
        }

        firstInvalidField.focus();
        return CONSTANTS.INVALID;
    }

    function _handleInput(event) {
        const field = event.target;
        const form = field.form;

        if (!form || !form.matches(SELECTORS.VALIDATED_FORM) || typeof field.checkValidity !== CONSTANTS.FUNCTION_TYPE) {
            return;
        }

        if (field.checkValidity()) {
            _clearFieldError(form, field);
            return;
        }

        _showFieldError(form, field);
    }

    function _handleSubmit(event) {
        const form = event.target;

        if (!form.matches(SELECTORS.VALIDATED_FORM) || _validateForm(form)) {
            return;
        }

        event.preventDefault();
    }

    document.addEventListener(CONSTANTS.EVENT_INPUT, _handleInput);
    document.addEventListener(CONSTANTS.EVENT_SUBMIT, _handleSubmit);
}());
