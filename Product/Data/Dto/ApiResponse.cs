namespace PRODUCT.Data.Dto
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public ApiResponse(bool success, string message, int statusCode, T? data = default)
        {
            Success = success;
            Message = message;
            StatusCode = statusCode;
            Data = data;
        }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Operation successful", int statusCode = 200)
        {
            return new ApiResponse<T>(true, message, statusCode, data);
        }

        public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
        {
            return new ApiResponse<T>(false, message, statusCode, default!)
            {
                Errors = errors ?? new List<string>()
            };
        }
    }
}
