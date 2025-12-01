using CompilationLib;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SchemaFilter<DefaultValuesSwaggerExtensions>();
});
builder.Services.AddScoped<ICompileHandler, CompileHandler>();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/compile", async (CompileRequest a, ICompileHandler compileHandler, CancellationToken cancellationToken) =>
{
    var response= await compileHandler.Handle(a,cancellationToken);
    return response;
}).WithName("Compile");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
public class DefaultValueAttribute : Attribute
{
    public IOpenApiPrimitive Value { get; set; }


    public DefaultValueAttribute(PrimitiveType type, string value = "")
    {
        SetValue(type, value);
    }

    public DefaultValueAttribute(PrimitiveType type = PrimitiveType.DateTime, int addYears = 0, int addDays = 0, int addMonths = 0)
    {
        SetValue(type, "", addYears, addDays, addMonths);
    }

    private void SetValue(PrimitiveType Type, string value = "", int addYears = 0, int addDays = 0, int addMonths = 0)
    {
        switch (Type)
        {
            case PrimitiveType.Integer:
                Value = new OpenApiInteger(Convert.ToInt32(value));
                break;
            case PrimitiveType.Long:
                Value = new OpenApiLong(Convert.ToInt64(value));
                break;
            case PrimitiveType.Float:
                Value = new OpenApiFloat(Convert.ToUInt64(value));
                break;
            case PrimitiveType.Double:
                Value = new OpenApiDouble(Convert.ToDouble(value));
                break;
            case PrimitiveType.String:
                Value = new OpenApiString(value);
                break;
            case PrimitiveType.Byte:
                Value = new OpenApiByte(Convert.ToByte(value));
                break;
            case PrimitiveType.Binary:
                Value = new OpenApiBinary(value.ToCharArray().Select(c => Convert.ToByte(c)).ToArray());
                break;
            case PrimitiveType.Boolean:
                Value = new OpenApiBoolean(Convert.ToBoolean(value));
                break;
            case PrimitiveType.Date:
                break;
            case PrimitiveType.DateTime:
                Value = new OpenApiDate(DateTime.Now.AddYears(addYears).AddDays(addDays).AddMonths(addMonths));
                break;
            case PrimitiveType.Password:
                Value = new OpenApiPassword(value);
                break;
            default:
                break;
        }
    }

}

public class DefaultValuesSwaggerExtensions : Swashbuckle.AspNetCore.SwaggerGen.ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var attributes = context?.MemberInfo?.GetCustomAttributes(true).OfType<DefaultValueAttribute>();

        if (attributes?.Any() == true)
        {
            schema.Default = attributes.First().Value;
        }
    }
}