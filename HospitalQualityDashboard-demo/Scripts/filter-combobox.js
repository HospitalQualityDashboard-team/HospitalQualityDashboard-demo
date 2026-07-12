(function () {
    var activeCombobox = null;

    function normalize(value) {
        var text = (value || '').toLowerCase();
        if (typeof text.normalize === 'function') {
            text = text.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        }

        return text.replace(/đ/g, 'd');
    }

    function closeActiveCombobox() {
        if (activeCombobox) {
            activeCombobox.classList.remove('is-open');
            activeCombobox = null;
        }
    }

    function createShell(input, clearLabel) {
        var wrapper = document.createElement('div');
        var clear = document.createElement('button');
        var list = document.createElement('div');

        wrapper.className = 'report-filter-combobox';
        input.classList.add('report-filter-input');
        input.autocomplete = 'off';
        input.setAttribute('aria-haspopup', 'listbox');

        clear.type = 'button';
        clear.className = 'report-filter-combobox-clear';
        clear.setAttribute('aria-label', clearLabel || 'Clear selection');
        clear.textContent = 'x';

        list.className = 'report-filter-options';
        list.setAttribute('role', 'listbox');

        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);
        wrapper.appendChild(clear);
        wrapper.appendChild(list);

        return {
            wrapper: wrapper,
            input: input,
            clear: clear,
            list: list
        };
    }

    function wireCombobox(shell, options, getCurrentValue, setValue, clearValue) {
        function renderOptions() {
            var term = normalize(shell.input.value);
            var currentValue = getCurrentValue();

            shell.list.innerHTML = '';
            options.filter(function (option) {
                return !term || normalize(option.text).indexOf(term) > -1;
            }).forEach(function (option) {
                var item = document.createElement('button');
                item.type = 'button';
                item.className = 'report-filter-option' + (option.value === currentValue ? ' is-selected' : '');
                item.textContent = option.text;
                item.title = option.text;
                item.setAttribute('role', 'option');
                item.addEventListener('mousedown', function (event) {
                    event.preventDefault();
                    setValue(option.value, option.text);
                    closeActiveCombobox();
                });
                shell.list.appendChild(item);
            });
        }

        function openCombobox() {
            if (activeCombobox && activeCombobox !== shell.wrapper) {
                closeActiveCombobox();
            }

            activeCombobox = shell.wrapper;
            renderOptions();
            shell.wrapper.classList.add('is-open');
        }

        shell.input.addEventListener('focus', openCombobox);
        shell.input.addEventListener('click', openCombobox);
        shell.input.addEventListener('input', function () {
            clearValue(false);
            openCombobox();
        });
        shell.input.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeActiveCombobox();
            }
        });
        shell.input.addEventListener('blur', function () {
            window.setTimeout(function () {
                if (!shell.wrapper.contains(document.activeElement)) {
                    closeActiveCombobox();
                }
            }, 120);
        });
        shell.clear.addEventListener('click', function () {
            clearValue(true);
            shell.input.focus();
        });
    }

    function enhanceSelect(select) {
        if (select.getAttribute('data-filter-combobox-ready') === 'true') {
            return;
        }

        select.setAttribute('data-filter-combobox-ready', 'true');

        var placeholder = select.getAttribute('data-filter-placeholder') || '';
        var options = Array.prototype.slice.call(select.options).map(function (option) {
            return { value: option.value, text: option.text };
        });
        var selected = options.filter(function (option) {
            return option.value === select.value;
        })[0];

        var input = document.createElement('input');
        input.type = 'text';
        input.className = 'form-control';
        input.placeholder = placeholder;
        input.value = select.value && selected ? selected.text : '';

        select.classList.add('d-none');
        select.insertAdjacentElement('afterend', input);

        var shell = createShell(input, 'Xoa lua chon');

        wireCombobox(
            shell,
            options,
            function () { return select.value; },
            function (value, text) {
                select.value = value;
                shell.input.value = value ? text : '';
            },
            function (clearText) {
                select.value = '';
                if (clearText) {
                    shell.input.value = '';
                }
            });
    }

    function getTextOptions(input) {
        var sourceId = input.getAttribute('data-filter-options-source');
        var source = sourceId ? document.getElementById(sourceId) : null;

        if (!source) {
            return [];
        }

        return Array.prototype.slice.call(source.querySelectorAll('[data-filter-option]')).map(function (option) {
            var text = option.getAttribute('data-text') || option.textContent || '';
            return { value: text, text: text };
        });
    }

    function enhanceTextInput(input) {
        if (input.getAttribute('data-filter-combobox-ready') === 'true') {
            return;
        }

        input.setAttribute('data-filter-combobox-ready', 'true');

        var options = getTextOptions(input);
        var shell = createShell(input, 'Xoa tim kiem');

        wireCombobox(
            shell,
            options,
            function () { return shell.input.value; },
            function (value, text) {
                shell.input.value = text || value || '';
            },
            function (clearText) {
                if (clearText) {
                    shell.input.value = '';
                }
            });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('select[data-filter-combobox]').forEach(enhanceSelect);
        document.querySelectorAll('input[data-filter-text-combobox]').forEach(enhanceTextInput);
        document.addEventListener('click', function (event) {
            if (activeCombobox && !activeCombobox.contains(event.target)) {
                closeActiveCombobox();
            }
        });
    });
})();
