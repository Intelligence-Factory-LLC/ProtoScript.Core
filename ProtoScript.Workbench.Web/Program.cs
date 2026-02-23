using BasicUtilities;
using Buffaly.Common;
using RooTrax.Common;
using WebAppUtilities;

ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
configurationBuilder.AddEnvironmentVariables();
IConfigurationRoot config = configurationBuilder.Build();

BasicUtilities.Settings.SetAppSettings(config.GetSection("AppSettings"));
StartupConfigurator.ConfigureLogs(config);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

ProtoScript.Extensions.ProtoScriptWorkbench.SetWebRoot(app.Environment.WebRootPath);

JsonWsOptions jsonWsOptions = config.GetSection("JsonWs").Get<JsonWsOptions>() ?? new JsonWsOptions();

app.UseStaticFiles();
app.UseRouting();

JsonWsHandlerService.RegisterJsonWs(app, jsonWsOptions, x => true);

app.MapGet("/", context =>
{
	context.Response.Redirect("/index.html", permanent: false);
	return Task.CompletedTask;
});

app.Run();
