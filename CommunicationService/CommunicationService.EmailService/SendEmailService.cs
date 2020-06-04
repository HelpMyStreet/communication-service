using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Utils.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CommunicationService.EmailService
{
    public class SendEmailService : ISendEmailService
    {
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        private readonly IConnectUserService _connectUserService;

        public SendEmailService(IOptions<SendGridConfig> sendGridConfig, IConnectUserService connectUserService)
        {
            _sendGridConfig = sendGridConfig;
            _connectUserService = connectUserService;
        }

        public async Task<bool> SendEmailToUser(SendEmailToUserRequest sendEmailToUserRequest)
        {
            List<User> users = _connectUserService.PostUsersForListOfUserID(new List<int>()
            {
                sendEmailToUserRequest.ToUserID
            }).Result;

            if (users != null)
            {
                return await SendEmail(
                        users[0].UserPersonalDetails.EmailAddress,
                        users[0].UserPersonalDetails.FirstName,
                        sendEmailToUserRequest.Subject,
                        sendEmailToUserRequest.BodyText,
                        sendEmailToUserRequest.BodyHTML
                        );
            }
            else
            {
                return false;
            }

        }

        public async Task<bool> SendEmailToUsers(SendEmailToUsersRequest sendEmailToUsersRequest)
        {
            List<int> UserIds = new List<int>();

            if(sendEmailToUsersRequest.Recipients.ToUserIDs?.Count()>0)
                UserIds.AddRange(sendEmailToUsersRequest.Recipients.ToUserIDs);

            if (sendEmailToUsersRequest.Recipients.CCUserIDs?.Count() > 0)
                UserIds.AddRange(sendEmailToUsersRequest.Recipients.CCUserIDs);

            if (sendEmailToUsersRequest.Recipients.BCCUserIDs?.Count() > 0)
                UserIds.AddRange(sendEmailToUsersRequest.Recipients.BCCUserIDs);


            var DistinctUsers = UserIds.Distinct().ToList();


            List<User> Users = _connectUserService
                .PostUsersForListOfUserID(DistinctUsers)
                .Result;

            return await SendEmail(sendEmailToUsersRequest, Users);  
        }

        public async Task<bool> SendDynamicEmail(string templateId,SendGridData sendGridData)
        {
            var apiKey = _sendGridConfig.Value.ApiKey;
            if (apiKey == string.Empty)
            {
                throw new Exception("SendGrid Api Key missing.");
            }
            sendGridData.EmailToAddress = "jawwad.mukhtar@gmail.com";
            var client = new SendGridClient(apiKey);
            Personalization personalization = new Personalization()
            {
                Tos = new List<EmailAddress>() { new EmailAddress(sendGridData.EmailToAddress,sendGridData.EmailToName) },
                TemplateData = sendGridData.BaseDynamicData
            };

            var eml = new SendGridMessage()
            {
                From = new EmailAddress(_sendGridConfig.Value.FromEmail, _sendGridConfig.Value.FromName),
                TemplateId = templateId,
                Personalizations = new List<Personalization>()
                {
                    personalization
                }
            };

            Response response = await client.SendEmailAsync(eml);
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                return true;
            else
                return false;
        }

        private async Task<bool> SendEmail(SendEmailToUsersRequest sendEmailToUsersRequest, List<User> Users)
        {            
            var apiKey = _sendGridConfig.Value.ApiKey;
            if (apiKey == string.Empty)
            {
                throw new Exception("SendGrid Api Key missing.");
            }

            var client = new SendGridClient(apiKey);
            var eml = new SendGridMessage()
            {
                From = new EmailAddress(_sendGridConfig.Value.FromEmail, _sendGridConfig.Value.FromName),
                Subject = sendEmailToUsersRequest.Subject,
                PlainTextContent = sendEmailToUsersRequest.BodyText,
                HtmlContent = sendEmailToUsersRequest.BodyHTML
            };

            if (sendEmailToUsersRequest.Recipients.ToUserIDs?.Count() > 0)
            {
                foreach (int userId in sendEmailToUsersRequest.Recipients.ToUserIDs)
                {
                    var User = Users.Where(w => w.ID == userId).FirstOrDefault();
                    if (User != null && User.UserPersonalDetails != null)
                    {
                        eml.AddTo(new EmailAddress(User.UserPersonalDetails.EmailAddress, User.UserPersonalDetails.FirstName + " " + User.UserPersonalDetails.LastName));
                    }
                }
            }

            if (sendEmailToUsersRequest.Recipients.CCUserIDs?.Count() > 0)
            {
                foreach (int userId in sendEmailToUsersRequest.Recipients.CCUserIDs)
                {
                    var User = Users.Where(w => w.ID == userId).FirstOrDefault();
                    if (User != null && User.UserPersonalDetails != null)
                    {
                        eml.AddCc(new EmailAddress(User.UserPersonalDetails.EmailAddress, User.UserPersonalDetails.FirstName + " " + User.UserPersonalDetails.LastName));
                    }
                }
            }

            if (sendEmailToUsersRequest.Recipients.BCCUserIDs?.Count() > 0)
            {
                foreach (int userId in sendEmailToUsersRequest.Recipients.BCCUserIDs)
                {
                    var User = Users.Where(w => w.ID == userId).FirstOrDefault();
                    if (User != null && User.UserPersonalDetails != null)
                    {
                        eml.AddBcc(new EmailAddress(User.UserPersonalDetails.EmailAddress, User.UserPersonalDetails.FirstName + " " + User.UserPersonalDetails.LastName));
                    }
                }
            }

            Response response = await client.SendEmailAsync(eml);
            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
                return true;
            else
                return false;
        }


        private async Task<bool> SendEmail(string toAddress, string toName, string subject, string bodyText, string bodyHtml)
        {            
            var apiKey = _sendGridConfig.Value.ApiKey;
            if (apiKey == string.Empty)
            {
                throw new Exception("SendGrid Api Key missing.");
            }

            var client = new SendGridClient(apiKey);
            var eml = new SendGridMessage()
            {
                From = new EmailAddress(_sendGridConfig.Value.FromEmail, _sendGridConfig.Value.FromName),
                Subject = subject,
                PlainTextContent = bodyText,
                HtmlContent = bodyHtml
            };
            eml.AddTo(new EmailAddress(toAddress, toName));
            Response response = await client.SendEmailAsync(eml);
            
            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
                return true;
            else
                return false;
        }


        public async Task<bool> SendEmail(SendEmailRequest sendEmailRequest)
        {
            return await SendEmail(sendEmailRequest.ToAddress, sendEmailRequest.ToName, sendEmailRequest.Subject, sendEmailRequest.BodyText, sendEmailRequest.BodyHTML);            
        }
    }
}
