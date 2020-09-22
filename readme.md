# Azure Front Door Filter

Provides middleware to filter access to your web application based on the request originating through a specified set of Azure Front Door resources.

When using Azure Front Door all requests made to your backend will contain a set of headers, one of which can be used to identify the instance of Azure Front Door the resource originated from.  See https://docs.microsoft.com/en-us/azure/frontdoor/front-door-faq#how-do-i-lock-down-the-access-to-my-backend-to-only-azure-front-door for specific documentation.

By configuring this middleware you can filter access to your web application to only the required Azure Front Door instances.

## Usage

1. Search for and install the package `Storm.AzureFrontDoorFilter` via NuGet.
2. Reference the namespace `using Storm.AzureFrontDoorFilter`
3. Add the middleware into the application builder:
   ```c#
    app.UseAzureFrontDoorFilter();
   ```
    > It is recommended that the middleware is placed before all other middleware so that it can run before other middleware, however you are free to place it where ever suits your needs

The default behaviour for the middlewware filter is to allow all requests, with further configuration handled within via configuration.

## Configuration

In order to filter requests to 1 or more Azure Front Door instances you will need to add the Id of the instance to the configuration.

Set the value of the "AllowAfdIds" configuration value to a string value that contains either:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "AllowedAfdIds": "*"
}

```

- A single Azure Front Door Id
  - `CBEBA294-B618-4559-8E37-DE8235C095DD`
- Multiple Azure Front Door Id's separated by semi-colons (;)
  - `CBEBA294-B618-4559-8E37-DE8235C095DD;E1DC2BB2-0277-4BA2-B550-9A7462D7CE62`
- A empty string to block all instances
- A wildcard string "*" to indicate that no specific filtering should occur (the default)
  - `*`

## What is the Id of my Azure Front Door?

Each Azure Front Door is assigned an Id that can be retrieved either directly within the resource blade of the Azure Portal or via an API call.  Currently this middleware only supports Id's that are directly referenced within the configuration system.