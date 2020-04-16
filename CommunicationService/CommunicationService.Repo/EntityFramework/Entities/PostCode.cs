using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Repo.EntityFramework.Entities
{
    public class PostCode
    {
        public PostCode()
        {
            AddressDetails = new HashSet<AddressDetails>();
        }

        public int Id { get; set; }
        public string PostalCode { get; set; }

        public virtual ICollection<AddressDetails> AddressDetails { get; set; }
    }
}
