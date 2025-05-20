using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using System.Text.Json.Serialization; // Para JsonStringEnumConverter global
using UserManagementApi.Services; // Asegúrate de tener este using

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar DbContext para PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Configurar controladores y JSON options para enums
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Esto asegura que los enums se serialicen como strings en las respuestas JSON
        // y se deserialicen correctamente desde strings en las peticiones.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// 3. Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Esto ayuda a Swagger a entender los enums como strings
    c.SchemaFilter<EnumSchemaFilter>(); // Necesitarás crear esta clase si quieres que Swagger muestre los nombres de los enums
});

// Opcional: CORS si tu frontend está en otro dominio
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IEmailService, ConsoleEmailService>(); // Usamos AddScoped
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:4200") // URL de desarrollo de Angular
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                          // En producción, cambia "http://localhost:4200" por tu dominio de frontend
                          // policy.WithOrigins("https://megasolucion.es")
                          //       .AllowAnyHeader()
                          //       .AllowAnyMethod();
                      });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Aplicar migraciones automáticamente en desarrollo (opcional, considera hacerlo manualmente en producción)
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //dbContext.Database.EnsureCreated(); // Si no usas migraciones
        dbContext.Database.Migrate(); // Si usas migraciones
    }
}

app.UseHttpsRedirection();

// Usar CORS (si lo configuraste)
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();


// Clase auxiliar para Swagger y Enums (colócala al final de Program.cs o en un archivo separado)
public class EnumSchemaFilter : Swashbuckle.AspNetCore.SwaggerGen.ISchemaFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiSchema schema, Swashbuckle.AspNetCore.SwaggerGen.SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            Enum.GetNames(context.Type)
                .ToList()
                .ForEach(name => schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(name)));
            schema.Type = "string"; // Muestra el enum como un string en la UI de Swagger
        }
    }
}