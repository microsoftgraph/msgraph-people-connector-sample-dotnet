# Microsoft 365 Copilot connector for people data sample

![License.](https://img.shields.io/badge/license-MIT-green.svg)

Microsoft 365 Copilot connectors for people data allows you to enrich or extend the views of people in your tenant with your own data and have it power various Microsoft 365 experiences, including the profile card, people search and Microsoft 365 Copilot chat. This .NET applications shows you how to build a [Microsoft 365 Copilot connector for people data](https://aka.ms/peopleconnectors).

**NOTE**: This capability is currently in public preview.

## Configure and run the sample

Follow the tutorial and explanation of the source code at [Build your first connector for people data](https://learn.microsoft.com/microsoft-365-copilot/extensibility/build-your-first-people-connector).

### Create an Entra ID app registration

Follow the instructions in the tutorial above to create the required Entra ID app registration, and to configure the client secrets.

### Modify the code

Modify the constants in `Program.cs` to reflect the id and name of your connection.

Update the `people` object to reflect the users you would like to enrich using the connection, or modify the code to import this data from an external source (such as a file or a remote system).

### Commands

This sample uses a command line interface (CLI).

- Use `dotnet run create` to create the Copilot connector using Microsoft Graph.
- Use `dotnet run register` to register the connection as a source of people data.
- Use `dotnet run sync` to ingest the people data into Microsoft Graph.

## Code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.