(function () {
  'use strict';

  var modalEl = document.getElementById('globalSearchModal');
  var inputEl = document.getElementById('globalSearchInput');
  var resultsEl = document.getElementById('globalSearchResults');
  var statusEl = document.getElementById('globalSearchStatus');

  if (!modalEl || !inputEl || !resultsEl) return;

  var modal = typeof bootstrap !== 'undefined' ? new bootstrap.Modal(modalEl) : null;
  var debounceTimer = null;
  var seq = 0;

  function setStatus(text) {
    if (statusEl) statusEl.textContent = text || '';
  }

  function escapeHtml(s) {
    if (!s) return '';
    var d = document.createElement('div');
    d.textContent = s;
    return d.innerHTML;
  }

  function render(items, truncated) {
    if (!items || !items.length) {
      resultsEl.innerHTML =
        '<div class="list-group-item text-muted small py-4 text-center">No matches. Try a code, name, or number.</div>';
      setStatus('');
      return;
    }

    var bySection = {};
    items.forEach(function (h) {
      var sec = h.section || 'Other';
      if (!bySection[sec]) bySection[sec] = [];
      bySection[sec].push(h);
    });

    var html = '';
    Object.keys(bySection).forEach(function (sec) {
      html +=
        '<div class="px-3 py-2 bg-label-secondary text-secondary small fw-semibold text-uppercase">' +
        escapeHtml(sec) +
        '</div>';
      bySection[sec].forEach(function (h) {
        html +=
          '<a href="' +
          escapeHtml(h.url) +
          '" class="list-group-item list-group-item-action border-0 border-bottom py-2 px-3">' +
          '<div class="d-flex justify-content-between align-items-start gap-2">' +
          '<div class="flex-grow-1 min-w-0">' +
          '<div class="fw-medium text-truncate">' +
          escapeHtml(h.title) +
          '</div>' +
          '<div class="small text-muted text-truncate">' +
          escapeHtml(h.entity) +
          (h.subtitle ? ' · ' + escapeHtml(h.subtitle) : '') +
          '</div>' +
          '</div>' +
          '<i class="icon-base bx bx-chevron-right text-muted flex-shrink-0"></i>' +
          '</div>' +
          '</a>';
      });
    });

    if (truncated) {
      html +=
        '<div class="list-group-item small text-muted py-2 text-center border-0">Showing the first results only. Refine your search.</div>';
    }

    resultsEl.innerHTML = html;
    setStatus(items.length + ' result(s)');
  }

  function runSearch(q) {
    var my = ++seq;
    if (!q || q.trim().length < 2) {
      resultsEl.innerHTML =
        '<div class="list-group-item text-muted small py-4 text-center">Type at least 2 characters.</div>';
      setStatus('');
      return;
    }

    setStatus('Searching…');
    resultsEl.innerHTML =
      '<div class="list-group-item text-center py-4"><div class="spinner-border spinner-border-sm text-primary" role="status"></div></div>';

    var queryUrl = modalEl.getAttribute('data-search-url') || '/GlobalSearch/Query';
    var sep = queryUrl.indexOf('?') >= 0 ? '&' : '?';
    fetch(queryUrl + sep + 'q=' + encodeURIComponent(q.trim()), {
      headers: { Accept: 'application/json' },
      credentials: 'same-origin',
    })
      .then(function (r) {
        if (!r.ok) throw new Error('Search failed');
        return r.json();
      })
      .then(function (data) {
        if (my !== seq) return;
        render(data.items || [], data.truncated);
      })
      .catch(function () {
        if (my !== seq) return;
        resultsEl.innerHTML =
          '<div class="list-group-item text-danger small py-3 text-center">Search is temporarily unavailable.</div>';
        setStatus('');
      });
  }

  inputEl.addEventListener('input', function () {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(function () {
      runSearch(inputEl.value);
    }, 280);
  });

  modalEl.addEventListener('shown.bs.modal', function () {
    inputEl.focus();
    inputEl.select();
    runSearch(inputEl.value);
  });

  modalEl.addEventListener('hidden.bs.modal', function () {
    inputEl.value = '';
    resultsEl.innerHTML =
      '<div class="list-group-item text-muted small py-4 text-center">Type at least 2 characters.</div>';
    setStatus('');
  });

  document.addEventListener('keydown', function (e) {
    if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
      var tag = (e.target && e.target.tagName) || '';
      if (tag === 'INPUT' || tag === 'TEXTAREA' || e.target.isContentEditable) return;
      e.preventDefault();
      if (modal) modal.show();
    }
  });
})();
