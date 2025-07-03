using System.CommandLine;
using System.Text.Json;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models.ExternalConnectors;
using Microsoft.Kiota.Authentication.Azure;

// Constants for the Contoso HR Connector
const string CONNECTOR_ID = "contosohrconnector";
const string CONNECTOR_NAME = "Contoso HR Connector";

// The people data to be synced
// This is a sample data structure that would typically be fetched from an external HR system
var people = new[]
{
    new { UPN = "alexw@contoso.com",
        Department = "Engineering",
        Position = "Software Engineer",
        FavoriteColor = "Blue" },
    new { UPN = "luisg@contoso.com",
        Department = "Marketing",
        Position = "Marketing Manager",
        FavoriteColor = "Green" }
};

// Initialize the Graph client with user secrets
var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var credential = new ClientSecretCredential(
    config["settings:tenantId"], config["settings:clientId"], config["settings:clientSecret"]);
var httpClient = GraphClientFactory.Create();
var authProvider = new AzureIdentityAuthenticationProvider(
    credential, scopes: ["https://graph.microsoft.com/.default"]);
var graphClient = new GraphServiceClient(authProvider, "https://graph.microsoft.com/beta");

// Create the CLI commands
var rootCommand = new RootCommand("Contoso HR Connector");
var setupCommand = new Command("setup", "Setup the Contoso HR Connector");
var registerCommand = new Command("register", "Register the Contoso HR Connector");
var syncCommand = new Command("sync", "Sync the Contoso HR Connector");

rootCommand.Add(setupCommand);
rootCommand.Add(registerCommand);
rootCommand.Add(syncCommand);

// Register command to setup the Contoso HR Connector
// This command will create the connector and configure the schema
setupCommand.SetAction(async parsedResult =>
{
    Console.WriteLine("Setting up the Contoso HR Connector...");

    var newConnectionParameters = new ExternalConnection
    {
        Id = CONNECTOR_ID,
        Name = CONNECTOR_NAME,
    };

    var newConnection = await graphClient.External.Connections.PostAsync(newConnectionParameters);

    if (newConnection == null)
    {
        Console.WriteLine("Failed to create the Contoso HR Connector.");
        return;
    }

    Console.WriteLine("Configuring the schema, this may take a few minutes...");
    var requestInfo = graphClient.External.Connections[newConnection.Id].Schema.ToPatchRequestInformation(new Microsoft.Graph.Beta.Models.ExternalConnectors.Schema
    {
        BaseType = "microsoft.graph.externalItem",
        Properties =
           [
                new() {
                     Name = "accounts",
                     Type = Microsoft.Graph.Beta.Models.ExternalConnectors.PropertyType.String
                },
                new() {
                     Name = "positions",
                     Type = Microsoft.Graph.Beta.Models.ExternalConnectors.PropertyType.String
                },
                new() {
                     Name = "favoriteColor",
                     Type = Microsoft.Graph.Beta.Models.ExternalConnectors.PropertyType.String
                }
           ]
    });

    // We will poll for the operation to complete, so the request
    // is customized to include a header that says we want an async response
    var requestMessage = await graphClient.RequestAdapter.ConvertToNativeRequestAsync<HttpRequestMessage>(requestInfo);
    _ = requestMessage ?? throw new Exception("Could not create native HTTP request");
    requestMessage.Method = HttpMethod.Patch;
    requestMessage.Headers.Add("Prefer", "respond-async");

    var responseMessage = await httpClient.SendAsync(requestMessage) ??
           throw new Exception("No response returned from API");

    // Wait for the operation to complete
    if (responseMessage.IsSuccessStatusCode)
    {
        var operationId = responseMessage.Headers.Location?.Segments.Last() ??
            throw new Exception("Could not get operation ID from Location header");
        do
        {
            var operation = await graphClient.External
                .Connections[newConnection.Id]
                .Operations[operationId]
                .GetAsync() ?? throw new ServiceException("Operation not found");

            if (operation?.Status == ConnectionOperationStatus.Completed)
            {
                return;
            }
            else if (operation?.Status == ConnectionOperationStatus.Failed)
            {
                throw new ServiceException($"Schema operation failed: {operation?.Error?.Code} {operation?.Error?.Message}");
            }
            // Wait 1 minute and then try again
            await Task.Delay(60000);
        } while (true);
    }
    else
    {
        throw new ServiceException("Registering schema failed",
            responseMessage.Headers, (int)responseMessage.StatusCode);
    }
});

// Register command to register the Contoso HR Connector
// This command will register the connector with the Microsoft Graph
// It will create a profile source and update the profile property settings
registerCommand.SetAction(async parsedResult =>
{
    Console.WriteLine("Registering the Contoso HR Connector...");
    _ = await graphClient.Admin.People.ProfileSources
        .PostAsync(new Microsoft.Graph.Beta.Models.ProfileSource
        {
            SourceId = CONNECTOR_ID,
            DisplayName = CONNECTOR_NAME,
            WebUrl = "https://hr.contoso.com"
        }) ?? throw new ServiceException("Failed to register the Contoso HR Connector");

    var propertySettings = await graphClient.Admin.People.ProfilePropertySettings.GetAsync() ?? throw new ServiceException("No response returned from API");

    var globalSettings = propertySettings.Value?.SingleOrDefault(x => x.Name is null) ?? throw new ServiceException("No response returned from API");

    var sources = globalSettings.PrioritizedSourceUrls;
    var sourceId = globalSettings.Id;

    if (sources == null || sources.Count == 0)
    {
        sources = new List<string>([$"https://graph.microsoft.com/beta/admin/people/profileSources(sourceId='{CONNECTOR_ID}')"]);
    }
    else
    {
        sources.Insert(0, $"https://graph.microsoft.com/beta/admin/people/profileSources(sourceId='{CONNECTOR_ID}')");
    }

    var newPropertySetting = new Microsoft.Graph.Beta.Models.ProfilePropertySetting
    {
        PrioritizedSourceUrls = sources
    };

    var responseMessage = await graphClient.Admin.People.ProfilePropertySettings[sourceId]
        .PatchAsync(newPropertySetting) ?? throw new ServiceException("No response returned from API");
});

// Sync command to sync the Contoso HR Connector
// This command will take the people data and sync it to the Microsoft Graph
// It will create or update external items in the Contoso HR Connector
syncCommand.SetAction(async parsedResult =>
{
    Console.WriteLine("Syncing the Contoso HR Connector...");

    foreach (var person in people)
    {
        Console.WriteLine($"Syncing {person.UPN}...");
        var personIdentifier = person.UPN.Replace("@", "_at_").Replace(".", "_dot_");
        Console.WriteLine($"Person identifier: {personIdentifier}");
        var graphUser = await graphClient.Users.GetAsync(requestConfiguration =>
            requestConfiguration.QueryParameters.Filter = $"userPrincipalName eq '{person.UPN}'") ?? throw new ServiceException($"User with UPN {person.UPN} not found in Microsoft Graph.");

        var user = graphUser.Value?.FirstOrDefault();
        var oid = user?.Id ?? throw new ServiceException($"User with UPN {person.UPN} does not have an OID.");

        var newItem = new ExternalItem
        {
            Id = personIdentifier,
            Acl =
            [
                new Acl
                {
                    AccessType = AccessType.Grant,
                    Type = AclType.Everyone,
                    Value = "EVERYONE"
                }
            ],
            Properties = new Properties
            {
                AdditionalData = new Dictionary<string, object>
                {
                    { "accounts", JsonSerializer.Serialize(new[]
                        {
                            new {
                                userPrincipalName = person.UPN,
                                externalDirectoryObjectId = oid,
                            }
                        }
                    )},
                    { "positions", JsonSerializer.Serialize(new[]
                        {
                            new {
                                detail = new
                                {
                                    jobTitle = person.Position,
                                    company = new
                                    {
                                        department = person.Department
                                    },
                                },
                                isCurrent = true
                            }
                        }
                    )},
                    { "favoriteColor", person.FavoriteColor }
                }
            }
        };

        await graphClient.External
              .Connections[CONNECTOR_ID]
              .Items[personIdentifier]
              .PutAsync(newItem);
    }
});

// Start the command line
ParseResult parseResult = rootCommand.Parse(args);
return parseResult.Invoke();

