; (function () {
    'use strict';

    var connection = null;
    var userId = document.querySelector('meta[name="userId"]')?.content;
    var pollingInterval = null;

    function init() {
        if (!userId) return;
        connectSignalR();
        setupEventListeners();
        loadInitialBadge();
        startPolling();
    }

    function connectSignalR() {
        if (connection && connection.state === 'Connected') return;

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/notificationHub')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.on('ReceiveNotification', function (notification) {
            prependNotification(notification);
            updateBadge(true);
            showToast(notification);
        });

        connection.onreconnected(function () {
            connection.invoke('JoinUserGroup', userId).catch(function (err) {
                console.error('SignalR join group failed:', err);
            });
        });

        connection.onclose(function () {
            console.warn('SignalR disconnected, polling fallback active');
        });

        connection.start()
            .then(function () {
                return connection.invoke('JoinUserGroup', userId);
            })
            .catch(function (err) {
                console.warn('SignalR connection failed, using polling fallback:', err);
            });
    }

    function setupEventListeners() {
        var bell = document.getElementById('notificationBell');
        if (bell) {
            bell.addEventListener('click', function () {
                loadNotifications();
            });
        }

        var markAllBtn = document.getElementById('markAllReadBtn');
        if (markAllBtn) {
            markAllBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                markAllAsRead();
            });
        }
    }

    function loadInitialBadge() {
        fetch('/Notification/GetUnreadCount')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                updateBadgeDisplay(data.count);
            })
            .catch(function (err) {
                console.warn('Failed to load badge:', err);
            });
    }

    function loadNotifications() {
        fetch('/Notification/GetAll?pageSize=10')
            .then(function (r) { return r.json(); })
            .then(function (notifications) {
                renderNotificationList(notifications);
            })
            .catch(function (err) {
                console.warn('Failed to load notifications:', err);
            });
    }

    function renderNotificationList(notifications) {
        var list = document.getElementById('notificationList');
        var noItem = document.getElementById('noNotificationsItem');
        if (!list) return;

        if (!notifications || notifications.length === 0) {
            if (noItem) noItem.style.display = '';
            return;
        }

        if (noItem) noItem.style.display = 'none';

        var html = '';
        notifications.forEach(function (n) {
            var severityClass = getSeverityClass(n.Severity);
            var iconBgClass = getIconBgClass(n.Category);
            var readClass = n.IsRead ? 'opacity-50' : '';
            var timeAgo = formatTimeAgo(n.CreatedAt);

            html +=
                '<li class="list-group-item list-group-item-action dropdown-notifications-item ' + readClass + '" ' +
                '    data-id="' + n.NotificationID + '" ' +
                '    onclick="window.markNotificationRead(' + n.NotificationID + ', \'' + (n.ActionUrl || '') + '\')">' +
                '  <div class="d-flex">' +
                '    <div class="flex-shrink-0 me-3">' +
                '      <div class="avatar ' + iconBgClass + '">' +
                '        <span class="avatar-initial rounded-circle">' +
                '          <i class="icon-base ' + getCategoryIcon(n.Category) + '"></i>' +
                '        </span>' +
                '      </div>' +
                '    </div>' +
                '    <div class="flex-grow-1">' +
                '      <h6 class="mb-1 small">' + escapeHtml(n.Title) + '</h6>' +
                '      <p class="mb-0 small text-muted">' + escapeHtml(n.Message) + '</p>' +
                '      <small class="text-muted">' + timeAgo + '</small>' +
                '    </div>' +
                '    <div class="flex-shrink-0">' +
                '      <span class="badge ' + severityClass + '">' + n.Severity + '</span>' +
                '    </div>' +
                '  </div>' +
                '</li>';
        });

        list.innerHTML = html;
    }

    function prependNotification(notification) {
        var list = document.getElementById('notificationList');
        var noItem = document.getElementById('noNotificationsItem');
        if (!list) return;

        if (noItem) noItem.style.display = 'none';

        var severityClass = getSeverityClass(notification.Severity);
        var iconBgClass = getIconBgClass(notification.Category);
        var timeAgo = formatTimeAgo(notification.CreatedAt);

        var item = document.createElement('li');
        item.className = 'list-group-item list-group-item-action dropdown-notifications-item';
        item.dataset.id = notification.NotificationID;
        item.style.animation = 'slideDown 0.3s ease';

        item.innerHTML =
            '<div class="d-flex" onclick="window.markNotificationRead(' + notification.NotificationID + ', \'' + (notification.ActionUrl || '') + '\')">' +
            '  <div class="flex-shrink-0 me-3">' +
            '    <div class="avatar ' + iconBgClass + '">' +
            '      <span class="avatar-initial rounded-circle">' +
            '        <i class="icon-base ' + getCategoryIcon(notification.Category) + '"></i>' +
            '      </span>' +
            '    </div>' +
            '  </div>' +
            '  <div class="flex-grow-1">' +
            '    <h6 class="mb-1 small">' + escapeHtml(notification.Title) + '</h6>' +
            '    <p class="mb-0 small text-muted">' + escapeHtml(notification.Message) + '</p>' +
            '    <small class="text-muted">' + timeAgo + '</small>' +
            '  </div>' +
            '  <div class="flex-shrink-0">' +
            '    <span class="badge ' + severityClass + '">' + notification.Severity + '</span>' +
            '  </div>' +
            '</div>';

        if (list.firstChild) {
            list.insertBefore(item, list.firstChild);
        } else {
            list.appendChild(item);
        }

        // Keep only last 10 in dropdown
        var items = list.querySelectorAll('.list-group-item');
        if (items.length > 10) {
            for (var i = 10; i < items.length; i++) {
                list.removeChild(items[i]);
            }
        }
    }

    function updateBadge(increment) {
        var badge = document.getElementById('notificationBadge');
        if (!badge) return;

        var current = parseInt(badge.textContent) || 0;
        var newCount = increment ? current + 1 : current;
        updateBadgeDisplay(newCount);
    }

    function updateBadgeDisplay(count) {
        var badge = document.getElementById('notificationBadge');
        if (!badge) return;

        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.classList.remove('d-none');
        } else {
            badge.textContent = '0';
            badge.classList.add('d-none');
        }
    }

    function startPolling() {
        if (pollingInterval) return;
        pollingInterval = setInterval(function () {
            if (connection && connection.state === 'Connected') return;
            loadInitialBadge();
        }, 30000);
    }

    function markAllAsRead() {
        fetch('/Notification/MarkAllRead', { method: 'POST' })
            .then(function (r) { return r.json(); })
            .then(function () {
                updateBadgeDisplay(0);
                var items = document.querySelectorAll('#notificationList .list-group-item');
                items.forEach(function (item) {
                    item.classList.add('opacity-50');
                });
            })
            .catch(function (err) {
                console.warn('Mark all read failed:', err);
            });
    }

    function showToast(notification) {
        var container = document.getElementById('toastContainer') || createToastContainer();
        var toastId = 'toast-' + Date.now();

        var severityClass = getSeverityClass(notification.Severity);
        var iconBgClass = getIconBgClass(notification.Category);

        var toast =
            '<div id="' + toastId + '" class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="true" data-bs-delay="5000">' +
            '  <div class="toast-header ' + severityClass + '"> ' +
            '    <div class="avatar ' + iconBgClass + ' me-2">' +
            '      <span class="avatar-initial rounded-circle">' +
            '        <i class="icon-base ' + getCategoryIcon(notification.Category) + '"></i>' +
            '      </span>' +
            '    </div>' +
            '    <strong class="me-auto">' + escapeHtml(notification.Title) + '</strong>' +
            '    <small>' + notification.Severity + '</small>' +
            '    <button type="button" class="btn-close btn-close-white ms-2" data-bs-dismiss="toast"></button>' +
            '  </div>' +
            '  <div class="toast-body">' + escapeHtml(notification.Message) + '</div>' +
            '</div>';

        container.insertAdjacentHTML('beforeend', toast);

        var bsToast = new bootstrap.Toast(document.getElementById(toastId));
        bsToast.show();

        document.getElementById(toastId)?.addEventListener('hidden.bs.toast', function () {
            this.remove();
        });
    }

    function createToastContainer() {
        var container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '11000';
        document.body.appendChild(container);
        return container;
    }

    function getSeverityClass(severity) {
        switch (severity) {
            case 'Critical': return 'bg-danger';
            case 'Warning': return 'bg-warning text-dark';
            case 'Info': return 'bg-info';
            case 'Success': return 'bg-success';
            default: return 'bg-secondary';
        }
    }

    function getIconBgClass(category) {
        switch (category) {
            case 'Finance': return 'bg-label-danger';
            case 'Inventory': return 'bg-label-warning';
            case 'HumanResources': return 'bg-label-success';
            case 'Sales': return 'bg-label-primary';
            case 'CustomerService': return 'bg-label-info';
            case 'Marketing': return 'bg-label-dark';
            case 'Executive': return 'bg-label-secondary';
            default: return 'bg-label-primary';
        }
    }

    function getCategoryIcon(category) {
        switch (category) {
            case 'Finance': return 'bx-dollar';
            case 'Inventory': return 'bx-box';
            case 'HumanResources': return 'bx-user';
            case 'Sales': return 'bx-shopping-bag';
            case 'CustomerService': return 'bx-headphone';
            case 'Marketing': return 'bx-trending-up';
            case 'Executive': return 'bx-briefcase';
            default: return 'bx-bell';
        }
    }

    function formatTimeAgo(isoString) {
        var date = new Date(isoString);
        var now = new Date();
        var diffMs = now - date;
        var diffMin = Math.floor(diffMs / 60000);

        if (diffMin < 1) return 'Vua xong';
        if (diffMin < 60) return diffMin + ' phut truoc';
        var diffHrs = Math.floor(diffMin / 60);
        if (diffHrs < 24) return diffHrs + ' gio truoc';
        var diffDays = Math.floor(diffHrs / 24);
        if (diffDays < 7) return diffDays + ' ngay truoc';
        return date.toLocaleDateString('vi-VN');
    }

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Global function for onclick handlers
    window.markNotificationRead = function (id, actionUrl) {
        fetch('/Notification/MarkAsRead?id=' + id, { method: 'POST' })
            .then(function () {
                var badge = document.getElementById('notificationBadge');
                if (badge) {
                    var current = parseInt(badge.textContent) || 0;
                    if (current > 0) updateBadgeDisplay(current - 1);
                }
                if (actionUrl && actionUrl !== 'undefined' && actionUrl !== '') {
                    window.location.href = actionUrl;
                }
            })
            .catch(function (err) {
                console.warn('Mark as read failed:', err);
                if (actionUrl && actionUrl !== 'undefined' && actionUrl !== '') {
                    window.location.href = actionUrl;
                }
            });
    };

    // Add animation keyframes if not exists
    if (!document.getElementById('notifAnimation')) {
        var style = document.createElement('style');
        style.id = 'notifAnimation';
        style.textContent =
            '@keyframes slideDown {' +
            '  from { opacity: 0; transform: translateY(-10px); }' +
            '  to { opacity: 1; transform: translateY(0); }' +
            '}';
        document.head.appendChild(style);
    }

    // Wait for DOM
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();
