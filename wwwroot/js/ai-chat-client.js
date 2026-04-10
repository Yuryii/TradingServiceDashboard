"use strict";

(function () {
    let chatConnection = null;
    let currentSessionId = null;
    let isConnected = false;
    let currentDepartment = window.AI_CHAT_DEPARTMENT || "sales";
    let isStreaming = false;
    let currentAssistantBubble = null;
    /** Plain text buffer for streaming; do not re-parse bubble.innerHTML (breaks markup and can stack one <p> per token/line). */
    let streamingAssistantText = "";
    /** Chart config extracted during streaming, rendered after bubble finalization. */
    let pendingChartConfig = null;

    const MAX_MESSAGES_BEFORE_CLEANUP = 100;

    /** Prefer sidebar list on full-page AI Assistant; fallback to floating widget #aiSessionList. */
    function getSessionListEl() {
        const fromSidebar = document.querySelector(".cgpt-sidebar #aiSessionList");
        if (fromSidebar) return fromSidebar;
        return document.getElementById("aiSessionList");
    }

    function activeDepartment() {
        if (typeof window.AI_CHAT_DEPARTMENT === "string" && window.AI_CHAT_DEPARTMENT.length) {
            return window.AI_CHAT_DEPARTMENT;
        }
        return currentDepartment || "sales";
    }

    window.setAIChatSessionId = function (id) {
        currentSessionId = id == null ? null : id;
    };

    function init() {
        if (typeof $.connection !== 'undefined' && $.connection.notificationHub) {
            $.connection.hub.url = "/signalr";
            $.connection.hub.start({ waitForAllDisconnects: true })
                .done(function () {
                    console.log("SignalR connected for notification");
                })
                .fail(function (err) {
                    console.error("SignalR connection failed:", err);
                });
        }

        if (!window.AI_CHAT_STANDALONE) {
            connectAIHub();
        }
    }

    async function connectAIHub() {
        try {
            if (chatConnection && chatConnection.state === signalR.HubConnectionState.Connected) {
                return;
            }
            if (chatConnection) {
                try {
                    await chatConnection.stop();
                } catch (e) { /* ignore */ }
                chatConnection = null;
            }
            chatConnection = new signalR.HubConnectionBuilder()
                .withUrl("/aiChatHub")
                .withAutomaticReconnect([0, 1000, 3000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Warning)
                .build();

            chatConnection.on("ReceiveChunk", function (chunk) {
                appendChunkToAssistantBubble(chunk);
            });

            chatConnection.on("StreamComplete", function () {
                finalizeAssistantBubble();
                stopStreaming();
            });

            chatConnection.on("ReceiveError", function (error) {
                appendErrorToAssistantBubble(error);
                finalizeAssistantBubble();
                stopStreaming();
            });

            chatConnection.onreconnecting(function () {
                updateConnectionStatus(false);
            });

            chatConnection.onreconnected(function () {
                updateConnectionStatus(true);
            });

            chatConnection.onclose(function () {
                updateConnectionStatus(false);
            });

            await chatConnection.start();
            isConnected = true;
            updateConnectionStatus(true);
            console.log("AI Chat Hub connected");
        } catch (err) {
            console.error("AI Chat Hub connection failed:", err);
            updateConnectionStatus(false);
        }
    }

    function updateConnectionStatus(connected) {
        isConnected = connected;
        const dot = document.getElementById("aiStatusDot");
        const text = document.getElementById("aiStatusText");
        if (dot && text) {
            dot.style.background = connected ? "#71dd37" : "#ff3e1d";
            dot.style.boxShadow = connected
                ? "0 0 0 2px rgba(113, 221, 55, 0.25)"
                : "0 0 0 2px rgba(255, 62, 29, 0.2)";
            text.textContent = connected ? "Online" : "Ket noi...";
        }
    }

    window.toggleAIPanel = function () {
        const panel = document.getElementById("ai-chat-panel");
        if (!panel) return;

        const isOpen = panel.classList.contains("open");

        if (isOpen) {
            panel.classList.remove("open");
        } else {
            panel.classList.add("open");
            loadSessionList();
            const input = document.getElementById("aiChatInput");
            if (input) {
                setTimeout(() => input.focus(), 350);
            }
        }
    };

    window.createNewSession = async function (skipEmptyGuard) {
        try {
            if (!skipEmptyGuard && currentSessionId) {
                const check = await fetch(`/api/aichat/sessions/${currentSessionId}/messages`, {
                    credentials: "same-origin",
                    headers: { "RequestVerificationToken": getCsrfToken() }
                });
                if (check.ok) {
                    const existing = await check.json();
                    if (!existing || !existing.length) {
                        showToast("Phien hien tai chua co tin nhan. Hay chat truoc khi tao phien moi.", "error");
                        return;
                    }
                }
            }

            const response = await fetch(`/api/aichat/sessions`, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getCsrfToken()
                },
                body: JSON.stringify({ department: activeDepartment() })
            });

            if (!response.ok) throw new Error("Failed to create session");

            const session = await response.json();
            currentSessionId = session.sessionId;
            clearMessages();
            showWelcome();
            loadSessionList();
            loadSessionHistory(session.sessionId);
        } catch (err) {
            console.error("Create session error:", err);
            showToast("Khong the tao phien moi. Vui long thu lai.", "error");
        }
    };

    async function loadSessionList() {
        try {
            const list = getSessionListEl();
            if (!list) return;

            const response = await fetch(`/api/aichat/sessions?department=${encodeURIComponent(activeDepartment())}`, {
                credentials: "same-origin",
                headers: { "RequestVerificationToken": getCsrfToken() }
            });

            if (!response.ok) {
                list.innerHTML =
                    '<div class="cgpt-session-empty">Unable to load session list. Please reload the page.</div>';
                return;
            }

            const data = await response.json();
            const sessions = Array.isArray(data) ? data : [];

            let html = "";
            if (!window.AI_CHAT_STANDALONE) {
                html = `<span class="ai-session-new-btn" onclick="createNewSession()">
                <i class="icon-base bx bx-plus"></i> New session
            </span>`;
            }

            sessions.forEach(function (s) {
                const date = new Date(s.lastMessageAt);
                const timeStr = date.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });
                const isActive =
                    Number(s.sessionId) === Number(currentSessionId) ? "active" : "";
                const safeTitle = escapeHtml(s.title || ("Session " + s.sessionId));

                html += `<div class="session-item ${isActive}" role="button" tabindex="0" data-session-id="${s.sessionId}" onclick="selectSession(${s.sessionId})">
                    <i class="icon-base bx bx-message-rounded-dots cgpt-session-item-icon"></i>
                    <span class="cgpt-session-item-title">${safeTitle}</span>
                    <span class="cgpt-session-item-time">${timeStr}</span>
                </div>`;
            });

            if (!html) {
                html = '<div class="cgpt-session-empty">No conversations for this department yet.</div>';
            }

            list.innerHTML = html;

            if (!window.AI_CHAT_STANDALONE && sessions.length > 0 && !currentSessionId) {
                selectSession(sessions[0].sessionId);
            }
        } catch (err) {
            console.error("Load sessions error:", err);
        }
    }

    window.selectSession = async function (sessionId) {
        currentSessionId = sessionId;
        clearMessages();
        await loadSessionHistory(sessionId);
        loadSessionList();
    };

    async function loadSessionHistory(sessionId) {
        const container = document.getElementById("aiChatMessages");
        if (container) {
            container.innerHTML =
                '<div id="aiHistoryLoading" class="ai-chat-empty" style="flex-direction:row;gap:8px;">' +
                '<div class="spinner-border spinner-border-sm text-secondary" role="status"></div>' +
                '<span style="font-size:13px;color:var(--bs-secondary-color);">Loading history...</span></div>';
        }

        try {
            const response = await fetch(`/api/aichat/sessions/${sessionId}/messages`, {
                credentials: "same-origin",
                headers: { "RequestVerificationToken": getCsrfToken() }
            });

            const messages = await response.json();
            const emptyState = document.getElementById("aiEmptyState");
            const quickActions = document.getElementById("aiQuickActions");

            if (!response.ok || !messages.length) {
                if (container) {
                    container.innerHTML =
                        '<div class="ai-chat-empty cgpt-empty" id="aiEmptyState">' +
                        '<div class="cgpt-empty-inner">' +
                        '<div class="cgpt-empty-icon"><i class="icon-base bx bx-chat"></i></div>' +
                        '<h2 class="cgpt-empty-title">No messages yet</h2>' +
                        '<p class="cgpt-empty-sub">Start a new conversation.</p>' +
                        '</div></div>';
                }
                if (quickActions) quickActions.style.display = "";
                loadSessionList();
                return;
            }

            if (emptyState) emptyState.style.display = "none";
            if (quickActions) quickActions.style.display = "none";

            messages.forEach(function (msg) {
                if (msg.role === "User" || msg.role === "user") {
                    appendUserMessage(msg.content, msg.createdAt);
                } else {
                    appendAssistantMessage(msg.content, msg.createdAt);
                }
            });

            scrollToBottom();
        } catch (err) {
            console.error("Load history error:", err);
            if (container) {
                container.innerHTML =
                    '<div class="ai-chat-empty cgpt-empty">' +
                    '<div class="cgpt-empty-inner">' +
                    '<div class="cgpt-empty-icon"><i class="icon-base bx bx-wifi-exclamation"></i></div>' +
                    '<h2 class="cgpt-empty-title">Connection error</h2>' +
                    '<p class="cgpt-empty-sub">Unable to load history. Please try again.</p>' +
                    '</div></div>';
            }
        }
    }

    window.sendMessage = async function () {
        if (isStreaming) return;

        const input = document.getElementById("aiChatInput");
        const sendBtn = document.getElementById("aiSendBtn");
        if (!input || !sendBtn) return;

        const message = input.value.trim();
        if (!message) return;

        if (!currentSessionId) {
            await createNewSession(true);
        }

        input.value = "";
        input.style.height = "auto";
        sendBtn.disabled = true;

        const emptyState = document.getElementById("aiEmptyState");
        if (emptyState) emptyState.style.display = "none";

        const quickActions = document.getElementById("aiQuickActions");
        if (quickActions) quickActions.style.display = "none";

        appendUserMessage(message);

        if (!isConnected || !chatConnection || chatConnection.state !== signalR.HubConnectionState.Connected) {
            await sendNonStreamingMessage(message);
            return;
        }

        startStreaming();
        try {
            if (window.AI_TEXT2SQL_MODE) {
                await chatConnection.invoke("SendMessageWithText2Sql", currentSessionId, activeDepartment(), message);
            } else {
                await chatConnection.invoke("SendMessage", currentSessionId, activeDepartment(), message);
            }
        } catch (err) {
            console.error("Send message error:", err);
            appendErrorToAssistantBubble("Da xay ra loi. Vui long thu lai.");
            finalizeAssistantBubble();
            stopStreaming();
        }
    };

    async function sendNonStreamingMessage(message) {
        try {
            startStreaming();
            const response = await fetch(`/api/aichat/stream`, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getCsrfToken()
                },
                body: JSON.stringify({
                    sessionId: currentSessionId,
                    department: activeDepartment(),
                    message: message,
                    useText2Sql: !!window.AI_TEXT2SQL_MODE
                })
            });

            if (!response.ok) throw new Error("Stream failed");

            const reader = response.body.getReader();
            const decoder = new TextDecoder();

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const chunk = decoder.decode(value);
                const lines = chunk.split("\n");

                for (const line of lines) {
                    if (line.startsWith("data: ")) {
                        const data = line.slice(6);
                        if (data === "[DONE]") break;
                        try {
                            const parsed = JSON.parse(data);
                            if (parsed.content) {
                                appendChunkToAssistantBubble(parsed.content);
                            }
                        } catch { }
                    }
                }
            }

            finalizeAssistantBubble();
            stopStreaming();
        } catch (err) {
            console.error("Non-streaming error:", err);
            appendErrorToAssistantBubble("Da xay ra loi. Vui long thu lai.");
            finalizeAssistantBubble();
            stopStreaming();
        }
    }

    function sendQuickMessage(text) {
        const input = document.getElementById("aiChatInput");
        if (input) {
            input.value = text;
            updateSendButton();
            sendMessage();
        }
    }

    window.handleInputKeydown = function (event) {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            sendMessage();
        }
    };

    window.autoResizeInput = function (textarea) {
        textarea.style.height = "auto";
        textarea.style.height = Math.min(textarea.scrollHeight, 100) + "px";
        updateSendButton();
    };

    function updateSendButton() {
        const input = document.getElementById("aiChatInput");
        const sendBtn = document.getElementById("aiSendBtn");
        if (input && sendBtn) {
            sendBtn.disabled = !input.value.trim() || isStreaming;
        }
    }

    function startStreaming() {
        isStreaming = true;
        streamingAssistantText = "";
        pendingChartConfig = null;
        // Only show typing here; create the assistant row on first chunk (avoids empty bubble + typing row).
        currentAssistantBubble = null;
        showTypingIndicator();
        updateSendButton();
    }

    function stopStreaming() {
        isStreaming = false;
        updateSendButton();
        hideTypingIndicator();
        cleanupOldMessages();
        const sessionListEl = getSessionListEl();
        const chatPanel = document.getElementById("ai-chat-panel");
        if (
            sessionListEl &&
            (window.AI_CHAT_STANDALONE || (chatPanel && chatPanel.classList.contains("open")))
        ) {
            loadSessionList();
        }
    }

    function appendUserMessage(content, createdAt) {
        const container = document.getElementById("aiChatMessages");
        if (!container) return;

        const timeStr = createdAt
            ? new Date(createdAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })
            : new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

        const div = document.createElement("div");
        div.className = "ai-msg user";
        div.innerHTML = `
            <div class="ai-msg-avatar"><i class="icon-base bx bx-user"></i></div>
            <div>
                <div class="ai-msg-bubble">${escapeHtml(content)}</div>
                <div class="ai-msg-time">${timeStr}</div>
            </div>`;
        container.appendChild(div);
        scrollToBottom();
    }

    function createAssistantBubble() {
        const container = document.getElementById("aiChatMessages");
        if (!container) return null;

        const div = document.createElement("div");
        div.className = "ai-msg assistant";
        div.innerHTML = `
            <div class="ai-msg-avatar"><i class="icon-base bx bx-bot"></i></div>
            <div>
                <div class="ai-msg-bubble"></div>
                <div class="ai-msg-time"></div>
            </div>`;
        container.appendChild(div);
        return div;
    }

    function appendChunkToAssistantBubble(chunk) {
        hideTypingIndicator();
        if (!currentAssistantBubble) {
            streamingAssistantText = "";
            currentAssistantBubble = createAssistantBubble();
        }

        // Check if this chunk contains a chart config marker
        var chartMatch = chunk.match(/\[CHART_CONFIG:(\{.*?\})\]/);
        if (chartMatch) {
            try {
                var chartConfig = JSON.parse(chartMatch[1]);
                // Remove the chart marker from the displayed text
                var textWithoutChart = chunk.replace(/\[CHART_CONFIG:\{.*?\}\]/, "");
                streamingAssistantText += textWithoutChart;
                // Store in session array and show Open Chart button
                if (!window.aiGeneratedCharts) window.aiGeneratedCharts = [];
                var chartIndex = window.aiGeneratedCharts.length;
                window.aiGeneratedCharts.push(chartConfig);
                // Defer button creation until bubble is finalized
                setTimeout(function () { showOpenChartButton(chartConfig, chartIndex); }, 0);
            } catch (e) {
                console.warn("Invalid chart config JSON:", e);
                streamingAssistantText += chunk;
            }
        } else {
            streamingAssistantText += chunk;
        }

        const bubble = currentAssistantBubble.querySelector(".ai-msg-bubble");
        if (bubble) {
            bubble.innerHTML = renderMarkdown(streamingAssistantText);
        }
        scrollToBottom();
    }

    function appendErrorToAssistantBubble(error) {
        hideTypingIndicator();
        if (!currentAssistantBubble) {
            currentAssistantBubble = createAssistantBubble();
        }

        const bubble = currentAssistantBubble.querySelector(".ai-msg-bubble");
        if (bubble) {
            bubble.innerHTML = `<span style="color:#ea5455;"><i class="icon-base bx bx-error-alt"></i> ${escapeHtml(error)}</span>`;
        }
    }

    function finalizeAssistantBubble() {
        hideTypingIndicator();
        if (currentAssistantBubble) {
            const timeEl = currentAssistantBubble.querySelector(".ai-msg-time");
            if (timeEl) {
                timeEl.textContent = new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
            }
        }
        currentAssistantBubble = null;
        streamingAssistantText = "";
    }

    /**
     * Creates the floating chart panel DOM structure (lazy, called once).
     */
    function createChartPanel() {
        var panel = document.createElement("div");
        panel.id = "ai-chart-panel";
        panel.className = "ai-chart-panel";

        // Backdrop
        var backdrop = document.createElement("div");
        backdrop.id = "ai-chart-backdrop";
        backdrop.className = "ai-chart-panel-backdrop";
        backdrop.onclick = closeAiChartPanel;

        panel.innerHTML =
            '<div class="ai-chart-panel-header">' +
            '<h6 class="ai-chart-panel-title" id="aiChartPanelTitle">Biểu đồ</h6>' +
            '<div class="ai-chart-panel-actions">' +
            '<button type="button" class="btn btn-sm btn-outline-primary" id="aiChartDownloadPng" title="Tai PNG">' +
            '<i class="icon-base bx bx-download"></i> PNG</button>' +
            '<button type="button" class="btn btn-sm btn-outline-primary" id="aiChartDownloadCsv" title="Tai CSV">' +
            '<i class="icon-base bx bx-file"></i> CSV</button>' +
            '<button type="button" class="ai-chart-panel-close" id="aiChartCloseBtn">' +
            '<i class="icon-base bx bx-x"></i></button>' +
            '</div></div>' +
            '<div class="ai-chart-panel-body">' +
            '<div id="aiChartPanelContent"></div></div>' +
            '<div class="ai-chart-panel-footer">' +
            '<small class="text-muted">Nguon: AI Assistant</small></div>';

        document.body.appendChild(panel);
        document.body.appendChild(backdrop);

        // Wire up close button
        document.getElementById("aiChartCloseBtn").onclick = closeAiChartPanel;

        // Wire up PNG download
        document.getElementById("aiChartDownloadPng").onclick = function () {
            if (window._currentApexChartInstance) {
                window._currentApexChartInstance.dataURI().then(function (_a) {
                    window._currentApexChartInstance.exportToPNG();
                });
            }
        };

        // Wire up CSV download
        document.getElementById("aiChartDownloadCsv").onclick = function () {
            if (window._currentApexChartInstance) {
                window._currentApexChartInstance.exportToCSV();
            }
        };

        return panel;
    }

    /** Opens the floating chart panel with the given config. */
    window.openAiChartPanel = function (chartIndex) {
        var charts = window.aiGeneratedCharts || [];
        var config = charts[chartIndex];
        if (!config) return;

        var panel = document.getElementById("ai-chart-panel");
        if (!panel) {
            panel = createChartPanel();
        }

        renderChartInPanel(config);
        panel.classList.add("open");

        var backdrop = document.getElementById("ai-chart-backdrop");
        if (backdrop) backdrop.classList.add("visible");
    };

    /** Closes the floating chart panel. */
    window.closeAiChartPanel = function () {
        var panel = document.getElementById("ai-chart-panel");
        if (panel) panel.classList.remove("open");
        var backdrop = document.getElementById("ai-chart-backdrop");
        if (backdrop) backdrop.classList.remove("visible");
    };

    /**
     * Shows the "Open Chart" button after the last assistant bubble,
     * and stores the chart config in the session array.
     */
    function showOpenChartButton(chartConfig, chartIndex) {
        var container = document.getElementById("aiChatMessages");
        if (!container) return;
        var lastMsg = container.querySelector(".ai-msg.assistant:last-child");
        if (!lastMsg) return;

        // Avoid duplicates
        if (lastMsg.querySelector(".ai-chart-action")) return;

        var btnDiv = document.createElement("div");
        btnDiv.className = "ai-chart-action";
        btnDiv.innerHTML =
            '<button type="button" class="btn btn-sm btn-primary" ' +
            'onclick="openAiChartPanel(' + chartIndex + ')">' +
            '<i class="icon-base bx bx-bar-chart-alt-2"></i> Mo biểu đồ</button>';

        var bubbleContainer = lastMsg.querySelector(".ai-msg-bubble");
        if (bubbleContainer) {
            bubbleContainer.parentNode.appendChild(btnDiv);
        } else {
            lastMsg.appendChild(btnDiv);
        }
        scrollToBottom();
    }

    /**
     * Renders an ApexCharts chart inside the floating panel.
     */
    function renderChartInPanel(chartConfig) {
        if (typeof ApexCharts === "undefined") return;

        var chartType = chartConfig.chartType || "bar";
        var title = chartConfig.title || "Biểu đồ";
        var subtitle = chartConfig.subtitle || "";
        var xaxis = chartConfig.xaxis || "";
        var yaxis = chartConfig.yaxis || "";
        var categories = Array.isArray(chartConfig.categories) ? chartConfig.categories : [];
        var series = Array.isArray(chartConfig.series) ? chartConfig.series : [];
        var colors = Array.isArray(chartConfig.colors) ? chartConfig.colors : ["#6965fd", "#03c3ec", "#71dd88", "#ffab00"];
        var unit = chartConfig.unit || "";
        var height = chartConfig.height || 400;

        // Update panel title
        var titleEl = document.getElementById("aiChartPanelTitle");
        if (titleEl) titleEl.textContent = title;

        // Map horizontalBar to bar with horizontal option
        var apexType = (chartType === "horizontalBar") ? "bar" : chartType;

        // Number formatter
        var formatter = function (val) {
            if (typeof val !== "number") return val;
            if (unit === "VND" || unit === "VNĐ") {
                if (Math.abs(val) >= 1000000000) return (val / 1000000000).toFixed(1) + "B";
                if (Math.abs(val) >= 1000000) return (val / 1000000).toFixed(1) + "M";
                if (Math.abs(val) >= 1000) return (val / 1000).toFixed(1) + "K";
                return val.toString();
            }
            if (unit === "$") return "$" + val.toLocaleString();
            if (unit === "%") return val.toFixed(1) + "%";
            if (val >= 1000000) return (val / 1000000).toFixed(1) + "M";
            if (val >= 1000) return (val / 1000).toFixed(1) + "K";
            return val.toString();
        };

        // Theme colors
        var cfg = window.config || {};
        var cols = cfg.colors || {};
        var labelColor = cols.textMuted || "#a1acb8";
        var borderColor = cols.borderColor || "#eceef1";
        var fontFamily = cfg.fontFamily || "Plus Jakarta Sans";

        var chartOptions = {
            chart: {
                height: height,
                type: apexType,
                toolbar: { show: true, tools: { download: true } },
                fontFamily: fontFamily,
                foreColor: labelColor,
                background: "transparent",
                animations: { enabled: true, speed: 400 }
            },
            colors: colors,
            dataLabels: { enabled: false },
            stroke: { curve: "smooth", width: 2 },
            legend: {
                show: true,
                position: "top",
                horizontalAlign: "left",
                fontSize: "12px",
                fontFamily: fontFamily,
                labels: { colors: labelColor },
                markers: { size: 4, radius: 12, shape: "circle" }
            },
            grid: {
                strokeDashArray: 7,
                borderColor: borderColor,
                padding: { top: -10, bottom: -10, left: 10, right: 10 }
            },
            series: series,
            xaxis: {
                categories: categories,
                labels: { style: { fontSize: "12px", fontFamily: fontFamily, colors: labelColor } },
                axisTicks: { show: false },
                axisBorder: { show: false },
                title: xaxis ? { text: xaxis, style: { fontSize: "12px", color: labelColor } } : undefined
            },
            yaxis: {
                labels: {
                    style: { fontSize: "13px", fontFamily: fontFamily, colors: labelColor },
                    formatter: formatter
                },
                title: yaxis ? { text: yaxis, style: { fontSize: "12px", color: labelColor } } : undefined
            },
            tooltip: { theme: "dark", y: { formatter: formatter } }
        };

        // Type-specific options
        if (apexType === "bar" && chartType === "horizontalBar") {
            chartOptions.plotOptions = {
                bar: { horizontal: true, barHeight: "65%", borderRadius: 6, borderRadiusApplication: "end" }
            };
        } else if (apexType === "bar") {
            chartOptions.plotOptions = {
                bar: { horizontal: false, columnWidth: "40%", borderRadius: 6, borderRadiusApplication: "end" }
            };
        } else if (apexType === "area") {
            chartOptions.fill = {
                opacity: 0.85,
                type: "gradient",
                gradient: {
                    shade: "light",
                    type: "vertical",
                    shadeIntensity: 0.5,
                    gradientToColors: [colors[0] + "44", (colors[1] || colors[0]) + "44"],
                    opacityFrom: 0.85,
                    opacityTo: 0.55,
                    stops: [0, 100]
                }
            };
        } else if (apexType === "pie" || apexType === "donut") {
            chartOptions.labels = categories;
            chartOptions.legend = {
                show: true,
                position: "bottom",
                horizontalAlign: "center",
                fontSize: "12px",
                fontFamily: fontFamily,
                labels: { colors: labelColor },
                markers: { size: 4, radius: 12, shape: "circle" }
            };
            chartOptions.dataLabels = { enabled: true, formatter: function (val) { return val.toFixed(1) + "%"; } };
            chartOptions.plotOptions = apexType === "donut"
                ? { pie: { donut: { size: "65%", labels: { show: true, name: { show: true }, value: { show: true, formatter: formatter } } } } }
                : {};
        }

        // Destroy previous chart instance
        if (window._currentApexChartInstance) {
            try { window._currentApexChartInstance.destroy(); } catch (e) { /* ignore */ }
            window._currentApexChartInstance = null;
        }

        var contentEl = document.getElementById("aiChartPanelContent");
        if (!contentEl) return;

        contentEl.innerHTML = "";

        try {
            window._currentApexChartInstance = new ApexCharts(contentEl, chartOptions);
            window._currentApexChartInstance.render();
        } catch (e) {
            console.error("Failed to render chart:", e);
            contentEl.innerHTML = "<p style='color:#ea5455;padding:16px;'>Loi hien thi bieu do.</p>";
        }
    }

    function appendAssistantMessage(content, createdAt) {
        isStreaming = false;
        if (currentAssistantBubble) {
            finalizeAssistantBubble();
        }

        const container = document.getElementById("aiChatMessages");
        if (!container) return;

        const timeStr = createdAt
            ? new Date(createdAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })
            : new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

        // Extract chart config from content if present
        var chartConfig = null;
        var displayContent = content;
        var chartMatch = content.match(/\[CHART_CONFIG:(\{.*?\})\]/);
        if (chartMatch) {
            try {
                chartConfig = JSON.parse(chartMatch[1]);
                displayContent = content.replace(/\[CHART_CONFIG:\{.*?\}\]/, "");
            } catch (e) {
                console.warn("Invalid chart config JSON:", e);
            }
        }

        const div = document.createElement("div");
        div.className = "ai-msg assistant";
        div.innerHTML = `
            <div class="ai-msg-avatar"><i class="icon-base bx bx-bot"></i></div>
            <div>
                <div class="ai-msg-bubble">${renderMarkdown(escapeHtml(displayContent))}</div>
                <div class="ai-msg-time">${timeStr}</div>
            </div>`;
        container.appendChild(div);

        // Show Open Chart button if chart config is present
        if (chartConfig) {
            if (!window.aiGeneratedCharts) window.aiGeneratedCharts = [];
            var chartIndex = window.aiGeneratedCharts.length;
            window.aiGeneratedCharts.push(chartConfig);
            showOpenChartButton(chartConfig, chartIndex);
        }

        scrollToBottom();
    }

    function showTypingIndicator() {
        const existing = document.getElementById("aiTypingIndicator");
        if (existing) return;

        const container = document.getElementById("aiChatMessages");
        if (!container) return;

        const div = document.createElement("div");
        div.id = "aiTypingIndicator";
        div.className = "ai-msg assistant";
        div.innerHTML = `
            <div class="ai-msg-avatar"><i class="icon-base bx bx-bot"></i></div>
            <div class="ai-msg-typing">
                <span></span><span></span><span></span>
            </div>`;
        container.appendChild(div);
        scrollToBottom();
    }

    function hideTypingIndicator() {
        const el = document.getElementById("aiTypingIndicator");
        if (el) el.remove();
    }

    function clearMessages() {
        const container = document.getElementById("aiChatMessages");
        if (!container) return;

        const empty = document.getElementById("aiEmptyState");
        const quickActions = document.getElementById("aiQuickActions");
        const dept = window.AI_CHAT_DEPARTMENT || "sales";

        container.innerHTML =
            '<div class="ai-chat-empty cgpt-empty" id="aiEmptyState">' +
            '<div class="cgpt-empty-inner">' +
            '<div class="cgpt-empty-icon"><i class="icon-base bx ' + getDeptIcon(dept) + '"></i></div>' +
            '<h2 class="cgpt-empty-title">History cleared</h2>' +
            '<p class="cgpt-empty-sub">Type a question below to start a new conversation.</p>' +
            '</div></div>';

        if (quickActions) quickActions.style.display = "";
    }

    function showWelcome() {
        const container = document.getElementById("aiChatMessages");
        if (!container) return;

        const empty = document.getElementById("aiEmptyState");
        if (empty) empty.style.display = "flex";

        const quickActions = document.getElementById("aiQuickActions");
        if (quickActions) quickActions.style.display = "";
    }

    function scrollToBottom() {
        const container = document.getElementById("aiChatMessages");
        if (container) {
            setTimeout(() => { container.scrollTop = container.scrollHeight; }, 10);
        }
    }

    function cleanupOldMessages() {
        const container = document.getElementById("aiChatMessages");
        if (!container) return;

        const messages = container.querySelectorAll(".ai-msg");
        if (messages.length > MAX_MESSAGES_BEFORE_CLEANUP) {
            const toRemove = Array.from(messages).slice(0, messages.length - MAX_MESSAGES_BEFORE_CLEANUP);
            toRemove.forEach(function (m) { m.remove(); });
        }
    }

    function renderMarkdown(text) {
        if (!text) return "";

        let html = text.replace(/\r\n/g, "\n");
        // Single newlines inside a paragraph become spaces; blank lines still separate blocks (avoids one <p> per line/word).
        html = html.split(/\n\n/).map(function (block) {
            return block.replace(/\n/g, " ");
        }).join("\n\n");

        html = html.replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>");
        html = html.replace(/\*(.+?)\*/g, "<em>$1</em>");
        html = html.replace(/`(.+?)`/g, "<code>$1</code>");

        const lines = html.split("\n");
        let inList = false;
        let result = "";

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];

            if (line.match(/^#{1,6}\s/)) {
                if (inList) { result += "</ul>"; inList = false; }
                const level = line.match(/^(#+)/)[1].length;
                result += `<p style="font-weight:600;font-size:${18 - level}px;margin:8px 0 4px;">${line.replace(/^#+\s/, "")}</p>`;
            } else if (line.match(/^[-*]\s/)) {
                if (!inList) { result += "<ul>"; inList = true; }
                result += `<li>${line.replace(/^[-*]\s/, "")}</li>`;
            } else if (line.trim() === "") {
                if (inList) { result += "</ul>"; inList = false; }
                result += "<br>";
            } else {
                if (inList) { result += "</ul>"; inList = false; }
                result += `<p>${line}</p>`;
            }
        }

        if (inList) result += "</ul>";
        return result;
    }

    function escapeHtml(text) {
        if (!text) return "";
        const div = document.createElement("div");
        div.textContent = text;
        return div.innerHTML;
    }

    function getCsrfToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : "";
    }

    function showToast(message, type) {
        if (typeof Swal !== "undefined") {
            Swal.fire({
                toast: true,
                position: "top-end",
                icon: type === "error" ? "error" : "success",
                title: message,
                showConfirmButton: false,
                timer: 3000
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    window.sendQuickMessage = sendQuickMessage;
    window.getCsrfToken = getCsrfToken;
    window.connectAIHub = connectAIHub;
    window.loadAIChatSessionList = loadSessionList;

})();
