import { connect } from "mqtt";

var client = connect('ws://' + location.host + '/mqtt',
    {
        clientId: "client1",
    });

client.on('connect', () => {
    client.subscribe('presence');
    client.publish('presence', 'Hello mqtt');
});

client.on('message', (topic, message) => {
    // message is Buffer
    console.log(message.toString());
});

window.onbeforeunload = () => {
    client.end();
};