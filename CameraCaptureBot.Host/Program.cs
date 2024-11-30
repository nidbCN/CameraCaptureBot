using CameraCaptureBot.Host;

var builder = Host.CreateDefaultBuilder(args);

// builder.UseEnvironment("");

var host = builder.Build();

host.Run();
