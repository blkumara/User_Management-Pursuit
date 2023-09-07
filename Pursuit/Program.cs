
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pursuit.Context;
using Pursuit.Context.AD;
using Pursuit.Context.ConfigFile;
using Pursuit.Helpers;
using Pursuit.Model;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Formatting.Compact;
using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;


/* =========================================================
    Item Name: DB,AD and swagger integration - Program
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */

var builder = WebApplication.CreateBuilder(args);

List<WriteTo> writetoconfig = builder.Configuration.GetSection("Serilog").GetSection("WriteTo").Get<List<WriteTo>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
          .AddJwtBearer(options =>
          {
              options.TokenValidationParameters = new TokenValidationParameters
              {
                  ValidateIssuer = true,
                  ValidateAudience = true,
                  ValidateLifetime = true,
                  ValidateIssuerSigningKey = true,
                  ValidIssuer = builder.Configuration["Jwt:Issuer"],
                  ValidAudience = builder.Configuration["Jwt:Issuer"],
                  IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
              };
          });

var SeriLogger = new LoggerConfiguration()
                                .Enrich.FromLogContext()
                                .MinimumLevel.Debug()
                                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                                .Enrich.FromLogContext()
                                // Add this line:
                                .WriteTo.MongoDBBson(
                                   writetoconfig.FirstOrDefault().Args.databaseUrl,
                                    "log", LogEventLevel.Debug)
                         .CreateLogger();



builder.Services.AddSingleton<IPursuitDBSettings>(serviceProvider =>
    serviceProvider.GetRequiredService<IOptions<PursuitDBSettings>>().Value);


builder.Services.Configure<PursuitDBSettings>(
    builder.Configuration.GetSection("PursuitDatabase"));

builder.Services.AddSingleton<IConfigSettings>(serviceProvider =>
    serviceProvider.GetRequiredService<IOptions<ConfigSettings>>().Value);



builder.Services.Configure <ConfigSettings>(
    builder.Configuration.GetSection("ConfigSettings"));


builder.Services.AddSingleton<ISerilogs>(serviceProvider =>
    serviceProvider.GetRequiredService<IOptions<Serilogs>>().Value);

builder.Services.Configure<Serilogs>(
    builder.Configuration.GetSection("Serilog"));



builder.Services.AddSingleton<IADDBSettings>(sP =>
    sP.GetRequiredService<IOptions<ADDBSettings>>().Value);

builder.Services.Configure<ADDBSettings>(builder.Configuration.GetSection("ADDatabase"));


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);


builder.Services.ConfigureWritable<PursuitDBSettings>(builder.Configuration.GetSection("PursuitDatabase"));
builder.Services.ConfigureWritable<ADDBSettings>(builder.Configuration.GetSection("ADDatabase"));
builder.Services.ConfigureWritable<ConfigSettings>(builder.Configuration.GetSection("ConfigSettings"));
builder.Services.ConfigureWritable<Serilogs>(builder.Configuration.GetSection("Serilog"));

builder.Services.Configure<PursuitDBSettings>(builder.Configuration.GetSection("PursuitDatabase"));
builder.Services.Configure<ADDBSettings>(builder.Configuration.GetSection("ADDatabase"));
builder.Services.Configure<ConfigSettings>(builder.Configuration.GetSection("ConfigSettings"));
builder.Services.Configure<Serilogs>(builder.Configuration.GetSection("Serilog"));

builder.Configuration.AddEnvironmentVariables();

builder.Services
    .AddCors()
    .AddControllers()
    .AddJsonOptions(opt => {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.Converters.Add(new JsonObjectIdConverter());
    });


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:100.64.0.0"), 106));
});


builder.Logging.ClearProviders();
builder.Logging
    .AddSerilog(SeriLogger);
Serilog.Debugging.SelfLog.Enable(Console.Error);

builder.Services.AddScoped(typeof(IPursuitRepository<>), typeof(PursuitRepository<>));
builder.Services.AddScoped(typeof(IADRepository<>), typeof(ADRepository<>));

builder.Services.AddScoped(typeof(IDeltaRepository<>), typeof(ADDeltaRepository<>));

builder.Services.AddTransient<GoogleRepository<ADRecord>>();
builder.Services.AddTransient<ADRepository<ADRecord>>();
builder.Services.AddTransient<AzureRepository<ADRecord>>();



builder.Services.AddTransient<ADDeltaRepository<DeltaModel>>();
builder.Services.AddTransient<AzureDeltaRepository<DeltaModel>>();
builder.Services.AddTransient<GWSDeltaRepository<DeltaModel>>();

builder.Services.AddTransient<ServiceResolver>(serviceProvider => key =>
{
    switch (key)
    {
        case "GWS":
            return serviceProvider.GetService<GoogleRepository<ADRecord>>();
        case "MS":
            return serviceProvider.GetService<ADRepository<ADRecord>>();
        case "AZ":
            return serviceProvider.GetService<AzureRepository<ADRecord>>();
        default:
            throw new KeyNotFoundException(); // or maybe return null, up to you
    }
});

builder.Services.AddTransient<DeltaServiceResolver>(serviceProvider => key =>
{
    switch (key)
    {

        case "MSDelta":
            return serviceProvider.GetService<ADDeltaRepository<DeltaModel>>();
        case "GWSDelta":
            return serviceProvider.GetService<GWSDeltaRepository<DeltaModel>>();
        case "AZDelta":
            return serviceProvider.GetService<AzureDeltaRepository<DeltaModel>>();
        default:
            throw new KeyNotFoundException(); // or maybe return null, up to you
    }
});


builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                                                    new HeaderApiVersionReader("x-api-version"),
                                                    new MediaTypeApiVersionReader("x-api-version"));
});

// Add ApiExplorer to discover versions
builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddScoped(typeof(IDailyJob), typeof(DailyJob));

builder.Services.AddSwaggerGen(c => {
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

 
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type=Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
});

builder.Services.ConfigureOptions<ConfigureSwagger>();


var app = builder.Build();

var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
    app.UseExceptionHandler(
         new ExceptionHandlerOptions()
         {
             AllowStatusCode404Response = true, // important!
             ExceptionHandlingPath = "/error"
         }
     );
    app.UseHsts();
}


//app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseSwagger();


app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());



app.MapControllers();


app.Run();
