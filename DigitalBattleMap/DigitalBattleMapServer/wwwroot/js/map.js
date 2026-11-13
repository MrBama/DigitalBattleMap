"use strict";

const backgroundUrl = "/Map/Get?layer=Background";
const gridAndStrokesUrl = "/Map/Get?layer=GridAndStrokes";
const tokensUrl = "/Map/Get?layer=Tokens";
const defaultMapSizeX = 1920;
const defaultMapSizeY = 1080;

let controlsPositionLeft = true;
let mousePressTimer = null;

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

function map(input, inMin, inMax, outMin, outMax) {
    return (input - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
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

connectionMap.on("EnablePing", function (x, y) {
    var rectBackground = document.getElementById("backgroundImage").getBoundingClientRect();
    if (rectBackground.top == rectBackground.bottom) {
        console.log('There is no map yet');
        return;
    }

    // Show animation first otherwise getBoundingClientRect() returns 0,0,0,0
    $('#pingAnimation').show();
    var rectAnimation = document.getElementById("pingAnimation").getBoundingClientRect();
    
    var mappedX = map(x, 0, defaultMapSizeX, 0, rectBackground.right - rectBackground.left);
    var mappedY = map(y, 0, defaultMapSizeY, 0, rectBackground.bottom - rectBackground.top);
    var animationSizeX = rectAnimation.right - rectAnimation.left;
    var animationSizeY = rectAnimation.bottom - rectAnimation.top;

    document.getElementById("pingAnimation").style.left = mappedX - (animationSizeX / 2) + "px";  
    document.getElementById("pingAnimation").style.top = mappedY - (animationSizeY / 2) + "px";  
});

connectionMap.on("DisablePing", function () {
    $('#pingAnimation').hide();
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
            
            // After controls are loaded, check their initial position and sync the condition panel
            syncConditionPanelPosition();
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
        
        // Sync condition panel position when controls are shown
        if ($('#controlsOverlay').is(':visible')) {
            syncConditionPanelPosition();
        }
    });

    $("#btnMoveControls").click(function () {
        if (controlsPositionLeft) {
            // Moving controls to LEFT
            $('.controls-overlay').css('left', '0');
            $('.controls-overlay').css('right', '');
            $('.controls-position-buttons').css('justify-content', 'start');
            document.getElementById("btnMoveControls").className = "btn btn-secondary fa fa-caret-right";
            
            // Move condition panel to RIGHT (default position)
            $('#conditionInfoPanel').removeClass('shift-left');
        }
        else {
            // Moving controls to RIGHT
            $('.controls-overlay').css('left', '');
            $('.controls-overlay').css('right', '0');
            $('.controls-position-buttons').css('justify-content', 'end');
            document.getElementById("btnMoveControls").className = "btn btn-secondary fa fa-caret-left";
            
            // Move condition panel to LEFT to avoid overlap
            $('#conditionInfoPanel').addClass('shift-left');
        }

        controlsPositionLeft = !controlsPositionLeft;
    });

    $(document).on("keypress", function (e) {
        if (document.fullscreenElement != null && e.which == 104) {
            $('#btnControls').toggle();
            $('#btnFullscreen').toggle();
        }
    });


    $('#mapContainer').on('mousedown', function (e) {
        e.preventDefault();

        var rectBackground = document.getElementById("backgroundImage").getBoundingClientRect();
        var rectControls = document.getElementById("controlsBody").getBoundingClientRect();

        if (rectBackground.top == rectBackground.bottom) {
            console.log('There is no map yet');
            return;
        }

        // Don't ping when the click was on top of controls overlay
        if (e.pageX >= rectControls.left && e.pageX <= rectControls.right && e.pageY >= rectControls.top && e.pageY <= rectControls.bottom) {
            return;
        }

        // Ping when the click is on top of the map
        if (e.pageX >= rectBackground.left && e.pageX <= rectBackground.right && e.pageY >= rectBackground.top && e.pageY <= rectBackground.bottom) {
            var relativePosX = e.pageX - rectBackground.left;
            var relativePosY = e.pageY - rectBackground.top;
            var mappedPosX = map(relativePosX, 0, rectBackground.right - rectBackground.left, 0, defaultMapSizeX);
            var mappedPosY = map(relativePosY, 0, rectBackground.bottom - rectBackground.top, 0, defaultMapSizeY);

            mousePressTimer = setTimeout(() => {
                $.ajax({
                    url: "Map/EnablePingFromClient",
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({
                        x: Number(mappedPosX),
                        y: Number(mappedPosY)
                    }),
                });
            }, 1000);
        }
    });

    $('#mapContainer').on('mouseup', function (e) {
        clearTimeout(mousePressTimer);
    });
})

function syncConditionPanelPosition() {
    // Check if controls overlay is visible and positioned on the right
    var controlsOverlay = $('.controls-overlay');
    var isVisible = controlsOverlay.is(':visible');
    var isPositionedRight = controlsOverlay.css('right') !== 'auto' && controlsOverlay.css('right') !== '';
    
    if (isVisible && isPositionedRight) {
        // Controls are on the right, move condition panel to left
        $('#conditionInfoPanel').addClass('shift-left');
    } else {
        // Controls are on the left or hidden, condition panel should be on the right
        $('#conditionInfoPanel').removeClass('shift-left');
    }
}