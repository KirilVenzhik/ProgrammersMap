using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using Microsoft.EntityFrameworkCore;
using ProgChecklist.Data;
using ProgChecklist.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<WebEncoderOptions>(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddTopicCatalog(builder.Configuration);
builder.Services.AddResourceCatalog(builder.Configuration);
builder.Services.AddLessonStore(builder.Configuration);
builder.Services.AddSiteUrls(builder.Configuration);
builder.Services.AddAppDatabase(builder.Configuration);
builder.Services.AddAppIdentity();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapSeoEndpoints();
app.MapRazorPages();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
