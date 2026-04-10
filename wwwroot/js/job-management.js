/**
 * Job & Notification Management Page Script
 * Handles loading, rendering, filtering, and managing job configurations
 */

(function() {
    'use strict';

    var _jobConfigs = [];
    var _selectedIds = new Set();
    var _currentFilter = 'all';
    var _categoryFilter = '';
    var _severityFilter = '';
    var _statusFilter = '';

    // Cron expression presets
    var cronPresets = [
        { label: 'Every 5 min',   value: '*/5 * * * *' },
        { label: 'Every 10 min',  value: '*/10 * * * *' },
        { label: 'Every 30 min', value: '*/30 * * * *' },
        { label: 'Every hour',    value: '0 * * * *' },
        { label: 'Daily 8 AM',   value: '0 8 * * *' },
        { label: 'Daily 6 PM',   value: '0 18 * * *' },
        { label: 'Weekly Monday', value: '0 8 * * 1' },
        { label: 'Every day at noon', value: '0 12 * * *' },
        { label: 'Every Monday 9 AM', value: '0 9 * * 1' },
    ];

    // Severity badge colors
    function severityBadge(sev) {
        var map = {
            'Critical': 'bg-label-danger',
            'Warning': 'bg-label-warning',
            'Info': 'bg-label-info'
        };
        var cls = map[sev] || 'bg-label-secondary';
        return '<span class="badge ' + cls + '">' + sev + '</span>';
    }

    // Status badge
    function statusBadge(enabled) {
        if (enabled) {
            return '<span class="badge bg-label-success"><i class="icon-base bx bx-check-circle me-1"></i>Enabled</span>';
        }
        return '<span class="badge bg-label-secondary"><i class="icon-base bx bx-x-circle me-1"></i>Disabled</span>';
    }

    // Build cron expression select dropdown
    function buildCronSelect(selectedCron) {
        var options = cronPresets.map(function(p) {
            var sel = p.value === selectedCron ? ' selected' : '';
            return '<option value="' + p.value + '"' + sel + '>' + p.label + ' (' + p.value + ')</option>';
        }).join('');
        return '<select class="form-select form-select-sm cron-select" data-id="' + encodeURIComponent(selectedCron) + '">' + options + '</select>';
    }

    // Build table row
    function buildRow(cfg) {
        var checked = _selectedIds.has(cfg.configID) ? ' checked' : '';
        var severityClass = cfg.severity === 'Critical' ? 'text-danger' : (cfg.severity === 'Warning' ? 'text-warning' : 'text-body');
        var categoryClass = getCategoryClass(cfg.category);

        return '<tr data-id="' + cfg.configID + '" data-enabled="' + cfg.isEnabled + '" data-category="' + cfg.category + '" data-severity="' + cfg.severity + '">' +
            '<td><input class="form-check-input row-check" type="checkbox" value="' + cfg.configID + '" onchange="toggleJobSelect(' + cfg.configID + ')"' + checked + ' /></td>' +
            '<td>' + statusBadge(cfg.isEnabled) + '</td>' +
            '<td><div class="fw-medium ' + severityClass + '">' + cfg.notificationName + '</div><small class="text-muted">' + cfg.notificationCode + '</small></td>' +
            '<td><span class="badge ' + categoryClass + '">' + cfg.category + '</span></td>' +
            '<td>' + severityBadge(cfg.severity) + '</td>' +
            '<td>' + buildCronSelect(cfg.cronExpression || '') + '</td>' +
            '<td><span class="text-muted small">' + (cfg.checkIntervalMinutes || 0) + ' min</span></td>' +
            '<td>' +
                '<div class="d-flex gap-1">' +
                    '<button class="btn btn-xs btn-icon btn-outline-primary" onclick="toggleJobStatus(' + cfg.configID + ', ' + !cfg.isEnabled + ')" title="' + (cfg.isEnabled ? 'Disable' : 'Enable') + '">' +
                        '<i class="icon-base bx ' + (cfg.isEnabled ? 'bx-pause' : 'bx-play') + '"></i>' +
                    '</button>' +
                    '<button class="btn btn-xs btn-icon btn-outline-info" onclick="triggerJobNow(\'' + escapeJsString(cfg.notificationCode) + '\')" title="Run now">' +
                        '<i class="icon-base bx bx-bolt"></i>' +
                    '</button>' +
                    '<a href="/NotificationConfig/Edit/' + cfg.configID + '" class="btn btn-xs btn-icon btn-outline-secondary" title="Edit config">' +
                        '<i class="icon-base bx bx-edit"></i>' +
                    '</a>' +
                '</div>' +
            '</td>' +
        '</tr>';
    }

    // Get category badge class
    function getCategoryClass(category) {
        var map = {
            'Finance': 'bg-label-primary',
            'Inventory': 'bg-label-success',
            'HumanResources': 'bg-label-info',
            'Sales': 'bg-label-warning',
            'CustomerService': 'bg-label-danger',
            'Marketing': 'bg-label-secondary',
            'Executive': 'bg-label-dark'
        };
        return map[category] || 'bg-label-primary';
    }

    // Escape string for JavaScript
    function escapeJsString(str) {
        if (!str) return '';
        return str.replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/"/g, '\\"');
    }

    // Apply all filters
    function getFiltered(configs) {
        return configs.filter(function(c) {
            // Status filter (from toggle buttons)
            if (_currentFilter === 'enabled' && !c.isEnabled) return false;
            if (_currentFilter === 'disabled' && c.isEnabled) return false;

            // Category filter
            if (_categoryFilter && c.category !== _categoryFilter) return false;

            // Severity filter
            if (_severityFilter && c.severity !== _severityFilter) return false;

            // Status filter (from dropdown)
            if (_statusFilter === 'enabled' && !c.isEnabled) return false;
            if (_statusFilter === 'disabled' && c.isEnabled) return false;

            return true;
        });
    }

    // Render table
    function renderTable(configs) {
        var tbody = document.getElementById('jobConfigTableBody');
        if (!tbody) return;

        var filtered = getFiltered(configs);

        if (filtered.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-5"><i class="icon-base bx bx-inbox me-2"></i>No notifications found</td></tr>';
        } else {
            tbody.innerHTML = filtered.map(buildRow).join('');
        }

        // Update count label
        var countLabel = document.getElementById('jobCountLabel');
        if (countLabel) {
            var en = configs.filter(function(c) { return c.isEnabled; }).length;
            countLabel.textContent = filtered.length + ' of ' + configs.length + ' notifications (' + en + ' enabled)';
        }

        // Update stats
        updateStats(configs);

        // Bind cron change events
        bindCronChanges();
    }

    // Update statistics cards
    function updateStats(configs) {
        var total = configs.length;
        var enabled = configs.filter(function(c) { return c.isEnabled; }).length;
        var disabled = total - enabled;
        var categories = new Set(configs.map(function(c) { return c.category; })).size;

        document.getElementById('totalCount').textContent = total;
        document.getElementById('enabledCount').textContent = enabled;
        document.getElementById('disabledCount').textContent = disabled;
        document.getElementById('categoryCount').textContent = categories;
    }

    // Bind cron select change events
    function bindCronChanges() {
        document.querySelectorAll('.cron-select').forEach(function(sel) {
            sel.onchange = function() {
                var id = parseInt(sel.closest('tr').querySelector('.row-check').value);
                var cron = sel.value;
                updateCron(id, cron);
            };
        });
    }

    // Update cron expression via API
    function updateCron(id, cron) {
        fetch('/NotificationConfig/UpdateCron', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ configId: id, cronExpression: cron })
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data.success) {
                showToast('Cron updated to: ' + cron, 'success');
            } else {
                showToast(data.message || 'Failed to update cron', 'error');
            }
        })
        .catch(function() {
            showToast('Network error', 'error');
        });
    }

    // Get anti-forgery token
    function getAntiForgeryToken() {
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    // Show toast notification
    function showToast(message, type) {
        window.showToast(message, type);
    }

    // Load job configurations from API
    window.loadJobConfigs = function() {
        fetch('/JobManagement/GetJobConfigs')
            .then(function(r) {
                if (!r.ok) throw new Error('Failed to load');
                return r.json();
            })
            .then(function(data) {
                _jobConfigs = Array.isArray(data) ? data : [];
                renderTable(_jobConfigs);
            })
            .catch(function(err) {
                console.error('Error loading job configs:', err);
                var tbody = document.getElementById('jobConfigTableBody');
                if (tbody) {
                    tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger py-5"><i class="icon-base bx bx-error me-2"></i>Failed to load configurations. Please refresh.</td></tr>';
                }
            });
    };

    // Toggle single job status
    window.toggleJobStatus = function(id, enable) {
        fetch('/NotificationConfig/UpdateJobStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ configId: id, isEnabled: enable })
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data.success) {
                var cfg = _jobConfigs.find(function(c) { return c.configID === id; });
                if (cfg) cfg.isEnabled = data.isEnabled;
                renderTable(_jobConfigs);
                showToast(data.message || 'Status updated', 'success');
            } else {
                showToast(data.message || 'Failed to update status', 'error');
            }
        })
        .catch(function() {
            showToast('Network error', 'error');
        });
    };

    // Toggle job selection
    window.toggleJobSelect = function(id) {
        if (_selectedIds.has(id)) {
            _selectedIds.delete(id);
        } else {
            _selectedIds.add(id);
        }
        updateCheckAll();
    };

    // Toggle all jobs
    window.toggleAllJobs = function(checked) {
        document.querySelectorAll('.row-check').forEach(function(cb) {
            var id = parseInt(cb.value);
            if (checked) {
                _selectedIds.add(id);
            } else {
                _selectedIds.delete(id);
            }
            cb.checked = checked;
        });
    };

    // Update "check all" checkbox state
    function updateCheckAll() {
        var all = document.querySelectorAll('.row-check');
        var checked = document.querySelectorAll('.row-check:checked');
        var cbAll = document.getElementById('checkAllJobs');
        if (cbAll) {
            cbAll.checked = all.length > 0 && checked.length === all.length;
        }
    }

    // Set table filter (All/Enabled/Disabled)
    window.setTableFilter = function(filter) {
        _currentFilter = filter;

        // Update button states
        document.getElementById('btn-filter-all').classList.toggle('active', filter === 'all');
        document.getElementById('btn-filter-enabled').classList.toggle('active', filter === 'enabled');
        document.getElementById('btn-filter-disabled').classList.toggle('active', filter === 'disabled');

        renderTable(_jobConfigs);
    };

    // Apply filters from dropdowns
    window.applyFilters = function() {
        _categoryFilter = document.getElementById('filterCategory').value;
        _severityFilter = document.getElementById('filterSeverity').value;
        _statusFilter = document.getElementById('filterStatus').value;

        renderTable(_jobConfigs);
    };

    // Clear all filters
    window.clearFilters = function() {
        document.getElementById('filterCategory').value = '';
        document.getElementById('filterStatus').value = '';
        document.getElementById('filterSeverity').value = '';

        _categoryFilter = '';
        _severityFilter = '';
        _statusFilter = '';
        _currentFilter = 'all';

        // Reset button states
        document.getElementById('btn-filter-all').classList.add('active');
        document.getElementById('btn-filter-enabled').classList.remove('active');
        document.getElementById('btn-filter-disabled').classList.remove('active');

        renderTable(_jobConfigs);
    };

    // Bulk enable selected jobs
    window.bulkEnableSelected = function() {
        if (_selectedIds.size === 0) {
            showToast('Please select at least one job', 'error');
            return;
        }

        var ids = Array.from(_selectedIds);
        var promises = ids.map(function(id) {
            return fetch('/NotificationConfig/UpdateJobStatus', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({ configId: id, isEnabled: true })
            }).then(function(r) { return r.json(); });
        });

        Promise.all(promises)
            .then(function(results) {
                var success = results.filter(function(r) { return r.success; }).length;
                showToast(success + ' jobs enabled successfully', 'success');
                _selectedIds.clear();
                setTimeout(function() { renderTable(_jobConfigs); }, 500);
            })
            .catch(function() {
                showToast('Network error', 'error');
            });
    };

    // Bulk disable selected jobs
    window.bulkDisableSelected = function() {
        if (_selectedIds.size === 0) {
            showToast('Please select at least one job', 'error');
            return;
        }

        var ids = Array.from(_selectedIds);
        var promises = ids.map(function(id) {
            return fetch('/NotificationConfig/UpdateJobStatus', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({ configId: id, isEnabled: false })
            }).then(function(r) { return r.json(); });
        });

        Promise.all(promises)
            .then(function(results) {
                var success = results.filter(function(r) { return r.success; }).length;
                showToast(success + ' jobs disabled successfully', 'success');
                _selectedIds.clear();
                setTimeout(function() { renderTable(_jobConfigs); }, 500);
            })
            .catch(function() {
                showToast('Network error', 'error');
            });
    };

    // Trigger job manually
    window.triggerJobNow = function(jobId) {
        fetch('/NotificationConfig/TriggerJobNow', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ jobId: jobId })
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            showToast(data.message || 'Job triggered', data.success ? 'success' : 'error');
        })
        .catch(function() {
            showToast('Network error', 'error');
        });
    };

    // Refresh table
    window.refreshJobTable = function() {
        loadJobConfigs();
    };

    // Initialize on page load
    document.addEventListener('DOMContentLoaded', function() {
        loadJobConfigs();
    });

})();
