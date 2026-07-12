(function () {
    function getSeparator(input) {
        return input.getAttribute('data-date-separator') || '/';
    }

    function escapeRegex(value) {
        return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    function padDatePart(value) {
        return value.length === 1 ? '0' + value : value;
    }

    function formatDigits(value, separator) {
        var digits = (value || '').replace(/\D/g, '').substring(0, 8);
        if (digits.length <= 2) {
            return digits;
        }

        if (digits.length <= 4) {
            return digits.substring(0, 2) + separator + digits.substring(2);
        }

        return digits.substring(0, 2) + separator + digits.substring(2, 4) + separator + digits.substring(4);
    }

    function formatDisplayDate(value, separator) {
        if (!/^\d{4}-\d{2}-\d{2}$/.test(value || '')) {
            return '';
        }

        var parts = value.split('-');
        return parts[2] + separator + parts[1] + separator + parts[0];
    }

    function isLeapYear(year) {
        return year % 400 === 0 || (year % 4 === 0 && year % 100 !== 0);
    }

    function getDaysInMonth(month, year) {
        var days = [31, isLeapYear(year) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        return days[month - 1] || 0;
    }

    function parseDisplayDate(value, separator) {
        var expression = new RegExp('^(\\d{1,2})' + escapeRegex(separator) + '(\\d{1,2})' + escapeRegex(separator) + '(\\d{4})$');
        var match = expression.exec((value || '').trim());
        if (!match) {
            return { isValid: false, isoValue: '' };
        }

        var day = parseInt(match[1], 10);
        var month = parseInt(match[2], 10);
        var year = parseInt(match[3], 10);
        var maxDay = getDaysInMonth(month, year);

        if (year < 1 || month < 1 || month > 12 || day < 1 || day > maxDay) {
            return { isValid: false, isoValue: '' };
        }

        return {
            isValid: true,
            isoValue: match[3] + '-' + padDatePart(match[2]) + '-' + padDatePart(match[1])
        };
    }

    function getInvalidMessage(input) {
        return input.getAttribute('data-date-invalid-message') || 'Ngay khong hop le. Vui long nhap dung ngay theo lich.';
    }

    function validateInput(input) {
        var value = (input.value || '').trim();
        var separator = getSeparator(input);

        if (!value) {
            input.setCustomValidity('');
            return true;
        }

        var parsed = parseDisplayDate(value, separator);
        if (!parsed.isValid) {
            input.setCustomValidity(getInvalidMessage(input));
            return false;
        }

        input.setCustomValidity('');
        return true;
    }

    function syncPicker(input) {
        var picker = document.getElementById(input.getAttribute('data-date-picker-id'));
        if (!picker) {
            return;
        }

        var parsed = parseDisplayDate(input.value, getSeparator(input));
        picker.value = parsed.isValid ? parsed.isoValue : '';
    }

    function openPickerForInput(input) {
        var picker = document.getElementById(input.getAttribute('data-date-picker-id'));
        if (!picker) {
            return;
        }

        syncPicker(input);
        picker.focus();

        if (typeof picker.showPicker === 'function') {
            picker.showPicker();
        } else {
            picker.click();
        }
    }

    function wireInput(input) {
        if (input.getAttribute('data-date-input-ready') === 'true') {
            return;
        }

        input.setAttribute('data-date-input-ready', 'true');
        input.addEventListener('input', function () {
            input.value = formatDigits(input.value, getSeparator(input));
            validateInput(input);
            syncPicker(input);
        });

        input.addEventListener('blur', function () {
            input.value = formatDigits(input.value, getSeparator(input));
            validateInput(input);
            syncPicker(input);
        });

        if (input.form) {
            input.form.addEventListener('submit', function () {
                validateInput(input);
                syncPicker(input);
            });
        }
    }

    function wirePicker(picker) {
        if (picker.getAttribute('data-date-picker-ready') === 'true') {
            return;
        }

        picker.setAttribute('data-date-picker-ready', 'true');
        picker.addEventListener('change', function () {
            var input = document.getElementById(picker.getAttribute('data-date-display-id'));
            if (input) {
                input.value = formatDisplayDate(picker.value, getSeparator(input));
                validateInput(input);
            }
        });
    }

    function wireTrigger(trigger) {
        if (trigger.getAttribute('data-date-trigger-ready') === 'true') {
            return;
        }

        trigger.setAttribute('data-date-trigger-ready', 'true');
        trigger.addEventListener('click', function () {
            var picker = document.getElementById(trigger.getAttribute('data-date-picker-trigger'));
            var input = picker ? document.getElementById(picker.getAttribute('data-date-display-id')) : null;
            if (input) {
                openPickerForInput(input);
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-date-input]').forEach(wireInput);
        document.querySelectorAll('[data-date-picker]').forEach(wirePicker);
        document.querySelectorAll('[data-date-picker-trigger]').forEach(wireTrigger);
    });
})();
