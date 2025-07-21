using Booking.Repository;
using Microsoft.EntityFrameworkCore;
using Ophusdev.Kafka.Abstraction;
using Ophusdev.Kafka.Configuration;
using Ophusdev.Kafka.Consumer;
using Ophusdev.Kafka.Extensions;
using Ophusdev.Kafka.Producer;
using Ophusdev.Orchestrator.Api.Extensions;
using Ophusdev.Orchestrator.Business;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Business.Services;
using Orchestrator.Repository;
using Orchestrator.Repository.Abstraction;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

// Add services to the container.
builder.Services.AddDbContext<BookingDbContext>(options => options.UseSqlServer("name=ConnectionStrings:OrchestratorDbContext", b => b.MigrationsAssembly("Ophusdev.Orchestrator.Api")));

builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<IBusiness, Business>();

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<ITopicTranslator, TopicTranslator>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddSingleton<IKafkaConsumer, KafkaConsumer>();

builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddScoped<IBookingSagaOrchestrator, BookingSagaOrchestrator>();

builder.Services.AddKafkaTopicHandlers();

builder.Services.AddHostedService<SagaConsumerService>();

builder.Services.AddOphusdevInventoryClient(builder.Configuration);

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

await app.RunAsync();
