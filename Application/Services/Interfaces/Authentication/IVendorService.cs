using Application.DTOs;
using Application.DTOs.Vendor;

namespace Application.Services.Interfaces.Authentication
{
    public interface IVendorService
    {
        Task<BaseResponse<Guid>> CreateVendorRequestAsync(CreateVendorRequestDTO RequestDTO, string userEmail);
        Task<BaseResponse<bool>> ApproveVendorRequestAsync(Guid requestId);
        Task<BaseResponse<bool>> RejectVendorRequestAsync(Guid requestId, string reason);
    }
}