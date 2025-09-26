namespace InventoryService.Domain.Models
{
    public class BaseResponse
    {
        public string Message { get; set; } = string.Empty;
        public int ErrorCode { get; set; }

        public BaseResponse()
        {
            ErrorCode = 0;
            Message = "Operation successful";
        }

        public BaseResponse(string message, int errorCode = 0)
        {
            Message = message;
            ErrorCode = errorCode;
        }

        public static BaseResponse Success(string message = "Operation successful")
        {
            return new BaseResponse(message, 0);
        }

        public static BaseResponse Error(string message, int errorCode = 500)
        {
            return new BaseResponse(message, errorCode);
        }
    }

    public class BaseResponse<T> : BaseResponse
    {
        public T? Data { get; set; }

        public BaseResponse() : base() { }

        public BaseResponse(T data, string message = "Operation successful", int errorCode = 0)
            : base(message, errorCode)
        {
            Data = data;
        }

        public static BaseResponse<T> Success(T data, string message = "Operation successful")
        {
            return new BaseResponse<T>(data, message, 0);
        }

        public static new BaseResponse<T> Error(string message, int errorCode = 500)
        {
            return new BaseResponse<T>(default(T), message, errorCode);
        }
    }

    // Database response model for stored procedures
    public class StoredProcedureResult<T>
    {
        public int ErrorCode { get; set; }
        public T? Data { get; set; }
    }
}