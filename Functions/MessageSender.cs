using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MessageProvider.Models;
using Azure.Communication.Email;
using Azure;

namespace MessageProvider.Functions;

public class MessageSender(ILoggerFactory loggerFactory, EmailClient emailClient)
{
	private readonly ILogger _logger = loggerFactory.CreateLogger<MessageSender>();
	private readonly EmailClient _emailClient = emailClient;

	[Function("MessageSender")]
	public void Run([SqlTrigger("[dbo].[ClientContacts]", "NL-OnatrixDatabase")] IReadOnlyList<SqlChange<ClientContacts>> changes, FunctionContext context)
	{
		_logger.LogInformation("SQL Changes: " + JsonConvert.SerializeObject(changes));

		try
		{
			foreach (SqlChange<ClientContacts> change in changes)
			{
				if (change.Operation == 0 && !string.IsNullOrEmpty(change.Item.Email))
				{

					var newMessage = new MessageRequest
					{
						To = change.Item.Email,
						Subject = "Contact confirmation",
						HtmlBody = $@"
                    <!DOCTYPE html>
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                    </head>
                    <body style='font-family: Arial, sans-serif; padding: 10px; background-color: #f9f9f9;'>
                        <div style='max-width: 400px; margin: 0 auto; padding: 15px; background-color: #fff; border: 1px solid #ddd; border-radius: 5px;'>
                            <p style='font-size: 14px; color: #333;'>Dear {change.Item.UserName},</p>
                            <p style='font-size: 14px; color: #333;'>Thank you for contacting us. We will get back to you at <strong>{change.Item.Email}</strong> as soon as possible.</p>
                        </div>
                    </body>
                    </html>",
						PlainText = $"Thank you {change.Item.UserName} for contacting us. We will contact you at {change.Item.Email} as soon as possible."
					};

					if (newMessage != null)
					{
						var result = SendMessage(newMessage);
					}
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"ERROR :: MessageSender.Run() :: {ex.Message}");
		}
	}

	public bool SendMessage(MessageRequest messageRequest)
	{
		try
		{
			var result = _emailClient.Send(
				WaitUntil.Completed,

				senderAddress: Environment.GetEnvironmentVariable("senderAddress"),
				recipientAddress: messageRequest.To,
				subject: messageRequest.Subject,
				htmlContent: messageRequest.HtmlBody,
				plainTextContent: messageRequest.PlainText);
			
			if (result.HasCompleted) 
				return true;
		}
		catch (Exception ex)
		{
			_logger.LogError($"ERROR :: MessageSender.SendMessage() :: {ex.Message}");
		}
		return false;
	}
}
