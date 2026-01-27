using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Vendor
{
    public class CreateVendorRequestDTO
    {
        public required string BusinessEmail { get; set; }
        public required string BusinessName { get; set; }
        public string? BusinessAddress { get; set; }
        public required string BusinessPhone { get; set; }
        public required string BusinessDescription { get; set; }
    }
}