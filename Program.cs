using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using SupportRequests.Models;

// ключі
var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("config.json"))!;

var sqs = new AmazonSQSClient(config["AccessKey"], config["SecretKey"], RegionEndpoint.EUCentral1);
var queueUrl = config["QueueUrl"];

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1 - Create request");
    Console.WriteLine("2 - Process all requests");
    Console.WriteLine("3 - Show approximate number of requests in queue");
    Console.WriteLine("0 - Exit");
    Console.Write("Your choice: ");

    var choice = Console.ReadLine();
    Console.WriteLine();

    if (choice == "1")
    {
        await CreateRequest();
    }
    else if (choice == "2")
    {
        await ProcessRequests();
    }
    else if (choice == "3")
    {
        await ShowCount();
    }
    else if (choice == "0")
    {
        break;
    }
    else
    {
        Console.WriteLine("Unknown command.");
    }
}

async Task CreateRequest()
{
    var request = new SupportRequest();

    request.Id = Random.Shared.Next(1000, 10000);

    Console.Write("User name: ");
    request.UserName = Console.ReadLine() ?? "";

    Console.Write("Topic: ");
    request.Topic = Console.ReadLine() ?? "";

    Console.Write("Description: ");
    request.Description = Console.ReadLine() ?? "";

    Console.Write("Priority (1 - Low, 2 - Medium, 3 - High): ");
    var priority = Console.ReadLine();

    if (priority == "3")
    {
        request.Priority = Priority.High;
    }
    else if (priority == "2")
    {
        request.Priority = Priority.Medium;
    }
    else
    {
        request.Priority = Priority.Low;
    }

    // час створення
    request.CreatedAt = DateTime.UtcNow;

    // в json і в чергу
    var json = JsonSerializer.Serialize(request);

    await sqs.SendMessageAsync(queueUrl, json);

    Console.WriteLine($"Request #{request.Id} added to queue.");
}

async Task ProcessRequests()
{
    var processed = 0;

    while (true)
    {
        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest()
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 5
        });

        // якщо черга пуста то приходить null
        if (response.Messages == null || response.Messages.Count == 0)
        {
            break;
        }

        foreach (var message in response.Messages)
        {
            var request = JsonSerializer.Deserialize<SupportRequest>(message.Body)!;
            
            var waited = DateTime.UtcNow - request.CreatedAt;       // скільки заявка пролежала в черзі

            Console.WriteLine($"Request #{request.Id}");

            if (request.Priority == Priority.High)
            {
                Console.WriteLine("Urgent request");
            }

            Console.WriteLine($"User: {request.UserName}");
            Console.WriteLine($"Topic: {request.Topic}");
            Console.WriteLine($"Description: {request.Description}");
            Console.WriteLine($"Priority: {request.Priority}");
            Console.WriteLine($"Waited in queue: {(int)waited.TotalMinutes} min {waited.Seconds} sec");
            Console.WriteLine();

            // видалення
            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle);
            processed++;
        }
    }

    if (processed == 0)
    {
        Console.WriteLine("Queue is empty.");
    }
    else
    {
        Console.WriteLine($"Processed requests: {processed}");
    }
}

async Task ShowCount()
{
    // число приблизне
    var response = await sqs.GetQueueAttributesAsync(queueUrl, new List<string> { "ApproximateNumberOfMessages" });

    Console.WriteLine($"Approximate number of requests in queue: {response.Attributes["ApproximateNumberOfMessages"]}");
}