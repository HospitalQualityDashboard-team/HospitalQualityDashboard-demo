function initializeDashboardAnalysis() {
    var tabs = document.querySelectorAll('[data-dashboard-tab]');
    var panes = document.querySelectorAll('[data-dashboard-pane]');

    function renderCharts(container) {
        var comparison = container.querySelector('.comparison-progress-chart');
        if (comparison && !comparison.dataset.rendered) {
            var metrics = JSON.parse(comparison.dataset.metrics || '[]');
            new Chart(comparison, {
                type: 'bar',
                data: {
                    labels: metrics.map(function (x) { return x.TenKyBaoCao; }),
                    datasets: [
                        { label: 'Đúng hạn', data: metrics.map(function (x) { return x.DungHan; }), backgroundColor: '#22a06b' },
                        { label: 'Nộp trễ', data: metrics.map(function (x) { return x.NopTre; }), backgroundColor: '#e5484d' },
                        { label: 'Chưa nộp', data: metrics.map(function (x) { return x.ChuaNop; }), backgroundColor: '#f0a202' },
                        { label: 'Quá hạn chưa nộp', data: metrics.map(function (x) { return x.QuaHanChuaNop; }), backgroundColor: '#9f1239' }
                    ]
                },
                options: { responsive: true, maintainAspectRatio: false, scales: { x: { stacked: true }, y: { stacked: true, beginAtZero: true } }, plugins: { legend: { position: 'bottom' } } }
            });
            comparison.dataset.rendered = 'true';
        }

        var trend = container.querySelector('.dashboard-trend-chart');
        if (trend && !trend.dataset.rendered) {
            var trendMetrics = JSON.parse(trend.dataset.metrics || '[]');
            new Chart(trend, {
                type: 'line',
                data: {
                    labels: trendMetrics.map(function (x) { return x.TenKyBaoCao; }),
                    datasets: [
                        { label: 'Tỷ lệ hoàn thành', data: trendMetrics.map(function (x) { return x.TyLeHoanThanh; }), borderColor: '#146c78', tension: 0.28 },
                        { label: 'Tỷ lệ đúng hạn', data: trendMetrics.map(function (x) { return x.TyLeDungHan; }), borderColor: '#2563eb', tension: 0.28 },
                        { label: 'Nộp trễ', data: trendMetrics.map(function (x) { return x.NopTre; }), borderColor: '#e5484d', tension: 0.28, yAxisID: 'count' },
                        { label: 'Quá hạn chưa nộp', data: trendMetrics.map(function (x) { return x.QuaHanChuaNop; }), borderColor: '#f0a202', tension: 0.28, yAxisID: 'count' }
                    ]
                },
                options: { responsive: true, maintainAspectRatio: false, scales: { y: { beginAtZero: true, max: 100 }, count: { beginAtZero: true, position: 'right', grid: { drawOnChartArea: false } } }, plugins: { legend: { position: 'bottom' } } }
            });
            trend.dataset.rendered = 'true';
        }
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
        pane.querySelectorAll('[data-dashboard-analysis-form], [data-dashboard-trend-form]').forEach(function (form) {
            form.addEventListener('submit', function (event) {
                event.preventDefault();
                loadPane(pane, new URLSearchParams(new FormData(form)).toString());
            });
        });
        pane.querySelectorAll('[data-analysis-page]').forEach(function (button) {
            button.addEventListener('click', function () {
                var form = pane.querySelector('[data-dashboard-analysis-form]');
                form.querySelector('[name=Page]').value = button.dataset.analysisPage;
                form.dispatchEvent(new Event('submit'));
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
        tab.addEventListener('click', function () {
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
