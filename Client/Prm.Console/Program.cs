using Microsoft.Extensions.Configuration;
using Prm.Client.Api;
using Prm.Client.Auth;
using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Screens;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables(prefix: "PRM_")
    .Build();

var apiBaseUrl = Environment.GetEnvironmentVariable("PRM_API_URL")
    ?? configuration["ApiBaseUrl"]
    ?? "http://localhost:5140";

var session = new SessionState();
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };
var api = new PrmApiClient(http, session);
var navigator = new MenuNavigator(session, api);
var app = new ConsoleApp(session, api, navigator);

System.Console.WriteLine("PRM Console Client");
System.Console.WriteLine($"API: {apiBaseUrl}");
System.Console.WriteLine();

await navigator.RunAsync(new WelcomeScreen(app), CancellationToken.None);

System.Console.WriteLine("Goodbye.");
