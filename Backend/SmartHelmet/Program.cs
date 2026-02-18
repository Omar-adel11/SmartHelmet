
using System.Text;
using BLL.Abstractions;
using BLL.Services;
using BLL.Services.Helper;
using BLL.Services.Helper.Email;
using BLL.Services.Rides;
using DAL.Users;
using DAL.Users.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SmartHelmet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //DB
            builder.Services.AddDbContext<DBContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            //Identity
            builder.Services.AddIdentity<AppUser,IdentityRole<int>>()
                .AddEntityFrameworkStores<DBContext>()
                .AddDefaultTokenProviders(); 

            //bearer token authentication
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JWTOptions"));
           

            builder.Services.AddAuthentication(AuthenticationOptions =>
            {
                AuthenticationOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                AuthenticationOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JWTOptions:Issuer"],
                    ValidAudience = builder.Configuration["JWTOptions:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWTOptions:SecretKey"]))
                };
            }); ;

            //DI for Repository
            builder.Services.AddScoped<IRepository<EmergencyContact, int>, Repository<EmergencyContact, int>>();

            //mail service
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JWTOptions"));
            builder.Services.AddScoped<IEmailService, EmailService>();


            //DI for UnitOfWork
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            //DI for serviceManager
            builder.Services.AddScoped<IServiceManager, ServiceManager>();

            //Ride services
            builder.Services.AddScoped<IRideService, RideService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
