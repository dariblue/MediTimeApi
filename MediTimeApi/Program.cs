using MediTimeApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<UsuarioService>();
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Usa CORS antes de los endpoints
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();
app.Run();
