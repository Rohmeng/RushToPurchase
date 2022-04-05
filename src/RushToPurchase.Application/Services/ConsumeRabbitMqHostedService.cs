using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RushToPurchase.Domain.Interfaces;
using Serilog;

namespace RushToPurchase.Application.Services;

public class ConsumeRabbitMqHostedService : BackgroundService
{
    private const string ExchangeName = "order.topic";
    private const string CacheQueueName = "Cache_Queue";
    private const string PurchaseOrderQueueName = "PurchaseOrder_Queue";
    private const string AllQueueName = "AllTopic_Queue";
    private IConnection _connection;
    private IModel _channel;
    private readonly IOrderService _orderService;

    public ConsumeRabbitMqHostedService(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var orderServices = 
                scope.ServiceProvider
                    .GetRequiredService<IEnumerable<IOrderService>>();
            _orderService = orderServices.Last();
        }
        
        var factory = new ConnectionFactory {Uri = new Uri(configuration.GetConnectionString("rabbitmq"))};
        // create connection  
        _connection = factory.CreateConnection();

        // create channel  
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);
        _channel.QueueDeclare(CacheQueueName, true, false, false, null);
        _channel.QueueDeclare(PurchaseOrderQueueName, true, false, false, null);
        _channel.QueueDeclare(AllQueueName, true, false, false, null);

        _channel.QueueBind(CacheQueueName, ExchangeName, "cache.crud");
        _channel.QueueBind(PurchaseOrderQueueName, ExchangeName, "order.crud");
        _channel.QueueBind(AllQueueName, ExchangeName, "*");
        _channel.BasicQos(0, 1, false);

        _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (ch, ea) =>
        {
            var headers = ea.BasicProperties.Headers;
            // received message  
            var content = Encoding.UTF8.GetString(ea.Body.Span);
            Log.Information($"Mq Consumer: Exchange:{ea.Exchange}, RoutingKey:{ea.RoutingKey}, Content:{content}, DeliveryTag:{ea.DeliveryTag}, ConsumerTag:{ea.ConsumerTag}");
            // handle the received message  
            if (ea.RoutingKey == "cache.crud")
                _orderService.KeyDelMessage(content);
            if (ea.RoutingKey == "order.crud")
                HandleOrderMessage(ea.Body);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        // consumer.Shutdown += OnConsumerShutdown;
        // consumer.Registered += OnConsumerRegistered;
        // consumer.Unregistered += OnConsumerUnregistered;
        // consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

        _channel.BasicConsume(CacheQueueName, false, consumer);
        _channel.BasicConsume(PurchaseOrderQueueName, false, consumer);
        return Task.CompletedTask;
    }
    
    private void HandleOrderMessage(ReadOnlyMemory<byte> content)
    {
        var userinfo = JsonSerializer.Deserialize<List<int>>(content.Span);
        // we just print this message   
        _orderService.CreateOrderByMqConsumer(userinfo[0], userinfo[1]);
        Log.Information($"[HandleOrderMessage] consumer received {userinfo.ToArray()}");
    }

    private void HandleAllMessage(string content)
    {
        // we just print this message   
        Log.Information($"[HandleAllMessage] consumer received {content}");
    }

    private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}