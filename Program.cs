using HlkHelpers;
using MFAServer.Data;
using MFAServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using MFAServer.Logic;

try
{

    var builder = WebApplication.CreateBuilder(args);


    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        options.EnableDetailedErrors(true);
        options.EnableSensitiveDataLogging(true);
    });

 //   builder.Logging.AddConsole();

    builder.Services.Configure<IdentityOptions>(options =>
    {
        // Password settings.
        options.Password = new PasswordOptions()
        {
            RequireDigit = false,
            RequiredLength = 5,
            RequireLowercase = false,
            RequireUppercase = false,
            RequireNonAlphanumeric = false,
            RequiredUniqueChars = 1
        };

        // Lockout settings.
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(600);
        options.Lockout.MaxFailedAccessAttempts = 50;
        options.Lockout.AllowedForNewUsers = true;

        // User settings.
        options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._+?!@#$%^&*";
        options.User.RequireUniqueEmail = false;

    });

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims

    // config user identity
    builder.Services.AddIdentityCore<User>().AddRoles<Role>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders().AddRoleValidator<RoleValidator<Role>>()
        .AddRoleManager<RoleManager<Role>>().AddSignInManager<SignInManager<User>>();

    var secret = builder.Configuration.GetSection("AppSettings:Token").Value ?? "";

    // configure token generation
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
          //
          options.RequireHttpsMetadata = false;
          options.SaveToken = true;
          options.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuerSigningKey = true,
              IssuerSigningKey = new SymmetricSecurityKey(Utils.StringToByteArray(secret)),
              //
              ValidateIssuer = true,
              ValidIssuer = builder.Configuration["AppSettings:Issuer"],
              //
              ValidateAudience = true,
              AudienceValidator = (aud, token, valdationParam) => aud.All(x => x == builder.Configuration["AppSettings:Audience"] || (x.Length == 15 && long.TryParse(x, out _))),
              //ValidAudience = builder.Configuration["AppSettings:Audience"],
              ValidateLifetime = true,
              ClockSkew = TimeSpan.Zero // remove delay of token when expire
          };

      });


    builder.Services.AddAuthorization();

    builder.Services.AddEndpointsApiExplorer();
    // needed for swagger api versioning
    builder.Services.AddControllers(options => options.Conventions.Add(new SwaggerApiGrouping()));
    // config jwt auth for swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "MFAServer API", Version = "v1" });

        var secScheme = new OpenApiSecurityScheme()
        {
            Description = "Jwt Authorization: login with creds and copy and paste jwt token into box below",
            Name = "Authorization",
            In = ParameterLocation.Header,
            BearerFormat = "JWT",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        c.AddSecurityDefinition("Bearer", secScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                      {secScheme , Array.Empty<string>() }
                 });
    });

    builder.Services.AddSingleton<TwoFactorAuthLogic>();

    builder.Services.AddCors();

    var app = builder.Build();
    var env = app.Environment;

    //// run migrations and seed data
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
            SeedData.Initialize(scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex.Message, "An error occurred seeding the DB.");
            logger.LogError(ex, "{action} failed: {error}", "seedData", ex.GetFullErrorMessage());
        }
    }



    //// Configure the HTTP request pipeline.
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection(); 
    app.UseStaticFiles();

    app.UseRouting();

    app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

    //the order of this 2 below matters, u have to authenticate before authorize
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MFAServer API Docs V1");
    });

    app.MapControllers();


    app.MapGet("/", () => $"Am up @ {DateTime.Now}");

    app.Run();

}
catch (Exception ex)
{
    throw;
}
