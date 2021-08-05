using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.EqualityComparers;
using HelpMyStreet.Utils.Models;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class NewRequestNotificationMessageTests
    {
        private Mock<IConnectGroupService> _groupService;
        private Mock<IConnectUserService> _userService;
        private Mock<IConnectRequestService> _requestService;
        private Mock<IConnectAddressService> _addressService;
        private Mock<ICosmosDbService> _cosmosService;
        private Mock<IOptions<EmailConfig>> _emailConfig;
        private EmailConfig _emailConfigSettings;

        private GetVolunteersByPostcodeAndActivityResponse _getVolunteersByPostcodeAndActivityResponse;
        private List<ShiftJob> _openShiftJobs;
        private Dictionary<Location, LocationDetails> _dictLocationResponse;
        private User _user;
        private GetUserGroupsResponse _getUserGroupsResponse;
        private List<ShiftJob> _shiftJobs;
        private List<int> _requestIds;
        private List<RequestHistory> _requestHistory;
        private IEqualityComparer<ShiftJob> _shiftJobDedupe_EqualityComparer;
        private GetAllJobsByFilterResponse _getAllJobsByFilterResponse;
        private double? _radius;

        private NewRequestNotificationMessage _classUnderTest;

        [SetUp]
        public void SetUp()
        {
            _shiftJobDedupe_EqualityComparer = new JobBasicDedupe_EqualityComparer();
            SetupGroupService();
            SetupUserService();
            SetupRequestService();
            SetupAddressService();
            SetupEmailConfig();
            SetupCosmosDbService();

            _classUnderTest = new NewRequestNotificationMessage(
                _requestService.Object,
                _addressService.Object,
                _userService.Object,
                _cosmosService.Object,
                _emailConfig.Object,
                _groupService.Object
                );
        }

        private void SetupGroupService()
        {
            _groupService = new Mock<IConnectGroupService>();

            _groupService.Setup(x => x.GetUserGroups(It.IsAny<int>()))
                .ReturnsAsync(() => _getUserGroupsResponse);

            _groupService.Setup(x => x.GetGroupSupportActivityRadius(It.IsAny<int>(), It.IsAny<SupportActivities>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _radius);
        }

        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();

            _userService.Setup(x => x.GetVolunteersByPostcodeAndActivity(
                It.IsAny<string>(),
                It.IsAny<List<SupportActivities>>(),
                It.IsAny<double?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _getVolunteersByPostcodeAndActivityResponse);

            _userService.Setup(x => x.GetUserByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(()=> _user);
        }

        private void SetupRequestService()
        {
            _requestService = new Mock<IConnectRequestService>();

            _openShiftJobs = new List<ShiftJob>();
            _openShiftJobs.Add(new ShiftJob()
            {
                RequestID = 1,
                JobID = 1,
                Location = Location.FranklinHallSpilsby,
                SupportActivity = SupportActivities.VaccineSupport,
                StartDate = DateTime.UtcNow,
                ShiftLength = 60,
            });

            _openShiftJobs.Add(new ShiftJob()
            {
                RequestID = 1,
                JobID = 2,
                Location = Location.FranklinHallSpilsby,
                SupportActivity = SupportActivities.VaccineSupport,
                StartDate = DateTime.UtcNow,
                ShiftLength = 60,
            });

            _openShiftJobs.Add(new ShiftJob()
            {
                RequestID = 2,
                JobID = 3,
                Location = Location.LincolnCountyHospital,
                SupportActivity = SupportActivities.VaccineSupport,
                StartDate = DateTime.UtcNow.AddDays(1),
                ShiftLength = 60,
            });

            _shiftJobs = new List<ShiftJob>()
            {
                new ShiftJob()
                {
                    RequestID = 1,
                    JobID = 1,
                    SupportActivity = SupportActivities.VaccineSupport,
                    StartDate = DateTime.UtcNow,
                    ShiftLength = 60,
                }
            };

            _requestService.Setup(x => x.GetUserShiftJobsByFilter(It.IsAny<GetUserShiftJobsByFilterRequest>()))
                .ReturnsAsync(() => _shiftJobs);

            _requestService.Setup(x => x.GetAllJobsByFilter(It.IsAny<GetAllJobsByFilterRequest>()))
                .ReturnsAsync(() => _getAllJobsByFilterResponse);
        }

        private void SetupAddressService()
        {
            _dictLocationResponse = new Dictionary<Location, LocationDetails>();
            _dictLocationResponse.Add(Location.FranklinHallSpilsby, new LocationDetails() { Location = Location.FranklinHallSpilsby, Name= "Franklin Hall Spilsby", Address = new Address() { Postcode = "PostCode1" } });
            _dictLocationResponse.Add(Location.LincolnCountyHospital, new LocationDetails() { Location = Location.LincolnCountyHospital, Name = "Lincoln County Hospital",  Address = new Address() { Postcode = "PostCode2" } });
            _addressService = new Mock<IConnectAddressService>();
            _addressService.Setup(x => x.GetLocationDetails(It.IsAny<Location>(),It.IsAny<CancellationToken>()))
                .ReturnsAsync((Location l, CancellationToken ct) => _dictLocationResponse[l]);

        }

        private void SetupEmailConfig()
        {
            _emailConfigSettings = new EmailConfig
            {
                ShowUserIDInEmailTitle = true,
                RegistrationChaserMaxTimeInHours = 2,
                RegistrationChaserMinTimeInMinutes = 30,
                ServiceBusSleepInMilliseconds = 1000
            };

            _emailConfig = new Mock<IOptions<EmailConfig>>();
            _emailConfig.SetupGet(x => x.Value).Returns(_emailConfigSettings);
        }

        private void SetupCosmosDbService()
        {
            _cosmosService = new Mock<ICosmosDbService>();
            _requestIds = new List<int>();
            _cosmosService.Setup(x => x.GetShiftRequestDetailsSent(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(() => _requestIds);

            _requestHistory = new List<RequestHistory>();
            _cosmosService.Setup(x => x.GetAllUserShiftDetailsHaveBeenSentTo(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(() => _requestHistory);
        }

        [Test]
        public async Task IdentifyRecipientsReturnsCorrectUsers()
        {
            int? recipientUserId = null;
            int? jobId = null;
            int? groupId = null;
            int? requestId = null;

            List<VolunteerSummary> volunteerSummaries = new List<VolunteerSummary>();
            Random random = new Random();

            for (int i = 1; i < 50; i++)
            {
                volunteerSummaries.Add(new VolunteerSummary()
                {
                    DistanceInMiles = random.NextDouble(),
                    UserID = i
                });
            }

            _getVolunteersByPostcodeAndActivityResponse = new GetVolunteersByPostcodeAndActivityResponse()
            {
                Volunteers = volunteerSummaries
            };

            _getAllJobsByFilterResponse = new GetAllJobsByFilterResponse()
            {
                ShiftJobs = new List<ShiftJob>()
                {
                    new ShiftJob()
                    {
                        RequestID = 5,
                        JobID = 5,
                        Location = Location.FranklinHallSpilsby,
                        SupportActivity = SupportActivities.VaccineSupport,
                        StartDate = DateTime.UtcNow,
                        ShiftLength = 60,
                        ReferringGroupID = -1
                    }
                }
            };
            _radius = 20d;

            List<Core.Domains.SendMessageRequest> result = await _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId, requestId, null);
            Assert.AreEqual(_getVolunteersByPostcodeAndActivityResponse.Volunteers.Count(), result.Count());
        }

        [Test]
        public async Task CheckTemplateData()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = null;
            int? requestId = null;
            Dictionary<string, string> additionalParameters = new Dictionary<string, string>();
            additionalParameters.Add("locations", "-9,-8");
            string templateName = string.Empty;

            _getUserGroupsResponse = new GetUserGroupsResponse()
            {
                Groups = new List<int>()
                {
                    -1,-2,-3
                }
            };

            _user = new User()
            {
                ID = 1,
                UserPersonalDetails = new UserPersonalDetails()
                {
                    FirstName = "John",
                    LastName = "Test",
                    EmailAddress = "john@test.com"
                },
                PostalCode = "NG1 6DQ",
                SupportActivities = new List<SupportActivities>()
                { SupportActivities.VaccineSupport}
            };

            _shiftJobs = new List<ShiftJob>()
            {
                new ShiftJob()
                {
                    JobID =1,
                    RequestID = 1,
                    SupportActivity = SupportActivities.VaccineSupport
                },
                new ShiftJob()
                {
                    JobID =2,
                    RequestID = 1,
                    SupportActivity = SupportActivities.VaccineSupport
                }
            };

            _getAllJobsByFilterResponse = new GetAllJobsByFilterResponse()
            {
                ShiftJobs = new List<ShiftJob>()
                {
                    new ShiftJob()
                    {
                        RequestID = 5,
                        JobID = 5,
                        Location = Location.FranklinHallSpilsby,
                        SupportActivity = SupportActivities.VaccineSupport,
                        StartDate = DateTime.UtcNow,
                        ShiftLength = 60,
                        ReferringGroupID = -1
                    }
                }
            };
            _radius = 20d;

            Core.Domains.EmailBuildData result = await _classUnderTest.PrepareTemplateData(
                Guid.NewGuid(), 
                recipientUserId, 
                jobId, 
                groupId, 
                requestId, 
                additionalParameters, 
                templateName);

            var dedupedShifts = _openShiftJobs.Distinct(_shiftJobDedupe_EqualityComparer);
            var notMyShifts = dedupedShifts.Where(s => !_shiftJobs.Contains(s, _shiftJobDedupe_EqualityComparer)).ToList();
        }
    }
}
