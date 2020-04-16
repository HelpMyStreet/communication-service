using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Repo.EntityFramework.Entities
{
    public class AddressDetails
    {
        public int Id { get; set; }
        public string HouseName { get; set; }
        public string HouseNumber { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public int PostCodeId { get; set; }

        public virtual PostCode PostCode { get; set; }
    }
}
