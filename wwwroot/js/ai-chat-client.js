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

    const MAX_MESSAGES_BEFORE_CLEANUP = 100;

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
            const response = await fetch(`/api/aichat/sessions?department=${encodeURIComponent(activeDepartment())}`, {
                credentials: "same-origin",
                headers: { "RequestVerificationToken": getCsrfToken() }
            });

            if (!response.ok) return;

            const sessions = await response.json();
            const list = document.getElementById("aiSessionList");
            if (!list) return;

            let html = `<span class="ai-session-new-btn" onclick="createNewSession()">
                <i class="icon-base bx bx-plus"></i> New session
            </span>`;

            sessions.forEach(function (s) {
                const date = new Date(s.lastMessageAt);
                const timeStr = date.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });
                const isActive = s.sessionId === currentSessionId ? "active" : "";

                html += `<div class="session-item ${isActive}" onclick="selectSession(${s.sessionId})">
                    <i class="icon-base bx bx-chat" style="font-size:14px;"></i>
                    <span style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">
                        ${s.title || "Phien " + s.sessionId}
                    </span>
                    <span style="font-size:10px;color:#aaa;">${timeStr}</span>
                </div>`;
            });

            list.innerHTML = html;

            if (sessions.length > 0 && !currentSessionId) {
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
        try {
            const response = await fetch(`/api/aichat/sessions/${sessionId}/messages`, {
                credentials: "same-origin",
                headers: { "RequestVerificationToken": getCsrfToken() }
            });

            if (!response.ok) return;

            const messages = await response.json();
            const emptyState = document.getElementById("aiEmptyState");
            const quickActions = document.getElementById("aiQuickActions");

            if (!messages.length) {
                if (emptyState) emptyState.style.display = "flex";
                if (quickActions) quickActions.style.display = "";
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
            await chatConnection.invoke("SendMessage", currentSessionId, activeDepartment(), message);
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
                    message: message
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

        streamingAssistantText += chunk;
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

    function appendAssistantMessage(content, createdAt) {
        const container = document.getElementById("aiChatMessages");
        if (!container) return;

        const timeStr = createdAt
            ? new Date(createdAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })
            : new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

        const div = document.createElement("div");
        div.className = "ai-msg assistant";
        div.innerHTML = `
            <div class="ai-msg-avatar"><i class="icon-base bx bx-bot"></i></div>
            <div>
                <div class="ai-msg-bubble">${renderMarkdown(escapeHtml(content))}</div>
                <div class="ai-msg-time">${timeStr}</div>
            </div>`;
        container.appendChild(div);
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
        container.innerHTML = "";
        if (empty) {
            container.innerHTML = empty.outerHTML;
        }
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

})();
