using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MessagingApiLambda;

public class Function
{
    private static readonly string? QueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL");
    private static readonly string? TopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN");

    private readonly IAmazonSQS _sqs;
    private readonly IAmazonSimpleNotificationService _sns;

    public Function()
    {
        _sqs = new AmazonSQSClient();
        _sns = new AmazonSimpleNotificationServiceClient();
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var path = request.RawPath ?? "/";
        var method = request.RequestContext?.Http?.Method ?? "GET";

        if (method == "GET" && path.EndsWith("/status"))
        {
            return Ok(new
            {
                ok = true,
                service = "queueflow-messaging-api",
                time = DateTimeOffset.UtcNow
            });
        }

        var body = request.Body ?? string.Empty;

        Payload? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch (JsonException)
        {
            return BadRequest("JSON inválido. Ex: {\"message\":\"hello\"}");
        }

        payload ??= new Payload();

        if (method == "POST" && (path.EndsWith("/enqueue") || path.EndsWith("/publish")) &&
            string.IsNullOrWhiteSpace(payload.Message))
        {
            return BadRequest("Campo obrigatório ausente: message");
        }

        if (method == "POST" && path.EndsWith("/enqueue"))
        {
            if (string.IsNullOrWhiteSpace(QueueUrl))
                return ServerError("SQS_QUEUE_URL não configurada");

            var send = new SendMessageRequest
            {
                QueueUrl = QueueUrl,
                MessageBody = payload.Message
            };

            if (payload.Attributes is not null)
            {
                foreach (var kv in payload.Attributes)
                {
                    send.MessageAttributes[kv.Key] = new Amazon.SQS.Model.MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = kv.Value
                    };
                }
            }

            var resp = await _sqs.SendMessageAsync(send);

            return Ok(new { ok = true, messageId = resp.MessageId });
        }

        if (method == "POST" && path.EndsWith("/publish"))
        {
            if (string.IsNullOrWhiteSpace(TopicArn))
                return ServerError("SNS_TOPIC_ARN não configurada");

            var pub = new PublishRequest
            {
                TopicArn = TopicArn,
                Message = payload.Message
            };

            if (payload.Attributes is not null)
            {
                pub.MessageAttributes = new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>();

                foreach (var kv in payload.Attributes)
                {
                    pub.MessageAttributes[kv.Key] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = kv.Value
                    };
                }
            }

            var resp = await _sns.PublishAsync(pub);

            return Ok(new { ok = true, messageId = resp.MessageId });
        }

        return NotFound();
    }

    private static APIGatewayHttpApiV2ProxyResponse Ok(object obj) =>
        new()
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string> { ["content-type"] = "application/json" },
            Body = JsonSerializer.Serialize(obj)
        };

    private static APIGatewayHttpApiV2ProxyResponse BadRequest(string msg) =>
        new()
        {
            StatusCode = 400,
            Headers = new Dictionary<string, string> { ["content-type"] = "application/json" },
            Body = JsonSerializer.Serialize(new { ok = false, error = msg })
        };

    private static APIGatewayHttpApiV2ProxyResponse NotFound() =>
        new()
        {
            StatusCode = 404,
            Headers = new Dictionary<string, string> { ["content-type"] = "application/json" },
            Body = JsonSerializer.Serialize(new { ok = false, error = "Not Found" })
        };

    private static APIGatewayHttpApiV2ProxyResponse ServerError(string msg) =>
        new()
        {
            StatusCode = 500,
            Headers = new Dictionary<string, string> { ["content-type"] = "application/json" },
            Body = JsonSerializer.Serialize(new { ok = false, error = msg })
        };

    private sealed class Payload
    {
        public string? Message { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
    }
}
