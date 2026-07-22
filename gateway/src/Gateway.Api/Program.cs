var builder = WebApplication.CreateBuilder(args);

// Route/cluster tablosu appsettings'ten yuklenir (Faz 2'de doldurulacak — Core_Principles §9)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "gateway" },
    error = (object?)null
}));

app.MapReverseProxy();

app.Run();
