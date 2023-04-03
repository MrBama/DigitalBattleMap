 "use strict";

const backgroundUrl = "/Map/Get?layer=Background";
const gridAndStrokesUrl = "/Map/Get?layer=GridAndStrokes";
const tokensUrl = "/Map/Get?layer=Tokens";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/MapHub")
    .withAutomaticReconnect()
    .build();

connection.on("UpdateMap", function (drawLayer) {
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
        await connection.start();
    } catch (error) {
        console.log(error);
    }
};

start();