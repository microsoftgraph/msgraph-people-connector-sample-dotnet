# Microsoft 365 Copilot connector for people data sample

![License.](https://img.shields.io/badge/license-MIT-green.svg)

Microsoft 365 Copilot connectors for people data allows you to enrich or extend the views of people in your tenant with your own data and have it power various Microsoft 365 experiences, including the profile card, people search and Microsoft 365 Copilot chat. This .NET applications shows you how to build a [Microsoft 365 Copilot connector for people data](https://aka.ms/peopleconnectors).

**NOTE**: This capability is currently in public preview.

For additional details please see the [Build connectors with people data](https://learn.microsoft.com/microsoft-365-copilot/extensibility/build-connectors-with-people-data.md) article.

## Configure and run the sample

Follow these instructions to configure, build and run the example.

### Create an Entra ID app registration

This connector requires an Entra ID application to registered. Follow these steps to create a new Entra ID app registration for your people connector.

1. Log in to the [Azure Entra ID portal](https://aad.portal.azure.com) using a global administrator role.
1. Select **Applications > App registrations** and click on **+ New registration**.
1. Type the name of your application in the **Name** text box. Ex *ContosoHrConnector*.
1. Click **Register** to complete the registration.
1. Select **API Permissions** and choose **+ Add a permission** to add permissions to the app.
1. Choose **Microsoft Graph** and then **Application permissions** and select the following permission scopes:
    1. **ExternalConnection.ReadWrite.OwnedBy** (required to create the connection and schema)
    1. **ExternalItem.ReadWrite.OwnedBy** (required to ingest people data)
    1. **PeopleSettings.ReadWrite.All** (required to add the connection as a profile source)
    1. **Users.ReadBasic.All** (preview requirement for this sample)
1. Click **Add Permissions** and then click **Grant admin consent for Contoso** (replace Contoso with your organization’s name) to grant these permissions to the application. Select **Yes** to complete the grant.
1. Select **Certificates & secrets** and create a new secret with **+ New client secret**. Give it an appropriate description and expiry length and click **Add**.
1. Note the **Secret** value and store it in a safe location.
1. Click **Overview** and record the *Application (client) ID* and *Directory (tenant) ID*.

> [!TIP]
> For production scenarios, consider modify the code and create two different applications—one to create the connection, schema, and perform the profile source registration, and another for the actual ingestion. Use Managed Identities for credentials instead of storing client secrets.

### Set up authorization to Microsoft Graph

To connect the application to Microsoft Graph using the Entra ID app registration, run the following commands in the terminal window, in the directory of the console application where you cloned or copied the sample code. Replace the client ID, tenant ID, and client secret with the values you stored in a safe location.

``` bash
dotnet user-secrets init
dotnet user-secrets set settings:clientId <client-id>
dotnet user-secrets set settings:tenantId <tenant-id>
dotnet user-secrets set settings:clientSecret <client-secret>
```

### Modify the code

Next is to modify the code to match your tenants details. All places referred to below are marked with `TODO:` in the sample code.

1. Modify the constants in `Program.cs` to reflect the id and name of your connection.
1. Update the `people` object to reflect the users you would like to enrich using the connection, or modify the code to import this data from an external source (such as a file or a remote system).
1. If you decide to add additional properties in the `people` object, you must also update the schema (in the `setupCommand`) and the people sync logic (in the `syncCommand`).

### Commands

This sample uses a command line interface (CLI).

- Use `dotnet run setup` to create the Copilot connector using Microsoft Graph.
- Use `dotnet run register` to register the connection as a source of people data.
- Use `dotnet run sync` to ingest the people data into Microsoft Graph.

## Code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
