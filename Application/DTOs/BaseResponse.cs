namespace Application.DTOs
{
    public record BaseResponse<T>(
        bool Success = false,
        string Message = "",
        T? Data = default
    );

    public record BaseResponse(
    bool Success = false,
    string Message = ""
);
}

        
