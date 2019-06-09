def handle_authenticate(context):
    """
    This function is invoked whenever a user tries to access protected HTTP resources.
    This function must exist and return a proper value. Otherwise the request is denied.
    All authentication data must be sent via using the "Authorization" header. This header
    will be parsed and all values will be exposed to the context. 
    """
    header_value = context["header_value"] # The untouched header value.

    scheme = context["scheme"]
    parameter = context["parameter"]

    username = context["username"] # Only set if proper "Basic" authorization is used.
    password = context["password"] # Only set if proper "Basic" authorization is used.
    
    context["is_authenticated"] = True # Change this to _False_ in case of invalid credentials.

    # Example for an API key with proper format.
    if header_value == "APIKey 123456":
        context["is_authenticated"] = True