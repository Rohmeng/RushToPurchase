using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Serilog;

namespace RushToPurchase.Infra.Data.Mq;

public class RabbitmqClient: IRabbitmqClient, IDisposable
{
    private const string ExchangeName = "order.topic";
    private const string CacheQueueName = "Cache_Queue";
    private const string PurchaseOrderQueueName = "PurchaseOrder_Queue";
    private const string AllQueueName = "AllTopic_Queue";
    private readonly ConnectionFactory _connectionFactory;
    private readonly IConnection _connection;
    private IModel _model;

    public RabbitmqClient(IConfiguration configuration)
    {
        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(configuration.GetConnectionString("rabbitmq"))
        };
        _connection = _connectionFactory.CreateConnection();
        _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        _model = _connection.CreateModel();

        _model.ExchangeDeclare(ExchangeName, "topic", true);

        _model.QueueDeclare(CacheQueueName, true, false, false, null);
        _model.QueueDeclare(PurchaseOrderQueueName, true, false, false, null);
        _model.QueueDeclare(AllQueueName, true, false, false, null);

        _model.QueueBind(CacheQueueName, ExchangeName, "cache.crud");
        _model.QueueBind(PurchaseOrderQueueName, ExchangeName, "order.crud");
        _model.QueueBind(AllQueueName, ExchangeName, "*");
    }
    
    private void SendMessage(ReadOnlyMemory<byte> body, string routingKey)
    {
        var props = _model.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2;
        props.CorrelationId = Guid.NewGuid().ToString();
        Log.Information($"producer sending message, length:{body.Length}, routing key:{routingKey}");
        _model.BasicPublish(ExchangeName, routingKey, props, body);
    }

    private void RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        Log.Information("RabbitMQ Connection Shutdown");
    }
    
    public void Dispose()
    {
    }

    public void DeleteCacheKey(string key)
    {
        var body = Encoding.UTF8.GetBytes(key);
        SendMessage(body, "cache.crud");
    }

    public void CreateOrder(int sid, int uid)
    {
        var list = new List<int>() {sid, uid};
        var body = JsonSerializer.SerializeToUtf8Bytes(list);
        SendMessage(body, "order.crud");
    }
}