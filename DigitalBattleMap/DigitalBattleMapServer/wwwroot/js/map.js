"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/MapHub").build();

connection.start().then(function () {
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("UpdateMap", function (image) {
    document.getElementById("mapImage").src = image;
});