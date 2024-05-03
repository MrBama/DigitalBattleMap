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

connectionMap.on("UpdateMap", function (drawLayer) {
    console.log(drawLayer);

    switch (drawLayer) {
        case 0:
            $('#tokenImage').attr('src', tokensUrl + '&t=' + new Date().getTime());
            $('#gridAndStrokesImage').attr('src', gridAndStrokesUrl + '&t=' + new Date().getTime());
            $('#backgroundImage').attr('src', backgroundUrl + '&t=' + new Date().getTime());
            break;
        case 1:
            $('#backgroundImage').attr('src', backgroundUrl + '&t=' + new Date().getTime());
            break;
        case 2:
            $('#gridAndStrokesImage').attr('src', gridAndStrokesUrl + '&t=' + new Date().getTime());
            break;
        case 3:
            $('#tokenImage').attr('src', tokensUrl + '&t=' + new Date().getTime());
            break;
    }

});

async function start() {
    try {
        await connectionMap.start();
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
})