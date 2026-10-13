(function () {
    "use strict";

    const SELECTORS = {
        MONEY_FIELD: "input[data-app-money]"
    };

    const CSS_CLASSES = {};

    const CONSTANTS = {
        EVENT_FOCUSIN: "focusin",
        EVENT_FOCUSOUT: "focusout",
        EVENT_INPUT: "input",
        EVENT_SUBMIT: "submit",
        LOCALE: "es-CO",
        FRACTION_DIGITS: 2,
        GROUP_SEPARATOR: ".",
        DECIMAL_SEPARATOR: ",",
        PLAIN_SEPARATOR: ".",
        EMPTY: "",
        ZERO: "0",
        NINE: "9",
        TYPE_NUMBER: "number"
    };

    function _isDigit(character) {
        return character >= CONSTANTS.ZERO && character <= CONSTANTS.NINE;
    }

    function _digitsOnly(text) {
        let digits = CONSTANTS.EMPTY;

        for (const character of text) {
            if (_isDigit(character)) {
                digits += character;
            }
        }

        return digits;
    }

    function _sanitize(text) {
        const commaIndex = text.indexOf(CONSTANTS.DECIMAL_SEPARATOR);

        if (commaIndex !== -1) {
            const integers = _digitsOnly(text.slice(0, commaIndex));
            const decimals = _digitsOnly(text.slice(commaIndex + 1)).slice(0, CONSTANTS.FRACTION_DIGITS);

            return integers + CONSTANTS.DECIMAL_SEPARATOR + decimals;
        }

        const dotCount = text.split(CONSTANTS.GROUP_SEPARATOR).length - 1;

        if (dotCount === 1) {
            const lastDot = text.lastIndexOf(CONSTANTS.GROUP_SEPARATOR);
            const after = _digitsOnly(text.slice(lastDot + 1));

            if (after.length >= 1 && after.length <= CONSTANTS.FRACTION_DIGITS) {
                return _digitsOnly(text.slice(0, lastDot)) + CONSTANTS.DECIMAL_SEPARATOR + after;
            }
        }

        return _digitsOnly(text);
    }

    function _toEditable(text) {
        if (text.indexOf(CONSTANTS.DECIMAL_SEPARATOR) === -1) {
            return text.replace(CONSTANTS.PLAIN_SEPARATOR, CONSTANTS.DECIMAL_SEPARATOR);
        }

        return text.split(CONSTANTS.GROUP_SEPARATOR).join(CONSTANTS.EMPTY);
    }

    function _parseMasked(text) {
        if (text === CONSTANTS.EMPTY) {
            return null;
        }

        const plain = text
            .split(CONSTANTS.GROUP_SEPARATOR).join(CONSTANTS.EMPTY)
            .replace(CONSTANTS.DECIMAL_SEPARATOR, CONSTANTS.PLAIN_SEPARATOR);
        const value = Number(plain);

        return Number.isFinite(value) ? value : null;
    }

    function _formatMoney(value) {
        return value.toLocaleString(CONSTANTS.LOCALE, {
            minimumFractionDigits: CONSTANTS.FRACTION_DIGITS,
            maximumFractionDigits: CONSTANTS.FRACTION_DIGITS
        });
    }

    function _formatIntegers(digits) {
        if (digits === CONSTANTS.EMPTY) {
            return CONSTANTS.EMPTY;
        }

        return Number(digits).toLocaleString(CONSTANTS.LOCALE, { maximumFractionDigits: 0 });
    }

    function _formatLive(sanitized) {
        const separatorIndex = sanitized.indexOf(CONSTANTS.DECIMAL_SEPARATOR);

        if (separatorIndex === -1) {
            return _formatIntegers(sanitized);
        }

        const integers = sanitized.slice(0, separatorIndex);
        const decimals = sanitized.slice(separatorIndex + 1);
        const grouped = integers === CONSTANTS.EMPTY ? CONSTANTS.ZERO : _formatIntegers(integers);

        return grouped + CONSTANTS.DECIMAL_SEPARATOR + decimals;
    }

    function _countDigits(text) {
        let count = 0;

        for (const character of text) {
            if (_isDigit(character)) {
                count++;
            }
        }

        return count;
    }

    function _caretForDigits(text, digits) {
        const total = _countDigits(text);

        if (digits >= total) {
            return text.length;
        }

        let seen = 0;

        for (let index = 0; index < text.length; index++) {
            if (_isDigit(text[index])) {
                seen++;

                if (seen === digits) {
                    return index + 1;
                }
            }
        }

        return text.length;
    }

    function _matches(field) {
        return Boolean(field && field.matches && field.matches(SELECTORS.MONEY_FIELD));
    }

    function _handleFocusIn(event) {
        const field = event.target;

        if (!_matches(field)) {
            return;
        }

        field.value = _toEditable(field.value);
    }

    function _handleFocusOut(event) {
        const field = event.target;

        if (!_matches(field)) {
            return;
        }

        const value = _parseMasked(_sanitize(field.value));

        if (value === null) {
            return;
        }

        field.value = _formatMoney(value);
    }

    function _handleInput(event) {
        const field = event.target;

        if (!_matches(field)) {
            return;
        }

        const raw = field.value;
        const caret = typeof field.selectionStart === CONSTANTS.TYPE_NUMBER ? field.selectionStart : raw.length;
        const sanitized = _sanitize(raw);

        if (sanitized === CONSTANTS.EMPTY) {
            if (raw !== CONSTANTS.EMPTY) {
                field.value = CONSTANTS.EMPTY;
            }

            return;
        }

        const digitsBefore = _countDigits(raw.slice(0, caret));
        const formatted = _formatLive(sanitized);

        field.value = formatted;

        const position = _caretForDigits(formatted, digitsBefore);
        field.setSelectionRange(position, position);
    }

    function _handleSubmit(event) {
        const form = event.target;

        if (!form || !form.querySelectorAll) {
            return;
        }

        const fields = form.querySelectorAll(SELECTORS.MONEY_FIELD);

        for (let index = 0; index < fields.length; index++) {
            const value = _parseMasked(fields[index].value);

            if (value !== null) {
                fields[index].value = String(value);
            }
        }
    }

    document.addEventListener(CONSTANTS.EVENT_FOCUSIN, _handleFocusIn);
    document.addEventListener(CONSTANTS.EVENT_FOCUSOUT, _handleFocusOut);
    document.addEventListener(CONSTANTS.EVENT_INPUT, _handleInput);
    document.addEventListener(CONSTANTS.EVENT_SUBMIT, _handleSubmit);
}());
