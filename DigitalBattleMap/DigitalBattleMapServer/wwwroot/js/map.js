"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/MapHub").build();

connection.start().then(function () {
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("UpdateMap", function (drawLayer, image) {
    document.getElementById(drawLayer).src = image;
});
