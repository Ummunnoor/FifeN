using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Identity;

namespace Domain.Interfaces.Vendor
{
    public interface IVendorRequestManagement
    {
        Task AddAsync(VendorRequest request);
        Task<VendorRequest?> GetByIdAsync(Guid id);
        Task<VendorRequest?> GetPendingByUserIdAsync(string userId);
        Task<List<VendorRequest>> GetPendingRequestsAsync();
        Task UpdateAsync(VendorRequest request);
        Task<bool> HasPendingRequestAsync(string userId);
    }
}