def handle_authenticate(context):
    """
    This function is invoked whenever a user tries to access protected HTTP resources.
    This function must exist and return a proper value. Otherwise the request is denied.
    """

    username = context["username"]
    password = context["password"]

    context["is_authenticated"] = True # Change this to _False_ in case of invalid credentials.