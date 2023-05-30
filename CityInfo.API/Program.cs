using CityInfo.API;
using CityInfo.API.DbContexts;
using CityInfo.API.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/cityinfo.txt", rollingInterval: RollingInterval.Day) //location we want to log to, second parameters indicates that a new log file will be created everyday
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args); 
//builder.Logging.ClearProviders(); //we can manually define the logging services insetead of implement defined in CreateBuilder
//builder.Logging.AddConsole();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers(options =>    //AddControllers registers services that are typically required when building apis, like support for controllers, model binding, data annotations...
{
    options.ReturnHttpNotAcceptable = true; //returns status as 406 not acceptable, ise application doesn't support it.
}).AddNewtonsoftJson() //adds partial update services
.AddXmlDataContractSerializerFormatters();  //Adds XML input and output formatters


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer(); //therese two register the required services on the container that are needed by swagger implementation
builder.Services.AddSwaggerGen(setupAction =>   //registers services that are used for effectively generating the spec
{
    var xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlCommentsFullPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFile);

    setupAction.IncludeXmlComments(xmlCommentsFullPath);

    setupAction.AddSecurityDefinition("CityInfoApiBearerAuth", new Microsoft.OpenApi.Models.OpenApiSecurityScheme()
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        Description = "Input a valid token to access API"
    });

    setupAction.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "CityInfoApiBearerAuth" 
                }
            }, new List<string>()
        }
    });
}); 

builder.Services.AddSingleton<FileExtensionContentTypeProvider>(); //allwos us to inject FileExtensionContentTypeProvider

#if DEBUG
builder.Services.AddTransient<IMailService, LocalMailService>(); //we add LocalMailService to use
#else
builder.Services.AddTransient<IMailService, CloudMailService>();
#endif

builder.Services.AddTransient<CitiesDataStore>();

builder.Services.AddDbContext<CityInfoContext>(dbContextOptions => dbContextOptions.UseSqlite(builder.Configuration["ConnectionStrings:CityInfoDBConnectionString"])); //we have our connection string in appsettings.Development.json 

builder.Services.AddScoped<ICityInfoRepository, CityInfoRepository>(); //add interface and the implementation (rep pattern)

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies()); //ensures that the current assembly, so the city in front of the API assembly will be scanned for profiles 

builder.Services.AddAuthentication("Bearer") //we create access for tokens we created in app
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Authentication:Issuer"],
            ValidAudience = builder.Configuration["Authentication:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Authentication:SecretForKey"]))
        };
    }); //how we're going to validate token


//add authorization policy MustBeFromAntwerp is a name of policy, to activate this polici we want to write in the namespace [Authorize (Policy="MustBeFromAntwerp")]
builder.Services.AddAuthorization(options => {
    options.AddPolicy("MustBeFromAntwerp", policy =>
    {
    policy.RequireAuthenticatedUser(); //we want authentificated user
    policy.RequireClaim("city", "Antwerp"); //we wantvalue of claimtype to be Antwerp
    });
});

builder.Services.AddApiVersioning(setupAction =>
{
    setupAction.AssumeDefaultVersionWhenUnspecified = true;
    setupAction.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    setupAction.ReportApiVersions= true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); //middlewares (and others after this) they handle HTTP requests
    app.UseSwaggerUI(); //adds the middleware that uses that spec to generate the default swagger UI documentation 
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

//app.Run(async (context) =>
//{
//    await context.Response.WriteAsync("hello world");
//});

app.Run();
