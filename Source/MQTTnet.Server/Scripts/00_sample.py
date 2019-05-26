import json


def initialize():
    """
    This function is invoked after the script file has been loaded.
    It will be executed only one time.
    """

    print("Hello World from Sample script.")
   

def on_validate_client_connection(context):
    """
    This function is invoked whenever a client wants to connect. It can be used to validate the connection.
    """
    
    print(context)

    mqtt_net_server.write_shared_data(context["client_id"], {"custom_value_1": 1, "custom_value_2": True})

    return

    if context["client_id"] != "test_client":
        context["result"] = "connection_refused_not_authorized"
        return

    if context["username"] != "bud spencer":
        context["result"] = "connection_refused_not_authorized"
        return

    if context["password_string"] != "secret":
        context["result"] = "connection_refused_not_authorized"

    print(context)


def on_intercept_subscription(context):
    """
    This function is invoked whenever a client wants to subscribe to a topic.
    """
    
    print("Client '{client_id}' want's to subscribe to topic '{topic}'.".format(client_id=context["client_id"], topic=context["topic"]))


def on_intercept_application_message(context):
    """
    This function is invoked for every processed application message. It also allows modifying
    the message or cancel processing at all.
    """

    client_id = context["client_id"]

    if client_id != None:
        shared_data = mqtt_net_server.read_shared_data(context["client_id"], {})
        print(shared_data)

    if context["topic"] == "topic_with_response":

        json_payload = {
            "hello": "world",
            "x": 1,
            "y": True,
            "z": None
        }

        application_message = {
            "retain": False,
            "topic": "reply",
            "payload": json.dumps(json_payload)
            }

        mqtt_net_server.publish(application_message)

    print("Client '{client_id}' published topic '{topic}'.".format(client_id=context["client_id"], topic=context["topic"]))


def on_client_connected(event_args):
    """
    This function is called whenever a client has passed the validation is connected.
    """

    print("Client '{client_id}' is now connected.".format(client_id=event_args["client_id"]))


def on_client_disconnected(event_args):
    """
    This function is called whenever a client has disconnected.
    """

    print("Client '{client_id}' is now disconnected (type = {type}).".format(client_id=event_args["client_id"], type=event_args["type"]))


def on_client_subscribed_topic(event_args):
    """
    This function is called whenever a client has subscribed to a topic (when allowed).
    """

    print("Client '{client_id}' has subscribed to '{topic}'.".format(client_id=event_args["client_id"], topic=event_args["topic"]))


def on_client_unsubscribed_topic(event_args):
    """
    This function is called whenever a client has unsubscribed from a topic.
    """

    print("Client '{client_id}' has unsubscribed from '{topic}'.".format(client_id=event_args["client_id"], topic=event_args["topic"]))