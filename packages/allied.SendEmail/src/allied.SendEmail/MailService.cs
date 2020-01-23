using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace allied.SendEmail
{
    class MailService
    {
        public virtual void SendEmail(string toAddress, string toName, string fromAddress, string fromName, string replyToName, string replyToAddress, string subject, string body, bool isHtml, List<Attachment> attachments)
        {
            fromAddress = "DoNotReply@AlliedPayment.com";

            if (string.IsNullOrWhiteSpace(toName)) toName = toAddress;
            if (string.IsNullOrWhiteSpace(toAddress)) toAddress = string.Empty;
            if (string.IsNullOrWhiteSpace(fromName)) fromName = "DoNotReply";
            if (string.IsNullOrWhiteSpace(replyToName)) replyToName = string.Empty;
            if (string.IsNullOrWhiteSpace(replyToAddress)) replyToAddress = string.Empty;

            //var log = log4net.LogManager.GetLogger(this.GetType());

            var regex = new Regex(@"(?<name>\S+)@(?<domain>\S+)");
            if (!regex.IsMatch(toAddress))
            {
                // log.InfoFormat("Tried to send email to {0}. It does not look like an email address.. aborting", toAddress);
                return;
            }
            if (!regex.IsMatch(fromAddress))
            {
                //log.InfoFormat("Tried to send email from {0}. It does not look like an email address.. aborting", fromAddress);
                return;
            }

            var message = new MailMessage();
            message.Subject = subject;
            message.Body = body;
            message.From = new MailAddress(fromAddress, fromName);
            message.IsBodyHtml = isHtml;
            var toAddresses = toAddress.Split(';');
            var toNames = toName.Split(';');

            for (int i = 0; i < toAddresses.Count(); i++)
            {
                if (i < toNames.Count())
                {
                    message.To.Add(new MailAddress(toAddresses[i], toNames[i]));
                }
                else
                {
                    message.To.Add(new MailAddress(toAddresses[i]));
                }
            }

            if (!string.IsNullOrEmpty(replyToAddress) && !string.IsNullOrEmpty(replyToName))
            {
                message.ReplyToList.Add(new MailAddress(replyToAddress, replyToName));
            }



            string server = null;// = Allied.Core.Configuration.AppSettings["email.server"];
            string username = null;// = Allied.Core.Configuration.AppSettings["email.username"];
            string password = null;// = Allied.Core.Configuration.AppSettings["email.password"];
            string portStr = null;// = Allied.Core.Configuration.AppSettings["email.port"];

            if (string.IsNullOrWhiteSpace(server)) server = "pod51010.outlook.com";
            if (string.IsNullOrWhiteSpace(username)) username = "DoNotReply@alliedpayment.com";
            if (string.IsNullOrWhiteSpace(password)) password = "mSbs!2012";
            if (string.IsNullOrWhiteSpace(portStr)) portStr = "587";

            var port = 587;
            if (!int.TryParse(portStr, out port))
            {
                port = 587;
            };

            var svr = new SmtpClient(server, port);

            svr.Credentials = new NetworkCredential(username, password);
            svr.Port = port;
            svr.EnableSsl = true;


            if (attachments != null && attachments.Any())
            {
                foreach (var a in attachments)
                {
                    message.Attachments.Add(a);
                }
            }
            svr.Send(message);
        }
    }
}
