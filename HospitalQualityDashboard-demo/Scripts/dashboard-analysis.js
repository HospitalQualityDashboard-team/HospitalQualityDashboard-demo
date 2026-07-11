function initializeDashboardAnalysis() {
    var tabs = document.querySelectorAll('[data-dashboard-tab]');
    var panes = document.querySelectorAll('[data-dashboard-pane]');

    function renderCharts(container) {
        container.querySelectorAll('.comparison-doughnut-chart').forEach(function (chart) {
            if (chart.dataset.rendered) return;
            var metric = JSON.parse(chart.dataset.metric || '{}');
            new Chart(chart, {
                type: 'doughnut',
                data: {
                    labels: ['Đúng hạn', 'Nộp trễ', 'Chưa nộp', 'Quá hạn chưa nộp'],
                    datasets: [{
                        data: [metric.DungHan || 0, metric.NopTre || 0, metric.ChuaNop || 0, metric.QuaHanChuaNop || 0],
                        backgroundColor: ['#22A06B', 'rgba(229, 72, 77, .74)', '#D8E2EA', '#E5484D'],
                        borderColor: '#FFFFFF',
                        borderWidth: 3
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '62%',
                    plugins: { legend: { position: 'bottom' } }
                }
            });
            chart.dataset.rendered = 'true';
        });
    }

    function loadPane(pane, query) {
        pane.innerHTML = '<div class="analysis-loading"><span class="spinner-border spinner-border-sm"></span> Đang tải dữ liệu...</div>';
        fetch(pane.dataset.dashboardUrl + (query ? '?' + query : ''), { credentials: 'same-origin' })
            .then(function (response) { if (!response.ok) throw new Error(response.statusText); return response.text(); })
            .then(function (html) { pane.innerHTML = html; bindPane(pane); })
            .catch(function () {
                pane.innerHTML = '<div class="alert alert-danger">Không thể tải dữ liệu. <button type="button" class="btn btn-sm btn-outline-danger" data-analysis-retry>Thử lại</button></div>';
                pane.querySelector('[data-analysis-retry]').addEventListener('click', function () { loadPane(pane, query); });
            });
    }

    function refreshPeriodPicker(pane) {
        var frequency = pane.querySelector('[name=TanSuat]');
        var primary = pane.querySelector('[name=KyBaoCaoId]');
        var picker = pane.querySelector('[data-period-picker]');
        if (!frequency || !primary || !picker) return;
        var primaryOption = primary.options[primary.selectedIndex];
        var primaryStart = primaryOption ? primaryOption.dataset.start : null;
        picker.querySelectorAll('.period-picker-option').forEach(function (option) {
            var eligible = option.dataset.frequency === frequency.value && (!primaryStart || option.dataset.start < primaryStart) && option.querySelector('input').value !== primary.value;
            option.hidden = !eligible;
            if (!eligible) option.querySelector('input').checked = false;
        });
    }

    function bindPane(pane) {
        pane.querySelectorAll('[data-dashboard-analysis-form]').forEach(function (form) {
            form.addEventListener('submit', function (event) {
                event.preventDefault();
                loadPane(pane, new URLSearchParams(new FormData(form)).toString());
            });
        });
        pane.querySelectorAll('[data-analysis-export]').forEach(function (link) {
            link.addEventListener('click', function (event) {
                var form = link.closest('form');
                if (!form) return;
                event.preventDefault();
                var parameters = new URLSearchParams(new FormData(form));
                parameters.delete('Page');
                parameters.delete('PageSize');
                var exportUrl = link.dataset.exportUrl || link.getAttribute('href');
                window.location.href = exportUrl + (parameters.toString() ? '?' + parameters.toString() : '');
            });
        });
        var picker = pane.querySelector('[data-period-picker]');
        if (picker) {
            var checkboxes = Array.from(picker.querySelectorAll('input[type=checkbox]'));
            picker.querySelectorAll('[data-period-preset]').forEach(function (button) {
                button.addEventListener('click', function () {
                    var limit = Number(button.dataset.periodPreset);
                    checkboxes.filter(function (box) { return !box.closest('.period-picker-option').hidden; }).forEach(function (box, index) { box.checked = index < limit; });
                    picker.querySelector('[data-period-summary]').textContent = checkboxes.filter(function (box) { return box.checked; }).length + ' kỳ đã chọn';
                });
            });
            checkboxes.forEach(function (box) {
                box.addEventListener('change', function () {
                    if (checkboxes.filter(function (item) { return item.checked; }).length > 11) box.checked = false;
                    picker.querySelector('[data-period-summary]').textContent = checkboxes.filter(function (item) { return item.checked; }).length + ' kỳ đã chọn';
                });
            });
            var frequency = pane.querySelector('[name=TanSuat]');
            var primary = pane.querySelector('[name=KyBaoCaoId]');
            if (frequency) frequency.addEventListener('change', function () { pane.querySelector('[data-dashboard-analysis-form]').dispatchEvent(new Event('submit')); });
            if (primary) primary.addEventListener('change', function () { refreshPeriodPicker(pane); });
            refreshPeriodPicker(pane);
        }
        renderCharts(pane);
    }

    panes.forEach(function (pane) { if (!pane.hidden) bindPane(pane); });
    tabs.forEach(function (tab) {
        tab.addEventListener('click', function (event) {
            event.preventDefault();
            var name = tab.dataset.dashboardTab;
            tabs.forEach(function (item) { item.classList.toggle('is-active', item === tab); });
            panes.forEach(function (pane) {
                var active = pane.dataset.dashboardPane === name;
                pane.hidden = !active;
                pane.classList.toggle('is-active', active);
                if (active && name !== 'overview' && !pane.dataset.loaded) {
                    pane.dataset.loaded = 'true';
                    loadPane(pane, window.location.search.replace(/^\?/, ''));
                }
            });
            history.replaceState(null, '', '#' + name);
        });
    });
    var requested = window.location.hash.replace('#', '');
    var initial = document.querySelector('[data-dashboard-tab="' + requested + '"]');
    if (initial) initial.click();
}
