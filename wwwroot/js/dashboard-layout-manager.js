/**
 * Dashboard Layout Manager - Custom lightweight drag & drop grid layout
 * Works with absolute positioning + HTML5 Drag API (no external library needed)
 * Supports: drag to reorder, resize widgets, localStorage persistence
 */

(function() {
  'use strict';

  const DEFAULT_STORAGE_KEY = 'dashboard-layout-v1';
  const GRID_COLS = 12;

  class DashboardLayoutManager {
    constructor(containerId, options = {}) {
      this.container = document.getElementById(containerId);
      if (!this.container) {
        console.error('DashboardLayoutManager: container #' + containerId + ' not found');
        return;
      }

      this.options = {
        animate: true,
        gap: 20,
        cellHeight: 60,
        onLayoutChange: null,
        ...options
      };

      this.storageKey = this.options.storageKey || DEFAULT_STORAGE_KEY;

      this.widgets = [];
      this.isDragging = false;
      this.draggedWidget = null;
      this.editMode = false;
      this.placeholder = null;
      this.initialized = false;

      this.init();
    }

    init() {
      this.widgets = Array.from(this.container.querySelectorAll('.grid-stack-item'));
      if (this.widgets.length === 0) return;

      // Wait for layout to settle (defer to end of render queue)
      this._deferInit();
    }

    /** Defer initialization to ensure container has its final size */
    _deferInit() {
      requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          // Also wait for fonts/images that might affect layout
          const containerWidth = this.container.getBoundingClientRect().width;
          if (containerWidth === 0) {
            // Still not ready, try again after a short delay
            setTimeout(() => this._setupLayout(), 100);
          } else {
            this._setupLayout();
          }
        });
      });
    }

    _setupLayout() {
      const saved = this.loadFromStorage();
      if (saved && saved.length > 0 && this.isSavedLayoutCompatible(saved)) {
        this.applyLayout(saved);
      } else {
        if (saved && saved.length > 0) {
          try {
            localStorage.removeItem(this.storageKey);
          } catch (e) { /* ignore */ }
        }
        this.applyPositionsFromAttributes();
      }

      this.setupDragAndDrop();
      this.initialized = true;
    }

    /** Reject cached layout when widget set changed (deploy / conditional charts) or counts differ */
    isSavedLayoutCompatible(layout) {
      if (!Array.isArray(layout) || layout.length === 0) return false;
      const domIds = this.widgets
        .map(w => w.getAttribute('data-widget-id'))
        .filter(Boolean);
      const domSet = new Set(domIds);
      if (layout.length !== domSet.size) return false;
      for (let i = 0; i < layout.length; i++) {
        const item = layout[i];
        if (!item || !item.id || !domSet.has(item.id)) return false;
      }
      return true;
    }

    /** Convert data-gs-* attributes to pixel positions */
    applyPositionsFromAttributes() {
      const rect = this.container.getBoundingClientRect();
      const containerWidth = rect.width;
      const containerLeft = rect.left;

      // Get actual available width (parent padding aware)
      const computedStyle = window.getComputedStyle(this.container);
      const paddingLeft = parseFloat(computedStyle.paddingLeft) || 0;
      const paddingRight = parseFloat(computedStyle.paddingRight) || 0;
      const availableWidth = containerWidth - paddingLeft - paddingRight;

      const colWidth = (availableWidth - (GRID_COLS - 1) * this.options.gap) / GRID_COLS;
      const rowHeight = this.options.cellHeight;

      this.widgets.forEach((widget, index) => {
        const x = parseInt(widget.getAttribute('data-gs-x')) || 0;
        const y = parseInt(widget.getAttribute('data-gs-y')) || 0;
        const w = parseInt(widget.getAttribute('data-gs-width')) || 6;
        const h = parseInt(widget.getAttribute('data-gs-height')) || 5;

        const left = paddingLeft + x * (colWidth + this.options.gap);
        const top = y * (rowHeight + this.options.gap);
        const width = w * colWidth + (w - 1) * this.options.gap;
        const height = h * rowHeight + (h - 1) * this.options.gap;

        // CRITICAL: set position relative to container, not viewport
        widget.style.position = 'absolute';
        widget.style.left = left + 'px';
        widget.style.top = top + 'px';
        widget.style.width = width + 'px';
        widget.style.height = height + 'px';
        widget.style.boxSizing = 'border-box';
        widget.style.zIndex = String(index + 1);
        widget.style.margin = '0';
        widget.style.padding = '0';
      });

      this.updateContainerMinHeight();
    }

    /** Ensure grid extends to contain all widgets (scroll inside content area only) */
    updateContainerMinHeight() {
      const rowHeight = this.options.cellHeight;
      const gap = this.options.gap;
      let maxRow = 0;
      this.widgets.forEach(w => {
        const y = parseInt(w.getAttribute('data-gs-y'), 10) || 0;
        const h = parseInt(w.getAttribute('data-gs-height'), 10) || 1;
        maxRow = Math.max(maxRow, y + h);
      });
      const minH = maxRow > 0 ? maxRow * (rowHeight + gap) - gap + 16 : 400;
      this.container.style.minHeight = minH + 'px';
    }

    /** Reposition all widgets after container resize */
    recalculatePositions() {
      if (!this.initialized) return;
      this.applyPositionsFromAttributes();
      this.widgets.forEach(w => this.triggerChartUpdate(w));
    }

    setupDragAndDrop() {
      this.widgets.forEach(widget => {
        widget.draggable = true;

        // Add resize handle
        if (!widget.querySelector('.ui-resizable-handle')) {
          const handle = document.createElement('div');
          handle.className = 'ui-resizable-handle ui-resizable-se';
          handle.title = 'Resize widget';
          handle.setAttribute('draggable', 'false');
          widget.appendChild(handle);
          this.setupResizeEvents(widget, handle);
        }

        this.setupDragEvents(widget);
      });

      // Handle window resize (debounced)
      let resizeTimeout;
      window.addEventListener('resize', () => {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(() => {
          this.recalculatePositions();
        }, 100);
      });
    }

    setupDragEvents(widget) {
      widget.addEventListener('dragstart', (e) => {
        if (e.target && e.target.closest && e.target.closest('.ui-resizable-handle')) {
          e.preventDefault();
          return;
        }
        if (!this.editMode) {
          e.preventDefault();
          return;
        }
        this.isDragging = true;
        this.draggedWidget = widget;
        widget.classList.add('dragging');
        widget.style.opacity = '0.6';
        widget.style.zIndex = '1000';
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', widget.getAttribute('data-widget-id') || '');
        e.dataTransfer.setDragImage(widget, widget.offsetWidth / 2, 20);

        this.createPlaceholder(widget);
      });

      widget.addEventListener('dragend', (e) => {
        this.isDragging = false;
        widget.classList.remove('dragging');
        widget.style.opacity = '1';
        widget.style.zIndex = '';
        widget.style.cursor = '';
        this.draggedWidget = null;

        if (this.placeholder) {
          this.placeholder.remove();
          this.placeholder = null;
        }

        this.saveToStorage();
        if (this.options.onLayoutChange) {
          this.options.onLayoutChange(this.getLayout());
        }
      });

      widget.addEventListener('dragover', (e) => {
        if (!this.editMode || !this.draggedWidget || this.draggedWidget === widget) return;
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
      });

      widget.addEventListener('dragenter', (e) => {
        if (!this.editMode || !this.draggedWidget || this.draggedWidget === widget) return;
        e.preventDefault();
        widget.classList.add('drag-over');
      });

      widget.addEventListener('dragleave', (e) => {
        if (!this.editMode) return;
        const rect = widget.getBoundingClientRect();
        const x = e.clientX, y = e.clientY;
        if (x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) {
          widget.classList.remove('drag-over');
        }
      });

      widget.addEventListener('drop', (e) => {
        if (!this.editMode || !this.draggedWidget || this.draggedWidget === widget) return;
        e.preventDefault();
        widget.classList.remove('drag-over');

        this.swapWidgets(this.draggedWidget, widget);
      });
    }

    setupResizeEvents(widget, handle) {
      let isResizing = false;
      let startX, startY, originalGSWidth, originalGSHeight;

      handle.addEventListener('mousedown', (e) => {
        if (!this.editMode) return;
        e.preventDefault();
        e.stopPropagation();
        isResizing = true;

        startX = e.clientX;
        startY = e.clientY;
        originalGSWidth = parseInt(widget.getAttribute('data-gs-width')) || 6;
        originalGSHeight = parseInt(widget.getAttribute('data-gs-height')) || 5;

        widget.style.zIndex = '1000';
        widget.classList.add('resizing');

        const onMouseMove = (e) => {
          if (!isResizing) return;
          const dx = e.clientX - startX;
          const dy = e.clientY - startY;

          const containerRect = this.container.getBoundingClientRect();
          const computedStyle = window.getComputedStyle(this.container);
          const paddingLeft = parseFloat(computedStyle.paddingLeft) || 0;
          const availableWidth = containerRect.width - paddingLeft - (parseFloat(computedStyle.paddingRight) || 0);
          const colWidth = (availableWidth - (GRID_COLS - 1) * this.options.gap) / GRID_COLS;
          const rowHeight = this.options.cellHeight;

          const dCols = Math.round(dx / (colWidth + this.options.gap));
          const dRows = Math.round(dy / (rowHeight + this.options.gap));

          const finalW = Math.max(1, Math.min(GRID_COLS, originalGSWidth + dCols));
          const finalH = Math.max(1, originalGSHeight + dRows);

          widget.setAttribute('data-gs-width', finalW);
          widget.setAttribute('data-gs-height', finalH);

          // Apply pixel positions
          const left = paddingLeft + parseInt(widget.getAttribute('data-gs-x')) * (colWidth + this.options.gap);
          const top = parseInt(widget.getAttribute('data-gs-y')) * (rowHeight + this.options.gap);
          const width = finalW * colWidth + (finalW - 1) * this.options.gap;
          const height = finalH * rowHeight + (finalH - 1) * this.options.gap;

          widget.style.left = left + 'px';
          widget.style.top = top + 'px';
          widget.style.width = width + 'px';
          widget.style.height = height + 'px';
        };

        const onMouseUp = () => {
          if (!isResizing) return;
          isResizing = false;
          widget.style.zIndex = '';
          widget.classList.remove('resizing');
          document.removeEventListener('mousemove', onMouseMove);
          document.removeEventListener('mouseup', onMouseUp);

          this.triggerChartUpdate(widget);
          this.updateContainerMinHeight();
          this.saveToStorage();
          if (this.options.onLayoutChange) {
            this.options.onLayoutChange(this.getLayout());
          }
        };

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
      });
    }

    createPlaceholder(sourceWidget) {
      this.placeholder = document.createElement('div');
      this.placeholder.className = 'grid-stack-placeholder';
      Object.assign(this.placeholder.style, {
        position: 'absolute',
        boxSizing: 'border-box',
        background: 'rgba(105, 108, 255, 0.08)',
        border: '2px dashed #6965fd',
        borderRadius: '8px',
        pointerEvents: 'none',
        zIndex: '999',
        left: sourceWidget.style.left,
        top: sourceWidget.style.top,
        width: sourceWidget.style.width,
        height: sourceWidget.style.height,
        transition: 'all 0.15s'
      });
      this.container.appendChild(this.placeholder);
    }

    swapWidgets(widgetA, widgetB) {
      if (widgetA === widgetB) return;

      const tmpX = widgetA.getAttribute('data-gs-x');
      const tmpY = widgetA.getAttribute('data-gs-y');
      const tmpW = widgetA.getAttribute('data-gs-width');
      const tmpH = widgetA.getAttribute('data-gs-height');
      const tmpLeft = widgetA.style.left;
      const tmpTop = widgetA.style.top;
      const tmpWidth = widgetA.style.width;
      const tmpHeight = widgetA.style.height;
      const tmpZ = widgetA.style.zIndex;

      widgetA.setAttribute('data-gs-x', widgetB.getAttribute('data-gs-x'));
      widgetA.setAttribute('data-gs-y', widgetB.getAttribute('data-gs-y'));
      widgetA.setAttribute('data-gs-width', widgetB.getAttribute('data-gs-width'));
      widgetA.setAttribute('data-gs-height', widgetB.getAttribute('data-gs-height'));
      widgetA.style.left = widgetB.style.left;
      widgetA.style.top = widgetB.style.top;
      widgetA.style.width = widgetB.style.width;
      widgetA.style.height = widgetB.style.height;
      widgetA.style.zIndex = widgetB.style.zIndex;

      widgetB.setAttribute('data-gs-x', tmpX);
      widgetB.setAttribute('data-gs-y', tmpY);
      widgetB.setAttribute('data-gs-width', tmpW);
      widgetB.setAttribute('data-gs-height', tmpH);
      widgetB.style.left = tmpLeft;
      widgetB.style.top = tmpTop;
      widgetB.style.width = tmpWidth;
      widgetB.style.height = tmpHeight;
      widgetB.style.zIndex = tmpZ;

      this.triggerChartUpdate(widgetA);
      this.triggerChartUpdate(widgetB);
      this.updateContainerMinHeight();
    }

    triggerChartUpdate(widget) {
      const widgetId = widget.getAttribute('data-widget-id');
      if (!widgetId || !window.dashboardChartInstances || !window.dashboardChartInstances[widgetId]) return;
      const chart = window.dashboardChartInstances[widgetId];
      const plotHost =
        widget.querySelector('.grid-stack-item-content [id$="Chart"]') ||
        widget.querySelector('.grid-stack-item-content [id*="Chart"]') ||
        widget.querySelector('.grid-stack-item-content .card-body');

      setTimeout(() => {
        let h = 200;
        if (plotHost) {
          const r = plotHost.getBoundingClientRect();
          h = Math.max(80, Math.floor(r.height) || 200);
        }
        try {
          if (chart && typeof chart.updateOptions === 'function') {
            chart.updateOptions({ chart: { height: h } }, false, true);
          } else if (chart && typeof chart.update === 'function') {
            chart.update();
          }
        } catch (err) {
          if (chart && typeof chart.update === 'function') {
            chart.update();
          }
        }
      }, 50);
    }

    getLayout() {
      return this.widgets.map(widget => ({
        id: widget.getAttribute('data-widget-id'),
        x: parseInt(widget.getAttribute('data-gs-x')) || 0,
        y: parseInt(widget.getAttribute('data-gs-y')) || 0,
        w: parseInt(widget.getAttribute('data-gs-width')) || 6,
        h: parseInt(widget.getAttribute('data-gs-height')) || 5
      }));
    }

    applyLayout(layout) {
      if (!layout || !Array.isArray(layout)) return;

      layout.forEach(item => {
        const widget = this.widgets.find(w => w.getAttribute('data-widget-id') === item.id);
        if (widget) {
          widget.setAttribute('data-gs-x', item.x);
          widget.setAttribute('data-gs-y', item.y);
          widget.setAttribute('data-gs-width', item.w);
          widget.setAttribute('data-gs-height', item.h);
        }
      });

      this.applyPositionsFromAttributes();

      setTimeout(() => {
        this.widgets.forEach(w => this.triggerChartUpdate(w));
      }, 100);
    }

    saveToStorage() {
      try {
        this.applyPositionsFromAttributes();
        const layout = this.getLayout();
        localStorage.setItem(this.storageKey, JSON.stringify(layout));
      } catch (e) {
        console.warn('Could not save layout:', e);
      }
    }

    loadFromStorage() {
      try {
        const saved = localStorage.getItem(this.storageKey);
        if (saved) {
          return JSON.parse(saved);
        }
      } catch (e) {
        // ignore
      }
      return null;
    }

    setEditMode(enabled) {
      this.editMode = enabled;
      // Find the nearest parent container that wraps the grid (e.g. #dashboard-grid-container)
      const outer = this.container.closest('[id$="dashboard-grid-container"]') || this.container.parentElement;

      if (enabled) {
        this.container.classList.add('edit-mode', 'grid-edit-mode');
        if (outer && outer !== this.container) outer.classList.add('grid-edit-mode');
        this.widgets.forEach((widget, i) => {
          widget.style.cursor = 'grab';
          widget.classList.add('editable');
          widget.style.zIndex = String(i + 1);
        });
      } else {
        this.container.classList.remove('edit-mode', 'grid-edit-mode');
        if (outer && outer !== this.container) outer.classList.remove('grid-edit-mode');
        this.widgets.forEach((widget, i) => {
          widget.style.cursor = '';
          widget.classList.remove('editable', 'dragging', 'drag-over', 'resizing');
          widget.style.zIndex = String(i + 1);
        });
      }
    }

    toggleEditMode() {
      this.setEditMode(!this.editMode);
      return this.editMode;
    }

    resetLayout() {
      localStorage.removeItem(this.storageKey);
      this.applyPositionsFromAttributes();
      setTimeout(() => location.reload(), 100);
    }

    saveLayout() {
      this.saveToStorage();
    }
  }

  window.DashboardLayoutManager = DashboardLayoutManager;

})();
