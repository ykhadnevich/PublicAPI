/*using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Globalization;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHsts();
        }

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Hello World!");
            });

            endpoints.MapGet("/api/stats/users", async context =>

            {
                // Get the date parameter from the query string
                string date = context.Request.Query["date"];

                try
                {
                    // Read and parse the Data.txt file
                    var lines = System.IO.File.ReadAllLines("Data.txt");

                    // Find the line that matches the specified date and time
                    string line = lines.FirstOrDefault(l => l.Contains(date));

                    if (line != null)
                    {
                        // Extract the "Total online users" count
                        int startIndex = line.IndexOf("Total online users: ") + "Total online users: ".Length;
                        int endIndex = line.IndexOf(" ", startIndex);
                        string userCount = line.Substring(startIndex, endIndex - startIndex);

                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync($"Total online users: {userCount}");
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Data not found for the specified date and time.");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"An error occurred: {ex.Message}");
                }
            });

            endpoints.MapGet("/api/stats/user", async context =>
            {
                // Get the date and user ID parameters from the query string
                string date = context.Request.Query["date"];
                string userId = context.Request.Query["userId"];

                try
                {
                    // Read and parse the Data2.txt file
                    var lines = System.IO.File.ReadAllLines("Data2.txt");

                    // Find the line that matches the specified date and user ID
                    var matchingLine = lines.FirstOrDefault(line =>
                    {
                        return line.Contains($"Time:{date};") && line.Contains($"ID:{userId};");
                    });

                    if (matchingLine != null)
                    {
                        // Extract the "wasUserOnline" and "nearestOnlineTime" values
                        var wasUserOnline = matchingLine.Contains("wasUserOnline:true");
                        var nearestOnlineTime = "";

                        if (matchingLine.Contains("nearestOnlineTime:"))
                        {
                            int startIndex = matchingLine.IndexOf("nearestOnlineTime:") + "nearestOnlineTime:".Length;
                            int endIndex = matchingLine.IndexOf(";", startIndex);
                            nearestOnlineTime = matchingLine.Substring(startIndex, endIndex - startIndex);
                        }

                        // Prepare the response
                        var responseMessage = $"wasUserOnline: {wasUserOnline}, nearestOnlineTime: {nearestOnlineTime}";

                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync(responseMessage);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Data not found for the specified date and user ID.");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"An error occurred: {ex.Message}");
                }
            });

            bool CalculateOnlineChance(string dataFilePath, DateTime specifiedDate, double tolerance, string userId, out double onlineChance)
            {
                int matchingRecords = 0;
                int totalRecords = 0;

                try
                {
                    using (StreamReader reader = new StreamReader(dataFilePath))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            var userData = ParseUserData(line);

                            if (userData != null && userData.ID == userId)
                            {
                                totalRecords++;

                                DateTime dataDate = DateTime.ParseExact(userData.Time, "dd.MM.yyyy H:mm:ss", CultureInfo.InvariantCulture);
                                TimeSpan timeDifference = specifiedDate - dataDate;

                                if (userData.WasUserOnline == "online")
                                {
                                    matchingRecords++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }

                if (totalRecords == 0)
                {
                    onlineChance = 0;
                }
                else
                {
                    onlineChance = (double)matchingRecords / totalRecords;
                }

                return true; // Always return true to indicate that the calculation was successful.
            }
            UserData ParseUserData(string line)
            {
                var userData = new UserData();
                var parts = line.Split(';');

                foreach (var part in parts)
                {
                    if (part.StartsWith("ID:"))
                    {
                        userData.ID = part.Substring(3);
                    }
                    else if (part.StartsWith("Time:"))
                    {
                        userData.Time = part.Substring(5);
                    }
                    else if (part.StartsWith("nearestOnlineTime:"))
                    {
                        userData.NearestOnlineTime = part.Substring(18);
                    }
                    else if (part.StartsWith("wasUserOnline:"))
                    {
                        userData.WasUserOnline = part.Substring(14);
                    }
                }

                return userData;
            }

            endpoints.MapGet("/api/predictions/users", async context =>
            {
                // Get the query parameters from the URL
                string requestedDateString = context.Request.Query["date"];
                DateTime specifiedDate = DateTime.ParseExact(requestedDateString, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                string toleranceStr = context.Request.Query["tolerance"];
                string userId = context.Request.Query["userId"];

                if (double.TryParse(toleranceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double tolerance))
                {
                    if (CalculateOnlineChance("Data2.txt", specifiedDate, tolerance, userId, out double onlineChance))
                    {
                        var response = new
                        {
                            willBeOnline = onlineChance >= tolerance,
                            onlineChance = onlineChance
                        };

                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsJsonAsync(response);
                    }
                    else
                    {
                        context.Response.StatusCode = 200; // User data not found, onlineChance is 0.
                        await context.Response.WriteAsJsonAsync(new
                        {
                            willBeOnline = false,
                            onlineChance = 0
                        });
                    }
                }
                else
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Invalid tolerance parameter. Please use a valid numeric value.");
                }
            });

            endpoints.MapGet("/api/predictions/user", async context =>
            {
                // Get the query parameters from the URL
                string requestedDateString = context.Request.Query["date"];
                DateTime specifiedDate = DateTime.ParseExact(requestedDateString, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                string toleranceStr = context.Request.Query["tolerance"];
                string userId = context.Request.Query["userId"];

                if (double.TryParse(toleranceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double tolerance))
                {
                    if (CalculateOnlineChance("Data2.txt", specifiedDate, tolerance, userId, out double onlineChance))
                    {
                        var response = new
                        {
                            willBeOnline = onlineChance >= tolerance,
                            onlineChance = onlineChance
                        };

                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsJsonAsync(response);
                    }
                    else
                    {
                        context.Response.StatusCode = 200; // User data not found, onlineChance is 0.
                        await context.Response.WriteAsJsonAsync(new
                        {
                            willBeOnline = false,
                            onlineChance = 0
                        });
                    }
                }
                else
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Invalid tolerance parameter. Please use a valid numeric value.");
                }
            });


        });
    }
}

public class Program
{
    public static void Main()
    {
        CreateHostBuilder().Build().Run();
    }

    public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}


public class UserData
{
    public string? ID { get; set; }
    public string? Time { get; set; }
    public string? NearestOnlineTime { get; set; }
    public string? WasUserOnline { get; set; }
}*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using MoreLinq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

var jsonFilePath = "Data3.json"; // Replace with the actual path to your JSON file
var jsonData = File.ReadAllText(jsonFilePath);
var usersData = JsonConvert.DeserializeObject<Dictionary<Guid, User>>(jsonData);

app.MapGet("/api/users/list", (HttpContext context) =>
{
    var response = usersData.Values.Select(user => new
    {
        username = user.FirstName,
        userId = user.UserId,
        firstSeen = GetFirstSeenDate(user.OnlineRelic)
    });

    context.Response.StatusCode = 200;
    return context.Response.WriteAsJsonAsync(response);
});

static string GetFirstSeenDate(List<OnlineRelic> onlineRelic)
{
    if (onlineRelic == null || onlineRelic.Count == 0)
    {
        return null;
    }

    DateTimeOffset? firstSeenDate = null;

    foreach (var online in onlineRelic)
    {
        if (DateTimeOffset.TryParseExact(online.StartDate, new[] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy H:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            if (!firstSeenDate.HasValue || parsedDate < firstSeenDate)
            {
                firstSeenDate = parsedDate;
            }
        }
    }

    return firstSeenDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
}
app.MapGet("/api/predictions/users", (HttpContext context) =>
{
    // Get the date parameter from the query string
    string date = context.Request.Query["date"];

    try
    {
        // Read and parse the Data.txt file
        var lines = System.IO.File.ReadAllLines("PublicAPI/LetsTry/Data.txt");

        // Extract all "Total online user" counts for the specified date
        var userCounts = lines
            .Where(line => line.Contains(date))
            .Select(line =>
            {
                int startIndex = line.IndexOf("Total online users: ") + "Total online users: ".Length;
                int endIndex = line.IndexOf(" ", startIndex);
                return int.Parse(line.Substring(startIndex, endIndex - startIndex));
            })
            .ToList();

        if (userCounts.Any())
        {
            // Calculate the minimum and maximum user counts
            int minUserCount = userCounts.Min();
            int maxUserCount = userCounts.Max();



            // Generate a random number within the range of user counts for the specified date
            Random random = new Random();
            int randomUserCount = random.Next(minUserCount, maxUserCount + 1);

            context.Response.StatusCode = 200;
            return context.Response.WriteAsync($"onlineUsers: {randomUserCount}");
        }
        else
        {
            int minUserCount = userCounts.Min();
            int maxUserCount = userCounts.Max();
            // If the date does not exist in the file, generate a random number within a reasonable range.
            Random random = new Random();
            int randomUserCount = random.Next(40, 67); // Adjust the range as needed.

            context.Response.StatusCode = 200;
            return context.Response.WriteAsync($"onlineUsers: {randomUserCount}");
        }
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        return context.Response.WriteAsync($"An error occurred: {ex.Message}");
    }
});

app.Run();

bool CalculateOnlineChance(string dataFilePath, DateTime specifiedDate, double tolerance, string userId, out double onlineChance)
{
    int matchingRecords = 0;
    int totalRecords = 0;

    try
    {
        using (StreamReader reader = new StreamReader(dataFilePath))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                var userData = ParseUserData(line);

                if (userData != null && userData.ID == userId)
                {
                    totalRecords++;

                    DateTime dataDate = DateTime.ParseExact(userData.Time, "dd.MM.yyyy H:mm:ss", CultureInfo.InvariantCulture);
                    TimeSpan timeDifference = specifiedDate - dataDate;

                    if (userData.WasUserOnline == "online")
                    {
                        matchingRecords++;
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
    }

    if (totalRecords == 0)
    {
        onlineChance = 0;
    }
    else
    {
        onlineChance = (double)matchingRecords / totalRecords;
    }

    return true; // Always return true to indicate that the calculation was successful.
}


UserData ParseUserData(string line)
{
    var userData = new UserData();
    var parts = line.Split(';');

    foreach (var part in parts)
    {
        if (part.StartsWith("ID:"))
        {
            userData.ID = part.Substring(3);
        }
        else if (part.StartsWith("Time:"))
        {
            userData.Time = part.Substring(5);
        }
        else if (part.StartsWith("nearestOnlineTime:"))
        {
            userData.NearestOnlineTime = part.Substring(18);
        }
        else if (part.StartsWith("wasUserOnline:"))
        {
            userData.WasUserOnline = part.Substring(14);
        }
    }

    return userData;
}



public class User
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; }
    public List<OnlineRelic> OnlineRelic { get; set; }
}

public class OnlineRelic
{
    public string StartDate { get; set; }
    public string EndDate { get; set; }
}
public class UserData
{
    public string? ID { get; set; }
    public string? Time { get; set; }
    public string? NearestOnlineTime { get; set; }
    public string? WasUserOnline { get; set; }

    public Dictionary<string, User> Users { get; set; }
}