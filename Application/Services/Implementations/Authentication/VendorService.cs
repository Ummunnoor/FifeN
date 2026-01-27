using Application.DTOs;
using Application.DTOs.Vendor;
using Application.Services.Interfaces.Authentication;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using Domain.Interfaces.Vendor;

namespace Application.Services.Implementations.Authentication
{
    public class VendorService : IVendorService
    {
        private readonly IUserManagement _userManagement;
        private readonly IRoleManagement _roleManagement;
        private readonly IVendorRequestManagement _vendorRequestManagement;
        public VendorService(IUserManagement userManagement, IRoleManagement roleManagement, IVendorRequestManagement vendorRequestManagement)
        {
            _userManagement = userManagement;
            _roleManagement = roleManagement;
            _vendorRequestManagement = vendorRequestManagement;
        }
        public async Task<BaseResponse<bool>> ApproveVendorRequestAsync(Guid requestId)
        {
            var request = await _vendorRequestManagement.GetByIdAsync(requestId);
            if (request == null)
            {
                return new BaseResponse<bool>(
                    Success: false,
                    Message: "Vendor request not found"
                    );
            }
            if (request.Status != VendorRequestStatus.Pending)
            {
                return new BaseResponse<bool>(
                    Success: false,
                    Message: "Request already processed"
                    );
            }
            var user = await _userManagement.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return new BaseResponse<bool>(
                    Success: false,
                    Message: "User not found"
                    );
            }
            await _roleManagement.AssignRoleAsync(user, AppRole.Vendor);
            request.Status = VendorRequestStatus.Approved;
            await _vendorRequestManagement.UpdateAsync(request);
            return new BaseResponse<bool>(
                Success: true,
                Message: "Vendor request approved successfully"
                );
        }

        public async Task<BaseResponse<Guid>> CreateVendorRequestAsync(CreateVendorRequestDTO RequestDTO, string userEmail)
        {
            var user =  await _userManagement.GetUserByEmailAsync(userEmail);
            if (user == null)
            {
                return new BaseResponse<Guid>(
                    Success:false, 
                    Message: "User not found"
                    );
            }
            var hasPending = await _vendorRequestManagement.HasPendingRequestAsync(user.Id);
            if (hasPending)
            {
                return new BaseResponse<Guid>(
                    Success:false, 
                    Message: "You already have a pending vendor request"
                    );
            }
            var request = new VendorRequest
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BusinessName = RequestDTO.BusinessName,
                Description = RequestDTO.BusinessDescription,
                BusinessEmail = RequestDTO.BusinessEmail,
                Status = VendorRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _vendorRequestManagement.AddAsync(request);
            return new BaseResponse<Guid>(
                Success: true,
                Message: "Vendor request created successfully",
                Data: request.Id
            );
        }

        public async Task<BaseResponse<bool>> RejectVendorRequestAsync(Guid requestId, string reason)
        {
            var vendorRequest = await _vendorRequestManagement.GetByIdAsync(requestId);
            if (vendorRequest == null)
            {
                return new BaseResponse<bool>(
                    Success: false,
                    Message: "Vendor request not found"
                    );
            }
            if (vendorRequest.Status != VendorRequestStatus.Pending)
            {
                return new BaseResponse<bool>(
                    Success: false,
                    Message: "Request already processed"
                    );
            }
            vendorRequest.Status = VendorRequestStatus.Rejected;
            vendorRequest.ReviewedAt = DateTime.UtcNow;
            await _vendorRequestManagement.UpdateAsync(vendorRequest);
            return new BaseResponse<bool>(
                Success: true,
                Message: "Vendor request rejected successfully"
                );
        }
    }
}