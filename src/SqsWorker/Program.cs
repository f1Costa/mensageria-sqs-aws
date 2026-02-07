using Amazon.SQS;
using Amazon.SQS.Model;
using System;


var queueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL");
if (string.IsNullOrWhiteSpace(queueUrl))
{
    Console.WriteLine("SQS_QUEUE_URL não configurado.");
    return;
}

using var sqs = new AmazonSQSClient();

Console.WriteLine($"[QueueFlow] Worker iniciado. Consumindo de: {queueUrl}");

while (true)
{
    var resp = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
    {
        QueueUrl = queueUrl,
        MaxNumberOfMessages = 5,
        WaitTimeSeconds = 20,
        VisibilityTimeout = 30
    });

    foreach (var msg in resp.Messages)
    {
        try
        {
            Console.WriteLine($"[PROCESS] {msg.MessageId}: {msg.Body}");
            await sqs.DeleteMessageAsync(queueUrl, msg.ReceiptHandle);
            Console.WriteLine($"[OK] deletada: {msg.MessageId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO] {msg.MessageId}: {ex}");
        }
    }
}
