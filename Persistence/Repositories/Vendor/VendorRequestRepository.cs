using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Interfaces.Vendor;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories.Vendor
{
    public class VendorRequestRepository : IVendorRequestManagement
    {
        private readonly FifeNDbContext _context;
        public VendorRequestRepository(FifeNDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(VendorRequest request)
        {
            await _context.VendorRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task<VendorRequest?> GetByIdAsync(Guid id)
        {
            return await _context.VendorRequests.FirstOrDefaultAsync(vr => vr.Id == id);
        }

        public async Task<VendorRequest?> GetPendingByUserIdAsync(string userId)
        {
            return await _context.VendorRequests
            .FirstOrDefaultAsync(vr => vr.UserId == userId && vr.Status == VendorRequestStatus.Pending);
        }

        public async Task<List<VendorRequest>> GetPendingRequestsAsync()
        {
            return await _context.VendorRequests
            .Where(vr => vr.Status == VendorRequestStatus.Pending)
            .OrderBy(vr => vr.CreatedAt)
            .ToListAsync();
        }

        public async Task<bool> HasPendingRequestAsync(string userId)
        {
            return await _context.VendorRequests
            .AnyAsync(vr => vr.UserId == userId && vr.Status == VendorRequestStatus.Pending);
        }

        public async Task UpdateAsync(VendorRequest request)
        {
            _context.VendorRequests.Update(request);
            await _context.SaveChangesAsync();
        }
    }


}