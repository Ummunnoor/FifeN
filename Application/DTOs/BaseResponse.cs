using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record BaseResponse<T>(
        bool Success = false,
        string Message = "",
        T? Data = default
    );
}

        
