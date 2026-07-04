"use strict";

const backgroundUrl = "/Map/Get?layer=Background";
const gridAndStrokesUrl = "/Map/Get?layer=GridAndStrokes";
const tokensUrl = "/Map/Get?layer=Tokens";

let controlsPositionLeft = true;

const connectionMap = new signalR.HubConnectionBuilder()
    .withUrl("/MapHub")
    .withAutomaticReconnect()
    .build();

function openFullscreen() {
    var elem = document.getElementById("mapContainer");
    if (elem.requestFullscreen) {
        elem.requestFullscreen();
    }
    else if (elem.webkitRequestFullscreen) /* Safari */ {
        elem.webkitRequestFullscreen();
    }
    else if (elem.msRequestFullscreen) /* IE11 */ {
        elem.msRequestFullscreen();
    }
}

function closeFullscreen() {
    if (document.exitFullscreen) {
        document.exitFullscreen();
    }
    else if (document.webkitExitFullscreen) /* Safari */ {
        document.webkitExitFullscreen();
    }
    else if (document.msExitFullscreen) /* IE11 */ {
        document.msExitFullscreen();
    }
}

function updatePauseStatus(isPaused) {
    if (isPaused) {
        $('#pauseImage').show()
    }
    else {
        $('#pauseImage').hide()
    }
}

connectionMap.on("UpdateMap", function (drawLayer) {
    var time = new Date().getTime();
    switch (drawLayer) {
        case 0:
            $('#tokenImage').attr('src', tokensUrl + '&t=' + time);
            $('#gridAndStrokesImage').attr('src', gridAndStrokesUrl + '&t=' + time);
            $('#backgroundImage').attr('src', backgroundUrl + '&t=' + time);
            break;
        case 1:
            $('#backgroundImage').attr('src', backgroundUrl + '&t=' + time);
            break;
        case 2:
            $('#gridAndStrokesImage').attr('src', gridAndStrokesUrl + '&t=' + time);
            break;
        case 3:
            $('#tokenImage').attr('src', tokensUrl + '&t=' + time);
            break;
    }
});

connectionMap.on("UpdatePauseStatus", function (isPaused) {
    updatePauseStatus(isPaused);
});

async function start() {
    try {
        await connectionMap.start();

        $.ajax({
            url: "Map/GetPauseStatus",
            type: "GET",
            success: function (isPaused) {
                let paused = isPaused.toLowerCase() === "true";
                updatePauseStatus(paused);
            }
        })

    } catch (error) {
        console.log(error);
    }
};

$(document).on('fullscreenchange', function () {
    if (document.fullscreenElement == null) {
        $('#btnContainer').appendTo('#mapView');
        $('#controlsOverlay').appendTo('#mapView');
    }
});

start();

$(document).ready(function () {
    $.ajax({
        url: "Map/GetCharacterNavigationViewComponent",
        type: "GET",
        success: function (result) {
            $("#controlsBody").html(result);
        },
    })

    $("#btnFullscreen").click(function () {
        if (document.fullscreenElement == null) {
            openFullscreen();
            $('#btnContainer').appendTo('#mapContainer');
            $('#controlsOverlay').appendTo('#mapContainer');
        }
        else {
            closeFullscreen();
        }        
    });

    $("#btnControls").click(function () {
        $('#controlsOverlay').toggle();
    });

    $("#btnMoveControls").click(function () {
        if (controlsPositionLeft) {
            $('.controls-overlay').css('left', '0');
            $('.controls-overlay').css('right', '');
            $('.controls-position-buttons').css('justify-content', 'start');
            document.getElementById("btnMoveControls").className = "btn btn-secondary fa fa-caret-right";
        }
        else {
            $('.controls-overlay').css('left', '');
            $('.controls-overlay').css('right', '0');
            $('.controls-position-buttons').css('justify-content', 'end');
            document.getElementById("btnMoveControls").className = "btn btn-secondary fa fa-caret-left";
        }

        controlsPositionLeft = !controlsPositionLeft;
    });

    $(document).on("keypress", function (e) {
        if (document.fullscreenElement != null && e.which == 104) {
            $('#btnControls').toggle();
            $('#btnFullscreen').toggle();
        }
    });
})