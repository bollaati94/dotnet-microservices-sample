using Nest;
using ConsumerService;

var builder = WebApplication.CreateBuilder(args);

// Elasticsearch setup
var elasticSettings = new ConnectionSettings(new Uri("http://elasticsearch:9200"));
builder.Services.AddSingleton<IElasticClient>(new ElasticClient(elasticSettings));

// Add Consumer as Background Service
builder.Services.AddHostedService<GateEventConsumer>();

var app = builder.Build();
app.MapGet("/", () => "Consumer Service is running...");
app.Run();
