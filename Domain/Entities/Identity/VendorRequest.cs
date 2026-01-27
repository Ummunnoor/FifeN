using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Enums;

namespace Domain.Entities.Identity
{
    public class VendorRequest
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public required string BusinessEmail { get; set; } 
        public required string BusinessName { get; set; } 
        public required string Description { get; set; }
        public VendorRequestStatus Status { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ReviewedAt { get; set; }
    }
}