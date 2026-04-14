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

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/WebHub")
    .withAutomaticReconnect()
    .build();

function updateOrientation() {
    const settings = SettingsManager.getSettings();
    if (settings == null) {
        return "fa-arrow-circle-up";
    }
    
    const orientationMap = { "0": "fa-arrow-circle-left", "1": "fa-arrow-circle-down", "2": "fa-arrow-circle-right" };
    return orientationMap[settings.Orientation] || "fa-arrow-circle-up";
}

function initializeServerConnection() {
    if (!isInitialized) {
        isInitialized = true;
        $.ajax({ url: "Navigation/SetOrientation", type: "POST" });
    }
}

function setTokens(tokens) {
    initializeServerConnection();

    const select = document.getElementById("tokens");
    while (select.options.length > 0) select.remove(0);

    tokens.forEach(token => {
        const option = document.createElement("option");
        option.text = token;
        option.value = token;
        select.add(option);
    });

    getConditions();
}

function getConditions() {
    const character = $("#tokens").val();
    if (character) {
        $.ajax({ url: "Navigation/GetConditions", type: "POST", data: { 'character': character } });
    }
}

connection.on("SetConditions", (character, conditions) => {
    const selectedCharacter = $("#tokens").val();

    if (character.localeCompare(selectedCharacter, undefined, { sensitivity: 'accent' }) === 0) {
        conditionButtons.forEach(button => {
            document.getElementById(button).style.backgroundColor = '';
        });

        const appliedConditions = [];
        conditions.forEach(condition => {
            if (condition != heightCondition) {
                document.getElementById(conditionButtons[condition]).style.backgroundColor = '#303538';
                appliedConditions.push(conditionButtons[condition]);
            }
        });

        ConditionManager.syncWithAppliedConditions(appliedConditions);
    }
});

connection.on("SetTokens", (player, tokens) => {
    const settings = SettingsManager.getSettings();
    if (settings != null && settings.Name.localeCompare(player, undefined, { sensitivity: 'accent' }) === 0) {
        setTokens(tokens);
    }
});

connection.on("SetCampaign", (players) => {
    const settings = SettingsManager.getSettings();
    if (settings == null) return;

    initializeServerConnection();

    Object.keys(players).forEach(player => {
        if (settings.Name.localeCompare(player, undefined, { sensitivity: 'accent' }) === 0) {
            setTokens(players[player]);
        }
    });
});

$(document).ready(() => {
    $(".btn-direction").click(function() {
        const character = $("#tokens").val();
        const direction = $(this).attr('direction');

        $('.collapsible-conditions-content').hide();

        if (direction && character) {
            $.ajax({ url: "Navigation/Move", type: "POST", data: { 'character': character, 'direction': direction } });
        }
    });

    $(".btn-condition").click(function () {
        const character = $("#tokens").val();
        const condition = $(this).attr('condition');

        if (character) {
            $.ajax({ url: "Navigation/ToggleCondition", type: "POST", data: { 'character': character, 'condition': condition} });
        }
    });

    $("#tokens").change(function () {
        const character = $(this).val();
        conditionButtons.forEach(button => {
            document.getElementById(button).style.backgroundColor = '';
        });

        $.ajax({ url: "Navigation/GetConditions", type: "POST", data: { 'character': character } });
    });

    $(".btn-collapsible-conditions").click(() => {
        $('.collapsible-conditions-content').toggle();
    });

    $(".btn-orientation").click(() => {
        $('.collapsible-conditions-content').hide();
        document.getElementById("btnOrientation").className = "fa fa-2x " + updateOrientation();
        $.ajax({ url: "Navigation/ChangeOrientation", type: "POST" });
    });

    $(".btn-apply-height").click(() => {
        const character = $("#tokens").val();
        const height = $("#height").val();
        $("#height").val("");

        $.ajax({ url: "Navigation/SetHeight", type: "POST", data: { 'character': character, 'height': height } });
    });

    PanelManager.initializeEventListeners();
});

async function start() {
    try {
        PanelManager.updateConditionInfoButtonVisibility();
        await connection.start();

        const settings = SettingsManager.getSettings();
        if (settings != null) {
            $.ajax({
                url: "Navigation/GetTokens",
                type: "GET",
                data: { 'player': settings.Name },
                success: (tokens) => {
                    if (tokens != "") {
                        setTokens(JSON.parse(tokens));
                    }
                }
            });
        } else {
            $("#navigationMain").empty();
            $("#navigationMain").append("<p><b>No settings found!</b></p><p>Please go to settings and enter your name.</p>");
        }
    } catch (error) {
        console.error(error);
    }
}

start();


