using System.Diagnostics;
using Destructurama;
using Destructurama.Attributed;
using Serilog;
using SerilogTryOut;

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.CaptureStartupErrors(true);
    builder.AddObservability();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.MapGet("exception",
            (ILogger<Program> logger) =>
            {
                logger.LogError("Going to throw exception here");
                throw new Exception("Test");
            })
        .WithName("Exception");
    app.MapGet("/users",
            (ILogger<Program> logger) =>
            {
                var users = Enumerable.Range(1, 5).Select(index =>
                    {
                        const string generatingNewUser = "Generating new user";
                        using (var activity = Activity.Current?.Source.StartActivity(generatingNewUser))
                        {
                            var user = new User(
                                Guid.NewGuid(),
                                Faker.Name.First(),
                                Faker.Name.Last(),
                                Faker.RandomNumber.Next(18, 30),
                                Faker.Identification.SocialSecurityNumber()
                            );

                            logger.LogInformation("Creating {@User}", user);
                            activity?.SetStatus(index % 2 == 0 ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                            return user;
                        }
                    })
                    .ToArray();

                return users;
            })
        .WithName("Users")
        .WithOpenApi();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

internal record User
{

    public User(Guid Id, string FirstName, string LastName, int Age, string SocialSecurityNumber)
    {
        this.SocialSecurityNumber = SocialSecurityNumber;
        this.Id = Id;
        this.FirstName = FirstName;
        this.LastName = LastName;
        this.Age = Age;
    }


    public Guid Id { get; init; }

    [NotLogged]
    public string FirstName { get; init; }

    [NotLogged]
    public string LastName { get; init; }

    [NotLogged]
    public int Age { get; init; }

    [LogMasked(PreserveLength = true)]
    public string SocialSecurityNumber { get; init; }
}