using Microsoft.EntityFrameworkCore;
using EcomUserApi.Models;
using EcomUserApi.Services;
using EcomUserApi.RabbitMQ;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<UserDatabaseSettings>(
    builder.Configuration.GetSection("UserDatabase"));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<UserService>();
var rabbitHostName = Environment.GetEnvironmentVariable("RABBIT_HOSTNAME");
var connectionFactory = new ConnectionFactory
{
    HostName = rabbitHostName ?? "localhost",
    Port = 5672,
    UserName = "producer",
    Password = "producer"
};
var rabbitMqConnection = connectionFactory.CreateConnection();
builder.Services.AddSingleton(rabbitMqConnection);
builder.Services.AddSingleton<IRabbitMQClient, RabbitMQClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
