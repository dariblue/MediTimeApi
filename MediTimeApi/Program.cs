using MediTimeApi;
using MediTimeApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar Database como servicio Scoped para DI
builder.Services.AddScoped<Database>();

// Registrar todos los servicios
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<MedicamentoService>();
builder.Services.AddScoped<PacienteCuidadorService>();
builder.Services.AddScoped<HistorialTomaService>();
builder.Services.AddScoped<PushSubscriptionService>();

// Habilita CORS globalmente
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Swagger y controladores
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v2.1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MediTime API",
        Version = "2.1",
        Description = "API REST para la gestión y supervisión de toma de medicamentos — MediTime v2.1"
    });
});

var app = builder.Build();

Console.WriteLine("=== MediTime API v2.1 ===");
Console.WriteLine($"Entorno: {app.Environment.EnvironmentName}");

// Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v2.1/swagger.json", "MediTime API v2.1");
    });
}

// Usa CORS antes de los endpoints
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();
app.Run();
