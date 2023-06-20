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

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/WebHub")
    .withAutomaticReconnect()
    .build();

connection.on("SetConditions", function (character, conditions) {
    let selectedCharacter = $("#character").val();
    let characterWithId = character + "_1";

    if (character.localeCompare(selectedCharacter, undefined, { sensitivity: 'accent' }) === 0 || characterWithId.localeCompare(selectedCharacter, undefined, { sensitivity: 'accent' }) === 0) {
        for (const button of conditionButtons) {
            document.getElementById(button).style.backgroundColor = '';
        }

        for (const condition of conditions) {
            document.getElementById(conditionButtons[condition]).style.backgroundColor = '#303538';
        }
    }
});

async function start() {
    try {
        await connection.start();
    } catch (error) {
        console.log(error);
    }
};

start();

$(document).ready(function () {
    let currentOrientation = $(".btn-orientation").attr('orientation');

    function UpdateOrientation() {
        if (currentOrientation == "Up") {
            currentOrientation = "Left";
            return "fa-arrow-circle-left";
        }
        else if (currentOrientation == "Left") {
            currentOrientation = "Down";
            return "fa-arrow-circle-down";
        }
        else if (currentOrientation == "Down") {
            currentOrientation = "Right";
            return "fa-arrow-circle-right";
        }
        else {
            currentOrientation = "Up";
            return "fa-arrow-circle-up";
        }
    }

    $(".btn-direction").click(function() {
        let character = $("#character").val();
        let direction = $(this).attr('direction');

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
        let character = $("#character").val();
        let condition = $(this).attr('condition');

        if (!character)
            return;

        $.ajax({
            url: "Navigation/ToggleCondition",
            type: "POST",
            data: { 'character': character, 'condition': condition}
        })
    })

    $(window).on("load", function () {
        let character = $("#character").val();

        if (!character)
            return;

        $.ajax({
            url: "Navigation/GetConditions",
            type: "POST",
            data: { 'character': character }
        })
    });

    $("#character").change(function () {
        let character = $("#character").val();

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

        document.getElementById("btnOrientation").className = "fa fa-2x " + UpdateOrientation();

        $.ajax({
            url: "Navigation/ChangeOrientation",
            type: "POST"
        })
    })
});
