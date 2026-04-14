const conditionButtons = [
    "btnBaned",
    "btnBlessed",
    "btnBlinded",
    "btnCharmed",
    "btnConcentration",
    "btnDeafened",
    "btnDeath",
    "btnExhausted",
    "btnFlying",
    "btnFrightened",
    "btnGrappled",
    "btnHasted",
    "btnHex",
    "btnHighlighted",
    "btnIncapacitated",
    "btnInvisible",
    "btnMark",
    "btnParalyzed",
    "btnPetrified",
    "btnPoisoned",
    "btnProne",
    "btnRestrained",
    "btnStabilized",
    "btnStunned",
    "btnUnconcious"
]

const heightCondition = 25;

let isInitialized = false;
let activeConditions = {};
let visibleConditions = {}; // Track which conditions are visible in the UI
let closedConditions = {}; // Track which conditions have been manually closed by the user
let isPanelManuallyHidden = false; // Track if user manually closed the panel

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/WebHub")
    .withAutomaticReconnect()
    .build();

function getSettings() {
    let cookie = {};
    decodeURIComponent(document.cookie).split(';').forEach(function (el) {
        let [key, value] = el.split('=');
        cookie[key.trim()] = value;
    })

    if (cookie.hasOwnProperty("_settings")) {
        return JSON.parse(cookie["_settings"]);
    }
    else {
        return null;
    }
}

function getConditions() {
    let character = $("#tokens").val();

    if (!character)
        return;

    $.ajax({
        url: "Navigation/GetConditions",
        type: "POST",
        data: { 'character': character }
    })
}

function updateOrientation() {
    let settings = getSettings();
    if (settings == null) {
        return "fa-arrow-circle-up";
    }
    console.log(settings.Orientation);

    if (settings.Orientation == "0") {
        return "fa-arrow-circle-left";
    }
    else if (settings.Orientation == "1") {
        return "fa-arrow-circle-down";
    }
    else if (settings.Orientation == "2") {
        return "fa-arrow-circle-right";
    }
    else {
        currentOrientation = "Up";
        return "fa-arrow-circle-up";
    }
}

function initializeServerConnection() {
    if (!isInitialized) {
        isInitialized = true;

        $.ajax({
            url: "Navigation/SetOrientation",
            type: "POST"
        })
    }
}

function setTokens(tokens) {
    initializeServerConnection();

    let select = document.getElementById("tokens");

    while (select.options.length > 0) {
        select.remove(0);
    }

    for (const token of tokens) {
        let option = document.createElement("option");
        option.text = token;
        option.value = token;
        select.add(option);
    }

    getConditions();
}

connection.on("SetConditions", function (character, conditions) {
    let selectedCharacter = $("#tokens").val();

    if (character.localeCompare(selectedCharacter, undefined, { sensitivity: 'accent' }) === 0) {
        for (const button of conditionButtons) {
            document.getElementById(button).style.backgroundColor = '';
        }

        let appliedConditions = [];
        for (const condition of conditions) {
            if (condition != heightCondition) {
                document.getElementById(conditionButtons[condition]).style.backgroundColor = '#303538';
                appliedConditions.push(conditionButtons[condition]);
            }
        }

        // Update the info panel to only show applied conditions
        syncConditionPanelWithAppliedConditions(appliedConditions);
    }
});

connection.on("SetTokens", function (player, tokens) {
    let settings = getSettings();

    if (settings != null && settings.Name.localeCompare(player, undefined, { sensitivity: 'accent' }) === 0) {
        setTokens(tokens);
    }
});

connection.on("SetCampaign", function (players) {
    let settings = getSettings();
    if (settings == null) {
        return;
    }

    initializeServerConnection();

    for (const player in players) {
        if (settings.Name.localeCompare(player, undefined, { sensitivity: 'accent' }) === 0) {
            setTokens(players[player])
        }
    }
});

$(document).ready(function () {
    $(".btn-direction").click(function() {
        let character = $("#tokens").val();
        let direction = $(this).attr('direction');

        console.log(character);

        // Collapse conditions
        $('.collapsible-conditions-content').hide();

        // If direction is undefined, we hit te center button
        if (!direction || !character)
            return;
        
        $.ajax({
            url: "Navigation/Move",
            type: "POST",
            data: { 'character': character, 'direction': direction }
        })
    })

    $(".btn-condition").click(function () {
        let character = $("#tokens").val();
        let condition = $(this).attr('condition');

        if (!character)
            return;

        $.ajax({
            url: "Navigation/ToggleCondition",
            type: "POST",
            data: { 'character': character, 'condition': condition}
        })
    })

    $("#tokens").change(function () {
        let character = $("#tokens").val();

        for (const button of conditionButtons) {
            document.getElementById(button).style.backgroundColor = '';
        }

        $.ajax({
            url: "Navigation/GetConditions",
            type: "POST",
            data: { 'character': character }
        })
    })

    $(".btn-collapsible-conditions").click(function () {
        $('.collapsible-conditions-content').toggle();
    })   

    $(".btn-orientation").click(function () {
        // Collapse conditions
        $('.collapsible-conditions-content').hide();

        document.getElementById("btnOrientation").className = "fa fa-2x " + updateOrientation();

        $.ajax({
            url: "Navigation/ChangeOrientation",
            type: "POST"
        })        
    })

    $(".btn-apply-height").click(function () {
        let character = $("#tokens").val();
        let height = $("#height").val();
        $("#height").val("");

        $.ajax({
            url: "Navigation/SetHeight",
            type: "POST",
            data: { 'character': character, 'height': height }
        })
    })

    $("#closeConditionInfo").click(function () {
        hideConditionPanel();
        updateConditionInfoButtonVisibility();
    })

    $("#btnOpenConditionInfo").click(function () {
        // Reload visible conditions when reopening
        reloadActiveConditions();
        showConditionPanel();
        updateConditionInfoButtonVisibility();
    })
});

async function start() {
    try {
        // Initialize button visibility on startup
        updateConditionInfoButtonVisibility();

        await connection.start();

        // Request tokens with refresh
        let settings = getSettings();
        if (settings != null) {
            $.ajax({
                url: "Navigation/GetTokens",
                type: "GET",
                data: { 'player': settings.Name },
                success: function (tokens) {
                    if (tokens != "") {
                        let jsonObject = JSON.parse(tokens);
                        setTokens(jsonObject);
                    }

                }
            })
        }
        else {
            $("#navigationMain").empty();
            $("#navigationMain").append("<p><b>No settings found!</b></p><p>Please go to settings and enter your name.</p>");
        }
    } catch (error) {
        console.log(error);
    }
};

function addConditionInfo(conditionName, callback) {
    $.ajax({
        url: "Navigation/GetConditionInfo",
        type: "GET",
        data: { 'conditionName': conditionName },
        success: function (response) {
            let conditionData = JSON.parse(response);
            let settings = getSettings();
            let version = (settings && settings.ConditionVersion) ? settings.ConditionVersion : "5.5e";
            
            if (conditionData.versions && conditionData.versions[version]) {
                let versionData = conditionData.versions[version];
                
                activeConditions[conditionName] = versionData;
                renderConditionCard(conditionName, versionData);
                
                // Call the callback if provided
                if (callback) {
                    callback();
                }
            }
        },
        error: function (error) {
            console.error("Error loading condition info:", error);
            // Still call the callback even on error to prevent hanging
            if (callback) {
                callback();
            }
        }
    });
}

function removeConditionInfo(conditionName) {
    delete visibleConditions[conditionName];
    closedConditions[conditionName] = true;
    renderConditionCards();
    
    // Don't auto-close the panel when closing cards
    // Let user manually close it if they want
    updateConditionInfoButtonVisibility();
}

function syncConditionPanelWithAppliedConditions(appliedButtonIds) {
    // Get the condition names from the applied button IDs
    let appliedConditionNames = appliedButtonIds.map(buttonId => {
        let button = document.getElementById(buttonId);
        if (button) {
            return button.querySelector('.span-condition').textContent;
        }
        return null;
    }).filter(name => name !== null);

    // Remove any conditions from visibleConditions that are no longer applied
    for (const conditionName of Object.keys(visibleConditions)) {
        if (!appliedConditionNames.includes(conditionName)) {
            delete visibleConditions[conditionName];
        }
    }

    // Clear closed conditions that are no longer applied (they can be re-added fresh)
    for (const conditionName of Object.keys(closedConditions)) {
        if (!appliedConditionNames.includes(conditionName)) {
            delete closedConditions[conditionName];
        }
    }

    // Track how many conditions need to be loaded
    let conditionsToLoad = 0;
    for (const conditionName of appliedConditionNames) {
        // Only add to visible if it's not manually closed and not already visible
        if (!closedConditions[conditionName] && !visibleConditions[conditionName]) {
            visibleConditions[conditionName] = true;
            if (!activeConditions[conditionName]) {
                conditionsToLoad++;
            }
        }
    }

    // If there are no conditions to load, update immediately
    if (conditionsToLoad === 0) {
        renderConditionCards();
        // Don't auto-open if user manually closed it
        if (!isPanelManuallyHidden && Object.keys(visibleConditions).length > 0) {
            showConditionPanel();
        }
        return;
    }

    // Add conditions and track when all are loaded
    let loadedCount = 0;
    for (const conditionName of appliedConditionNames) {
        if (!closedConditions[conditionName] && !activeConditions[conditionName]) {
            addConditionInfo(conditionName, () => {
                loadedCount++;
                // When all conditions are loaded, update the panel
                if (loadedCount === conditionsToLoad) {
                    renderConditionCards();
                    // Only auto-open if user hasn't manually closed it
                    if (!isPanelManuallyHidden && Object.keys(visibleConditions).length > 0) {
                        showConditionPanel();
                    }
                }
            });
        }
    }
}

function renderConditionCard(conditionName, versionData) {
    let container = document.getElementById("conditionCardsContainer");
    
    if (!document.getElementById(`condition-card-${conditionName}`)) {
        let card = document.createElement("div");
        card.className = "condition-card";
        card.id = `condition-card-${conditionName}`;
        card.innerHTML = `
            <div class="condition-card-header">
                <img class="condition-card-icon" src="/ConditionIcons/${conditionName}.png" alt="${conditionName}" />
                <h6 class="condition-card-title">${conditionName}</h6>
                <button type="button" class="condition-card-close" data-condition="${conditionName}">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
            <p class="condition-card-description">${versionData.description}</p>
            ${versionData.link && versionData.link !== "" ? `<a class="condition-card-link" href="${versionData.link}" target="_blank">Learn more</a>` : ""}
        `;
        container.appendChild(card);
        
        // Add event listener to close button
        card.querySelector(".condition-card-close").addEventListener("click", function(e) {
            e.stopPropagation();
            removeConditionInfo(conditionName);
        });
    }
}

function renderConditionCards() {
    let container = document.getElementById("conditionCardsContainer");
    container.innerHTML = "";
    
    // Only render conditions that are both active AND visible
    for (const [conditionName, versionData] of Object.entries(activeConditions)) {
        if (visibleConditions[conditionName]) {
            renderConditionCard(conditionName, versionData);
        }
    }
}

function showConditionPanel() {
    document.getElementById("conditionInfoPanel").style.display = "block";
    isPanelManuallyHidden = false;
    updateConditionInfoButtonVisibility();
}

function hideConditionPanel() {
    document.getElementById("conditionInfoPanel").style.display = "none";
    isPanelManuallyHidden = true;
    updateConditionInfoButtonVisibility();
}

function updateConditionInfoButtonVisibility() {
    let btnOpenConditionInfo = document.getElementById("btnOpenConditionInfo");
    let btnCollapsibleConditions = document.querySelector(".btn-collapsible-conditions");
    let conditionPanel = document.getElementById("conditionInfoPanel");
    let hasVisibleConditions = Object.keys(visibleConditions).length > 0;
    let isPanelHidden = conditionPanel.style.display === "none";
    
    // Show the button if there are visible conditions AND the panel is hidden
    if (hasVisibleConditions && isPanelHidden) {
        btnOpenConditionInfo.style.display = "block";
        btnCollapsibleConditions.classList.remove("btn-full-width");
    } else {
        btnOpenConditionInfo.style.display = "none";
        btnCollapsibleConditions.classList.add("btn-full-width");
    }
}

function reloadActiveConditions() {
    // Reload only the conditions that are visible (were not closed by user)
    let conditionsToLoad = 0;
    for (const conditionName of Object.keys(visibleConditions)) {
        if (!activeConditions[conditionName]) {
            conditionsToLoad++;
        }
    }
    
    if (conditionsToLoad === 0) {
        renderConditionCards();
        return;
    }
    
    let loadedCount = 0;
    for (const conditionName of Object.keys(visibleConditions)) {
        if (!activeConditions[conditionName]) {
            addConditionInfo(conditionName, () => {
                loadedCount++;
                if (loadedCount === conditionsToLoad) {
                    renderConditionCards();
                }
            });
        }
    }
}

start();

