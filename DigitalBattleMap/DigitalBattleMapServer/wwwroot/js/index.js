"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/WebHub").build();

//Disable the buttons until connection is established.
document.getElementById("upLeftButton").disabled = true;
document.getElementById("upButton").disabled = true;
document.getElementById("upRightButton").disabled = true;
document.getElementById("leftButton").disabled = true;
document.getElementById("rightButton").disabled = true;
document.getElementById("downLeftButton").disabled = true;
document.getElementById("downButton").disabled = true;
document.getElementById("downRightButton").disabled = true;

connection.start().then(function () {
    document.getElementById("upLeftButton").disabled = false;
    document.getElementById("upButton").disabled = false;
    document.getElementById("upRightButton").disabled = false;
    document.getElementById("leftButton").disabled = false;
    document.getElementById("rightButton").disabled = false;
    document.getElementById("downLeftButton").disabled = false;
    document.getElementById("downButton").disabled = false;
    document.getElementById("downRightButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("upLeftButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("MoveTokenButtonPressed", user, "UpLeft").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("upButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("MoveTokenButtonPressed", user, "Up").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("upRightButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("MoveTokenButtonPressed", user, "UpRight").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("leftButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("MoveTokenButtonPressed", user, "Left").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("rightButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("MoveTokenButtonPressed", user, "Right").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("downLeftButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("MoveTokenButtonPressed", user, "DownLeft").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("downButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("MoveTokenButtonPressed", user, "Down").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("downRightButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("MoveTokenButtonPressed", user, "DownRight").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});