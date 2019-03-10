import { connect } from "mqtt";

var client = connect('ws://' + location.host + '/mqtt',
    {
        clientId: "client" + Math.floor(Math.random() * 6) + 1
    });

window.onbeforeunload = () => {
    client.end();
};

var publishButton = document.getElementById("publish");
var topicInput = <HTMLInputElement>document.getElementById("topic");
var msgInput = <HTMLInputElement>document.getElementById("msg");
var stateParagraph = document.getElementById("state");
var msgsList = <HTMLUListElement>document.getElementById("msgs");

publishButton.onclick = click => {
    var topic = topicInput.value;
    var msg = msgInput.value;
    client.publish(topic, msg);
};

client.on('connect', () => {
    client.subscribe('#', { qos: 0 }, (err, granted) => {
        console.log(err);
    });
    client.publish('presence', 'Hello mqtt');

    stateParagraph.innerText = "connected";
    showMsg("[connect]");
});

client.on("error", e => {
    showMsg("error: " + e.message);
});

client.on("reconnect", () => {
    stateParagraph.innerText = "reconnecting";
    showMsg("[reconnect]");
});

client.on('message', (topic, message) => {
    showMsg(topic + ": " + message.toString());
});

function showMsg(msg: string) {
    //console.log(msg);

    var node = document.createElement("LI");
    node.appendChild(document.createTextNode(msg));

    msgsList.appendChild(node);

    if (msgsList.childElementCount > 50) {
        msgsList.removeChild(msgsList.childNodes[0]);
    }
}