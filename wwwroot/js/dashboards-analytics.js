/**
 * Dashboard Analytics
 * Supports server-side data injection via window.dashboardCharts.
 * Each Razor view injects chart configs before this file runs.
 */

'use strict';

document.addEventListener('DOMContentLoaded', function (e) {
  // Provide fallback defaults so charts work on all dashboard pages
  if (typeof config === 'undefined') window.config = {};
  if (!window.config.colors) window.config.colors = {};
  if (!window.config.fontFamily) window.config.fontFamily = 'Plus Jakarta Sans';
  let cardColor, headingColor, legendColor, labelColor, borderColor, fontFamily;
  cardColor = window.config.colors.cardColor || '#ffffff';
  headingColor = window.config.colors.headingColor || '#566a7f';
  legendColor = window.config.colors.bodyColor || '#697a8d';
  labelColor = window.config.colors.textMuted || '#a1acb8';
  borderColor = window.config.colors.borderColor || '#eceef1';
  fontFamily = window.config.fontFamily;

  const serverCharts = window.dashboardCharts || {};

  // ============================================================
  // ORDER AREA CHART (#orderChart)
  // ============================================================
  const orderAreaChartEl = document.querySelector('#orderChart');
  if (typeof orderAreaChartEl !== 'undefined' && orderAreaChartEl !== null) {
    const orderData = serverCharts.orderChart;
    let orderSeries = [{ data: [180, 175, 275, 140, 205, 190, 295] }];
    if (orderData && Array.isArray(orderData.series) && orderData.series.length > 0) {
      orderSeries = orderData.series.map(function(s) {
        return { data: Array.isArray(s.data) ? s.data : [] };
      });
    }

    const orderAreaChartConfig = {
      chart: {
        height: 80,
        type: 'area',
        toolbar: { show: false },
        sparkline: { enabled: true }
      },
      markers: {
        size: 6,
        colors: 'transparent',
        strokeColors: 'transparent',
        strokeWidth: 4,
        discrete: [{
          fillColor: cardColor,
          seriesIndex: 0,
          dataPointIndex: Array.isArray(orderSeries[0] && orderSeries[0].data) ? orderSeries[0].data.length - 1 : 6,
          strokeColor: '#71dd88',
          strokeWidth: 2,
          size: 6,
          radius: 8
        }],
        offsetX: -1,
        hover: { size: 7 }
      },
      grid: {
        show: false,
        padding: { top: 15, right: 7, left: 0 }
      },
      colors: ['#71dd88'],
      fill: {
        type: 'gradient',
        gradient: {
          shadeIntensity: 1,
          opacityFrom: 0.4,
          gradientToColors: [cardColor],
          opacityTo: 0.4,
          stops: [0, 100]
        }
      },
      dataLabels: { enabled: false },
      stroke: { width: 2, curve: 'smooth' },
      series: orderSeries,
      xaxis: {
        show: false,
        lines: { show: false },
        labels: { show: false },
        stroke: { width: 0 },
        axisBorder: { show: false }
      },
      yaxis: { stroke: { width: 0 }, show: false }
    };
    new ApexCharts(orderAreaChartEl, orderAreaChartConfig).render();
  }

  // ============================================================
  // TOTAL REVENUE / MAIN BAR CHART (#totalRevenueChart)
  // NOTE: This chart is fully rendered server-side by Razor
  // inline scripts. Skip here to avoid double-rendering.
  // ============================================================

  // ============================================================
  // GROWTH / RADIAL BAR CHART (#growthChart)
  // Fully rendered server-side by Razor inline scripts.
  // ============================================================

  // ============================================================
  // REVENUE BAR CHART (#revenueChart)
  // ============================================================
  const revenueBarChartEl = document.querySelector('#revenueChart');
  if (typeof revenueBarChartEl !== 'undefined' && revenueBarChartEl !== null) {
    const revenueData = serverCharts.revenueChart;
    const revenueSeries = revenueData && revenueData.series && revenueData.series.length
      ? revenueData.series
      : [{ data: [40, 95, 60, 45, 90, 50, 75] }];
    const revenueCats = revenueData && revenueData.categories && revenueData.categories.length
      ? revenueData.categories
      : ['M', 'T', 'W', 'T', 'F', 'S', 'S'];

    const revenueBarChartConfig = {
      chart: {
        height: 95,
        type: 'bar',
        toolbar: { show: false }
      },
      plotOptions: {
        bar: {
          barHeight: '80%',
          columnWidth: '75%',
          startingShape: 'rounded',
          endingShape: 'rounded',
          borderRadius: 4,
          distributed: true
        }
      },
      grid: { show: false, padding: { top: -20, bottom: -12, left: -10, right: 0 } },
      colors: [
        '#6965fd', '#6965fd', '#6965fd',
        '#6965fd', '#6965fd', '#6965fd', '#6965fd'
      ],
      dataLabels: { enabled: false },
      series: revenueSeries,
      legend: { show: false },
      xaxis: {
        categories: revenueCats,
        axisBorder: { show: false },
        axisTicks: { show: false },
        labels: { style: { colors: labelColor, fontSize: '13px' } }
      },
      yaxis: { labels: { show: false } }
    };
    new ApexCharts(revenueBarChartEl, revenueBarChartConfig).render();
  }

  // ============================================================
  // PROFILE REPORT / YEARLY SPARKLINE CHART (#profileReportChart)
  // NOTE: Fully rendered server-side by Razor inline scripts.
  // ============================================================

  // ============================================================
  // ORDER STATISTICS DONUT CHART (#orderStatisticsChart)
  // ============================================================
  const chartOrderStatistics = document.querySelector('#orderStatisticsChart');
  if (typeof chartOrderStatistics !== 'undefined' && chartOrderStatistics !== null) {
    const statsData = serverCharts.orderStatistics;
    const rawStatsSeries = statsData && Array.isArray(statsData.series) && statsData.series.length
      ? statsData.series
      : [{ data: [50, 85, 25, 40] }];
    const statsSeries = rawStatsSeries.map(function(s) {
      return Array.isArray(s) ? s[0] : (s && typeof s === 'object' && typeof s.data === 'number' ? s.data : 0);
    });
    const statsLabels = statsData && statsData.labels ? statsData.labels : ['Electronic', 'Sports', 'Decor', 'Fashion'];
    const statsTotal = statsSeries.reduce(function(a, b) { return a + b; }, 0);

    const orderChartConfig = {
      chart: {
        height: 165,
        width: 136,
        type: 'donut',
        offsetX: 15
      },
      labels: statsLabels,
      series: statsSeries,
      colors: ['#71dd88', '#6965fd', '#8592a3', '#03c3ec'],
      stroke: { width: 5, colors: [cardColor] },
      dataLabels: {
        enabled: false,
        formatter: function (val) { return parseInt(val) + '%'; }
      },
      legend: { show: false },
      grid: { padding: { top: 0, bottom: 0, right: 15 } },
      states: { hover: { filter: { type: 'none' } }, active: { filter: { type: 'none' } } },
      plotOptions: {
        pie: {
          donut: {
            size: '75%',
            labels: {
              show: true,
              value: {
                fontSize: '1.125rem',
                fontFamily: fontFamily,
                fontWeight: 500,
                color: headingColor,
                offsetY: -17,
                formatter: function () { return parseInt(this.globals.seriesTotals[0] / statsTotal * 100) + '%'; }
              },
              name: { offsetY: 17, fontFamily: fontFamily },
              total: {
                show: true,
                fontSize: '13px',
                color: legendColor,
                label: 'Weekly',
                formatter: function () { return '38%'; }
              }
            }
          }
        }
      }
    };
    new ApexCharts(chartOrderStatistics, orderChartConfig).render();
  }

  // ============================================================
  // INCOME / FINANCE AREA CHART (#incomeChart)
  // ============================================================
  const incomeChartEl = document.querySelector('#incomeChart');
  if (typeof incomeChartEl !== 'undefined' && incomeChartEl !== null) {
    const incomeData = serverCharts.incomeChart;
    let incomeSeries = [{ data: [21, 30, 22, 42, 26, 35, 29] }];
    if (incomeData && Array.isArray(incomeData.series) && incomeData.series.length > 0) {
      incomeSeries = incomeData.series.map(function(s) {
        return { data: Array.isArray(s.data) ? s.data : [] };
      });
    }
      const incomeCats = Array.isArray(incomeData && incomeData.categories) && incomeData.categories.length
        ? incomeData.categories
        : ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'];
      const incomeLastIdx = incomeSeries[0] && Array.isArray(incomeSeries[0].data) ? incomeSeries[0].data.length - 1 : 6;

    const incomeChartConfig = {
      series: incomeSeries,
      chart: {
        height: 200,
        parentHeightOffset: 0,
        parentWidthOffset: 0,
        toolbar: { show: false },
        type: 'area'
      },
      dataLabels: { enabled: false },
      stroke: { width: 3, curve: 'smooth' },
      legend: { show: false },
      markers: {
        size: 6,
        colors: 'transparent',
        strokeColors: 'transparent',
        strokeWidth: 4,
        discrete: [{
          fillColor: '#ffffff',
          seriesIndex: 0,
          dataPointIndex: incomeLastIdx,
          strokeColor: '#6965fd',
          strokeWidth: 2,
          size: 6,
          radius: 8
        }],
        offsetX: -1,
        hover: { size: 7 }
      },
      colors: ['#6965fd'],
      fill: {
        type: 'gradient',
        gradient: {
          shadeIntensity: 1,
          opacityFrom: 0.3,
          gradientToColors: [cardColor],
          opacityTo: 0.3,
          stops: [0, 100]
        }
      },
      grid: {
        borderColor: borderColor,
        strokeDashArray: 8,
        padding: { top: -20, bottom: -8, left: 0, right: 8 }
      },
      xaxis: {
        categories: incomeCats,
        axisBorder: { show: false },
        axisTicks: { show: false },
        labels: {
          show: true,
          style: { fontSize: '13px', colors: labelColor }
        }
      },
      yaxis: {
        labels: { show: false },
        min: 10,
        max: 50,
        tickAmount: 4
      }
    };
    new ApexCharts(incomeChartEl, incomeChartConfig).render();
  }

  // ============================================================
  // EXPENSES MINI RADIAL CHART (#expensesOfWeek)
  // ============================================================
  const weeklyExpensesEl = document.querySelector('#expensesOfWeek');
  if (typeof weeklyExpensesEl !== 'undefined' && weeklyExpensesEl !== null) {
    const expenseData = serverCharts.expensesChart;
    const expenseVal = expenseData && expenseData.numericValue ? expenseData.numericValue : 65;

    const weeklyExpensesConfig = {
      series: [Math.min(100, Math.max(0, expenseVal))],
      chart: { width: 60, height: 60, type: 'radialBar' },
      plotOptions: {
        radialBar: {
          startAngle: 0,
          endAngle: 360,
          strokeWidth: '8',
          hollow: { margin: 2, size: '40%' },
          track: { background: borderColor },
          dataLabels: {
            show: true,
            name: { show: false },
            value: {
              formatter: function (val) { return '$' + parseInt(val); },
              offsetY: 5,
              color: legendColor,
              fontSize: '12px',
              fontFamily: fontFamily,
              show: true
            }
          }
        }
      },
      fill: { type: 'solid', colors: '#6965fd' },
      stroke: { lineCap: 'round' },
      grid: { padding: { top: -10, bottom: -15, left: -10, right: -10 } },
      states: { hover: { filter: { type: 'none' } }, active: { filter: { type: 'none' } } }
    };
    new ApexCharts(weeklyExpensesEl, weeklyExpensesConfig).render();
  }
});
