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

connection.on("SetConditions", function (character, conditions) {
    let selectedCharacter = $("#tokens").val();

    if (character.localeCompare(selectedCharacter, undefined, { sensitivity: 'accent' }) === 0) {
        for (const button of conditionButtons) {
            document.getElementById(button).style.backgroundColor = '';
        }

        for (const condition of conditions) {
            if (condition != heightCondition) {
                document.getElementById(conditionButtons[condition]).style.backgroundColor = '#303538';
            }
        }
    }
});

connection.on("SetTokens", function (player, tokens) {
    let settings = getSettings();

    if (settings != null && settings.Name.localeCompare(player, undefined, { sensitivity: 'accent' }) === 0) {
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
});

connection.on("SetCampaign", function (players) {
    let settings = getSettings();
    if (settings == null) {
        return;
    }

    initializeServerConnection();

    let select = document.getElementById("tokens");
    while (select.options.length > 0) {
        select.remove(0);
    }

    for (const player in players) {
        if (settings.Name.localeCompare(player, undefined, { sensitivity: 'accent' }) === 0) {        
            for (const token of players[player]) {
                let option = document.createElement("option");
                option.text = token;
                select.add(option);
            }
            getConditions();
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
});

async function start() {
    try {
        await connection.start();

        // Request tokens with refresh
        let settings = getSettings();
        if (settings != null) {
            $.ajax({
                url: "Navigation/GetTokens",
                type: "POST",
                data: { 'player': settings.Name }
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

start();

