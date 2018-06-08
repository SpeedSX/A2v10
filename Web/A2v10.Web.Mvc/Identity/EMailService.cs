﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using A2v10.Infrastructure;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace A2v10.Web.Mvc.Identity
{
	class EmailService : IIdentityMessageService, IMessageService
	{
		private readonly ILogger _logger;
		public EmailService(ILogger logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		String GetJsonResult(String phase, String destination, String result = null, String message = null)
		{
			var r = new
			{
				sendmail = new
				{
					phase,
					destination,
					result,
					message
				}
			};

			return JsonConvert.SerializeObject(r, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});
		}

		public Task SendAsync(IdentityMessage message)
		{
			Send(message.Destination, message.Subject, message.Body);
			return Task.FromResult(0);
		}

		public void Send(String to, String subject, String body)
		{
			try
			{
				using (var client = new SmtpClient())
				{
					using (var mm = new MailMessage())
					{
						mm.To.Add(new MailAddress(to));
						mm.Subject = subject;
						mm.Body = body;
						mm.IsBodyHtml = true;
						mm.BodyEncoding = Encoding.UTF8;
						mm.SubjectEncoding = Encoding.UTF8;
						// sync variant. avoid exception loss
						_logger.LogMessaging(GetJsonResult("send", to));
						client.Send(mm);
						_logger.LogMessaging(GetJsonResult("result", to, "success"));
					}
				}
			}
			catch (Exception ex)
			{
				String msg = ex.Message;
				if (ex.InnerException != null)
					msg = ex.InnerException.Message;
				_logger.LogMessaging(new LogEntry(LogSeverity.Error, GetJsonResult("result", to, "exception", msg)));
				throw; // rethrow
			}
		}

	}
}
