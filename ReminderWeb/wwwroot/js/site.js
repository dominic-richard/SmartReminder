console.log("SITE.JS IS LOADED");
let reminders = [];
let editingReminderId = null;

// Open reminder form
function openReminderForm() {
    document.getElementById("reminderModal").style.display = "flex";
}

// Close reminder form
function closeReminderForm() {
    document.getElementById("reminderModal").style.display = "none";
}


// Create reminder
async function createReminder(event) {

    event.preventDefault();

    const title =
        document.getElementById("reminderTitle").value.trim();

    const date =
        document.getElementById("reminderDate").value;

    const time =
        document.getElementById("reminderTime").value;

    if (!title || !date || !time) {
        alert("Please fill in all fields.");
        return;
    }

    const reminderDateTime =
        `${date}T${time}`;

    try {

        const form =
            event.target;

        const formData =
            new FormData(form);

        formData.append(
            "title",
            title
        );

        formData.append(
            "reminderDateTime",
            reminderDateTime
        );

        // EDIT EXISTING REMINDER
        if (editingReminderId !== null) {

            const response =
                await fetch(
                    `/?handler=EditReminder&id=${editingReminderId}`,
                    {
                        method: "POST",
                        body: formData
                    }
                );

            if (!response.ok) {
                throw new Error(
                    `Edit failed: ${response.status}`
                );
            }

            const result =
                await response.json();

            if (!result.success) {
                throw new Error(
                    "Reminder could not be edited."
                );
            }

            // Update JavaScript reminder
            const reminder =
                reminders.find(
                    reminder =>
                        reminder.id === editingReminderId
                );

            if (reminder) {

                reminder.title = title;

                reminder.date = date;

                reminder.time = time;

                reminder.dateTime =
                    new Date(reminderDateTime);
                reminder.isCompleted = false;
                reminder.completionProof = null;
                reminder.emergencyPromptedAt = null;
                reminder.emergencyResponse = null;
                reminder.pixelEffectStartedAt = null;
                reminder.triggered = false;
                sendDesktopMessage({ type: "pixelReset", id: reminder.id });
            }

            // Update card
            const card =
                document.querySelector(
                    `.preview-card[data-id="${editingReminderId}"]`
                );

            if (card) {

                card.querySelector(
                    ".time"
                ).textContent = time;

                card.querySelector(
                    ".reminder-info h3"
                ).textContent = title;

                card.querySelector(
                    ".reminder-info p"
                ).textContent = date;

                card.classList.remove("reminder-active");
                card.querySelector(
                    ".reminder-info small"
                ).textContent = "Waiting for reminder...";
                const reminderActions = card.querySelector(".reminder-response-actions");
                if (reminderActions) {
                    reminderActions.replaceChildren();
                    reminderActions.hidden = true;
                }
            }

            // Reset edit mode
            editingReminderId = null;

            document.querySelector(
                "#reminderModal .modal-header h2"
            ).textContent = "Create Reminder";

            document.getElementById(
                "reminderTitle"
            ).value = "";

            document.getElementById(
                "reminderDate"
            ).value = "";

            document.getElementById(
                "reminderTime"
            ).value = "";

            closeReminderForm();

            return;
        }

        // CREATE NEW REMINDER
        const response =
            await fetch(
                "/?handler=CreateReminder",
                {
                    method: "POST",
                    body: formData
                }
            );

        if (!response.ok) {
            throw new Error(
                `Create failed: ${response.status}`
            );
        }

        const result =
            await response.json();

        if (!result.success) {
            throw new Error(
                "Reminder could not be created."
            );
        }

        const reminder = {

            id: result.id,

            title: title,

            dateTime:
                new Date(reminderDateTime),

            date: date,

            time: time,

            triggered: false,
            isCompleted: false,
            completionProof: null,
            emergencyPromptedAt: null,
            emergencyResponse: null,
            pixelEffectStartedAt: null,
            emergencyTimeoutPending: false
        };

        reminders.push(reminder);

        displayReminder(reminder);

        document.getElementById(
            "reminderTitle"
        ).value = "";

        document.getElementById(
            "reminderDate"
        ).value = "";

        document.getElementById(
            "reminderTime"
        ).value = "";

        closeReminderForm();

    } catch (error) {
    console.error("Reminder error:", error);
    alert("Reminder error: " + error.message);
}
    }



// Display reminder
function displayReminder(reminder) {

    const reminderList = document.getElementById("reminderList");

    const emptyMessage =
        reminderList.querySelector(".empty-message");

    if (emptyMessage) {
        emptyMessage.remove();
    }

    const card = document.createElement("div");
    card.className = "preview-card reminder-card";
    card.dataset.id = reminder.id;

    const time = document.createElement("div");
    time.className = "time";
    time.textContent = reminder.time;

    const info = document.createElement("div");
    info.className = "reminder-info";

    const title = document.createElement("h3");
    title.textContent = reminder.title;

    const date = document.createElement("p");
    date.textContent = reminder.date;

    const status = document.createElement("small");
    status.textContent = reminder.isCompleted
        ? "✅ Finished"
        : "Waiting for reminder...";
    info.append(title, date, status);

    const managementActions = document.createElement("div");
    managementActions.className = "reminder-actions reminder-management-actions";

    const editButton = document.createElement("button");
    editButton.type = "button";
    editButton.className = "edit-button";
    editButton.textContent = "Edit";
    editButton.addEventListener("click", () => editReminder(reminder.id));

    const deleteButton = document.createElement("button");
    deleteButton.type = "button";
    deleteButton.className = "delete-button";
    deleteButton.textContent = "Delete";
    deleteButton.addEventListener("click", () => deleteReminder(reminder.id));

    managementActions.append(editButton, deleteButton);

    const responseActions = document.createElement("div");
    responseActions.className = "reminder-actions reminder-response-actions";
    responseActions.hidden = true;

    card.append(time, info, managementActions, responseActions);

    reminderList.appendChild(card);

    if (reminder.isCompleted) {
        return;
    }

    if (reminder.pixelEffectStartedAt) {
        activatePixelEffect(reminder, reminder.pixelEffectStartedAt);
        return;
    }

    if (reminder.emergencyPromptedAt && !reminder.emergencyResponse) {
        renderEmergencyChoices(reminder);
        return;
    }

    if (reminder.emergencyResponse === "notDone") {
        status.textContent = "🚨 Emergency marked Not Done. Pixels remain stopped.";
        showFinishedAction(reminder, card);
    }
}
function showBrowserNotification(reminder) {

    if (!("Notification" in window)) {
        return;
    }

    if (Notification.permission === "granted") {

        new Notification("Reminder", {
            body: reminder.title
        });

    } else if (Notification.permission !== "denied") {

        Notification.requestPermission().then(permission => {

            if (permission === "granted") {

                new Notification("Reminder", {
                    body: reminder.title
                });

            }
        });
    }
}
function createReminderActionButton(reminderId, action, label, className) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = className;
    button.textContent = label;
    button.addEventListener("click", () => {
        handleReminderAction(reminderId, action, button);
    });
    return button;
}

function createActionRow(card) {
    const actions = card.querySelector(".reminder-response-actions");
    actions.replaceChildren();
    actions.hidden = false;
    return actions;
}

function sendDesktopMessage(message) {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(message);
    }
}

async function postReminderHandler(handler, reminderId, values = {}) {
    const token = document.querySelector(
        'input[name="__RequestVerificationToken"]'
    )?.value;

    if (!token) {
        throw new Error("The security token is missing. Refresh the page and try again.");
    }

    const formData = new FormData();
    formData.append("__RequestVerificationToken", token);
    Object.entries(values).forEach(([key, value]) => {
        formData.append(key, value);
    });

    const response = await fetch(
        `/?handler=${handler}&id=${reminderId}`,
        {
            method: "POST",
            body: formData
        }
    );

    const result = await response.json();
    if (!response.ok || !result.success) {
        throw new Error(result.error || `${handler} failed (${response.status}).`);
    }

    return result;
}

function showFinishedAction(reminder, card) {
    const actions = createActionRow(card);
    actions.append(
        createReminderActionButton(
            reminder.id,
            "finished",
            "✅ Finished",
            "finished-btn"
        )
    );
}

function renderEmergencyChoices(reminder) {
    const card = document.querySelector(
        `.reminder-card[data-id="${reminder.id}"]`
    );
    if (!card) {
        return;
    }

    const status = card.querySelector(".reminder-info small");
    const actions = createActionRow(card);
    updateEmergencyCountdown(reminder);
    actions.append(
        createReminderActionButton(
            reminder.id,
            "emergencyDone",
            "✅ Emergency Done",
            "finished-btn"
        ),
        createReminderActionButton(
            reminder.id,
            "emergencyNotDone",
            "❌ Emergency Not Done",
            "not-done-btn"
        )
    );
}

function updateEmergencyCountdown(reminder) {
    const card = document.querySelector(
        `.reminder-card[data-id="${reminder.id}"]`
    );
    if (!card || !reminder.emergencyPromptedAt) {
        return;
    }

    const minutesLeft = Math.max(
        0,
        Math.ceil(
            (new Date(reminder.emergencyPromptedAt).getTime() +
                60 * 60 * 1000 -
                Date.now()) / 60000
        )
    );
    card.querySelector(".reminder-info small").textContent =
        `🚨 Emergency follow-up: respond within 1 hour (${minutesLeft} min remaining).`;
}

function activatePixelEffect(reminder, startedAt) {
    const card = document.querySelector(
        `.reminder-card[data-id="${reminder.id}"]`
    );
    if (!card) {
        return;
    }

    reminder.pixelEffectStartedAt = startedAt;
    card.classList.add("reminder-active");
    card.querySelector(".reminder-info small").textContent =
        "⚠️ Pixels grow by 5% every 30 minutes. Submit proof to clear them.";
    showFinishedAction(reminder, card);

    sendDesktopMessage({
        type: "pixelStart",
        id: reminder.id,
        startedAt: new Date(startedAt).toISOString()
    });
}

function triggerReminder(reminder) {
    if (reminder.isCompleted) {
        return;
    }

    if (reminder.emergencyPromptedAt && !reminder.emergencyResponse) {
        updateEmergencyCountdown(reminder);
    }

    const card = document.querySelector(
        `.reminder-card[data-id="${reminder.id}"]`
    );

    if (card) {
        card.classList.add("reminder-active");
        const status = card.querySelector(".reminder-info small");
        if (reminder.pixelEffectStartedAt) {
            activatePixelEffect(reminder, reminder.pixelEffectStartedAt);
        } else if (reminder.emergencyPromptedAt && !reminder.emergencyResponse) {
            renderEmergencyChoices(reminder);
        } else if (reminder.emergencyResponse === "notDone") {
            status.textContent = "🚨 Emergency marked Not Done. Pixels remain stopped.";
        } else {
            status.textContent = "🔔 Reminder is due!";
            const actions = createActionRow(card);
            actions.append(
                createReminderActionButton(reminder.id, "finished", "✅ Finished", "finished-btn"),
                createReminderActionButton(reminder.id, "emergency", "🚨 Emergency", "emergency-btn"),
                createReminderActionButton(reminder.id, "notDone", "❌ Not Done", "not-done-btn")
            );
        }
    }

    sendDesktopMessage({
            type: "reminderDue",
            id: reminder.id,
            title: reminder.title
    });
    showBrowserNotification(reminder);
}

async function handleReminderAction(reminderId, action, button) {
    const card = button.closest(".preview-card[data-id]");
    if (!card) {
        return;
    }
    const status = card.querySelector(".reminder-info small");
    const actions = card.querySelector(".reminder-response-actions");
    const reminder = reminders.find(item => item.id === reminderId);
    if (!reminder) {
        return;
    }

    if (action === "finished") {
        const input = document.createElement("input");
        input.type = "text";
        input.id = `proof-${reminderId}`;
        input.className = "proof-input";
        input.placeholder = "Enter your proof";
        input.maxLength = 2000;
        input.setAttribute("aria-label", "Proof of completion");

        const submitButton = document.createElement("button");
        submitButton.type = "button";
        submitButton.className = "submit-proof-btn";
        submitButton.textContent = "Submit Proof";
        submitButton.addEventListener("click", () => {
            submitFinishedProof(reminder, card, input);
        });

        status.textContent = "Proof required to confirm completion.";
        actions.replaceChildren(input, submitButton);
        input.focus();
        return;
    }

    if (action === "emergency") {
        try {
            const result = await postReminderHandler("StartEmergency", reminderId);
            reminder.emergencyPromptedAt = result.promptedAt;
            reminder.emergencyResponse = null;
            renderEmergencyChoices(reminder);
        } catch (error) {
            alert(`Emergency follow-up could not be started: ${error.message}`);
        }
        return;
    }

    if (action === "notDone") {
        try {
            const result = await postReminderHandler("StartPixels", reminderId);
            activatePixelEffect(reminder, result.pixelEffectStartedAt);
        } catch (error) {
            alert(`Pixel effect could not be started: ${error.message}`);
        }
        return;
    }

    if (action === "emergencyDone" || action === "emergencyNotDone") {
        const response = action === "emergencyDone" ? "done" : "notDone";
        try {
            const result = await postReminderHandler(
                "RespondEmergency",
                reminderId,
                { response }
            );
            reminder.emergencyResponse = response;
            if (result.pixelEffectStartedAt) {
                activatePixelEffect(reminder, result.pixelEffectStartedAt);
            } else {
                status.textContent = "🚨 Emergency marked Not Done. Pixels remain stopped.";
                card.classList.remove("reminder-active");
                showFinishedAction(reminder, card);
                sendDesktopMessage({ type: "pixelReset", id: reminderId });
            }
        } catch (error) {
            alert(`Emergency response could not be saved: ${error.message}`);
        }
    }
}

async function submitFinishedProof(reminder, card, input) {
    const proof = input.value.trim();
    if (!proof) {
        alert("Please enter your proof.");
        input.focus();
        return;
    }

    try {
        await postReminderHandler(
            "CompleteReminder",
            reminder.id,
            { proof }
        );
        reminder.isCompleted = true;
        reminder.completionProof = proof;
        reminder.pixelEffectStartedAt = null;
        reminder.emergencyPromptedAt = null;
        reminder.emergencyResponse = null;

        const status = card.querySelector(".reminder-info small");
        status.textContent = "✅ Finished";
        card.classList.remove("reminder-active");
        card.querySelector(".reminder-response-actions").hidden = true;
        sendDesktopMessage({ type: "pixelReset", id: reminder.id });
    } catch (error) {
        alert(`Completion proof could not be saved: ${error.message}`);
        input.focus();
    }
}

// Check reminders every second
function checkReminders() {

    const now = new Date();

    reminders.forEach(reminder => {
        if (reminder.isCompleted) {
            return;
        }

        if (
            reminder.emergencyPromptedAt &&
            !reminder.emergencyResponse &&
            !reminder.pixelEffectStartedAt &&
            !reminder.emergencyTimeoutPending &&
            Date.now() >= (reminder.emergencyTimeoutRetryAfter || 0) &&
            now.getTime() >= new Date(reminder.emergencyPromptedAt).getTime() + 60 * 60 * 1000
        ) {
            reminder.emergencyTimeoutPending = true;
            postReminderHandler("EmergencyTimeout", reminder.id)
                .then(result => {
                    if (result.started && result.pixelEffectStartedAt) {
                        activatePixelEffect(reminder, result.pixelEffectStartedAt);
                    } else {
                        reminder.emergencyTimeoutRetryAfter = Date.now() + 30000;
                    }
                })
                .catch(error => {
                    console.error("Emergency timeout error:", error);
                    reminder.emergencyTimeoutRetryAfter = Date.now() + 30000;
                })
                .finally(() => {
                    reminder.emergencyTimeoutPending = false;
                });
        }

        if (!reminder.triggered && now >= reminder.dateTime) {
            reminder.triggered = true;
            triggerReminder(reminder);
        }
    });
}

// Check every second
setInterval(() => {

    checkReminders();

}, 1000);
// Load reminders saved in SQLite

if (typeof savedReminders !== "undefined") {

    savedReminders.forEach(savedReminder => {

        const reminder = {

            id: savedReminder.id,

            title: savedReminder.title,

            dateTime: new Date(
                savedReminder.reminderDateTime
            ),

            date:
                savedReminder.reminderDateTime
                .split("T")[0],

            time:
                savedReminder.reminderDateTime
                .split("T")[1]
                .substring(0, 5),

            zones: savedReminder.zones || [],

            isCompleted: savedReminder.isCompleted,
            completionProof: savedReminder.completionProof,
            emergencyPromptedAt: savedReminder.emergencyPromptedAt,
            emergencyResponse: savedReminder.emergencyResponse,
            pixelEffectStartedAt: savedReminder.pixelEffectStartedAt,
            emergencyTimeoutPending: false,
            triggered: false
        };

        reminders.push(reminder);
        displayReminder(reminder);
    });
}
async function deleteReminder(id) {

    try {

        const result = await postReminderHandler("DeleteReminder", id);

        if (result.success) {

            reminders = reminders.filter(
                reminder => reminder.id !== id
            );

            const card =
                document.querySelector(
                    `.preview-card[data-id="${id}"]`
                );

            if (card) {
                card.remove();
            }
            sendDesktopMessage({ type: "pixelReset", id });

        } else {

            alert("Reminder could not be deleted.");

        }

    } catch (error) {

        console.error("Delete error:", error);

        alert("Something went wrong while deleting the reminder.");

    }
}
function editReminder(id) {

    const reminder = reminders.find(
        reminder => reminder.id === id
    );

    if (!reminder) {
        alert("Reminder not found.");
        return;
    }

    // Store the reminder being edited
    editingReminderId = id;

    // Open the existing modal
    document.getElementById("reminderModal").style.display = "flex";

    // Fill the form with existing values
    document.getElementById("reminderTitle").value =
        reminder.title;

    document.getElementById("reminderDate").value =
        reminder.date;

    document.getElementById("reminderTime").value =
        reminder.time;

    // Change modal title
    document.querySelector(
        "#reminderModal .modal-header h2"
    ).textContent = "Edit Reminder";
}
console.log("REMINDER ZONE DATA:", savedReminders);