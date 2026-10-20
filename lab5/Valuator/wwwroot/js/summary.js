function initializeSummary() {
    const summary = document.getElementById("summary");
    if (!summary || summary.dataset.completed === "true") return;

    const status = document.getElementById("connection-status");
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/summary", {
            transport: signalR.HttpTransportType.WebSockets,
            skipNegotiation: true
        })
        .withAutomaticReconnect()
        .build();

    configureSummaryConnection(connection, summary, status);
}

function configureSummaryConnection(connection, summary, status) {
    let retryTimer;
    const scheduleRetry = () => {
        if (summary.dataset.completed === "true") return;
        status.textContent = "Нет соединения с сервером. Повторное подключение…";
        window.clearTimeout(retryTimer);
        retryTimer = window.setTimeout(start, 3000);
    };

    configureRankResult(connection, summary, status);
    const subscribe = async () => {
        await connection.invoke("Subscribe", summary.dataset.textId);
        status.textContent = "";
    };

    configureSummaryReconnect(connection, status, subscribe, scheduleRetry);
    start();

    async function start() {
        await startSummaryConnection(connection, summary, subscribe, scheduleRetry);
    }
}

function configureRankResult(connection, summary, status) {
    connection.on("RankCalculated", (textId, rank) => {
        if (textId !== summary.dataset.textId) return;
        document.getElementById("rank").textContent = `Оценка содержания: ${rank}`;
        summary.dataset.completed = "true";
        status.textContent = "";
        connection.stop();
    });
}

async function startSummaryConnection(connection, summary, subscribe, scheduleRetry) {
    if (summary.dataset.completed === "true") return;
    if (connection.state !== signalR.HubConnectionState.Disconnected) return;
    try {
        await connection.start();
        await subscribe();
    } catch {
        await connection.stop();
        scheduleRetry();
    }
}

function configureSummaryReconnect(connection, status, subscribe, scheduleRetry) {
    connection.onreconnecting(() => {
        status.textContent = "Восстановление соединения…";
    });
    connection.onreconnected(() => subscribe().catch(() => connection.stop()));
    connection.onclose(scheduleRetry);
}

initializeSummary();
