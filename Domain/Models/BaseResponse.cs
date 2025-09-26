namespace OrderService.Domain.Models
{
    public class BaseResponse<T>
    {
        public string Message { get; set; }
        public T Data { get; set; }
        public int ErrorCode { get; set; }

        public BaseResponse()
        {
            ErrorCode = 0;
            Message = "Operation successful";
        }

        public BaseResponse(T data) : this()
        {
            Data = data;
        }

        public BaseResponse(string message, int errorCode)
        {
            Message = message;
            ErrorCode = errorCode;
            Data = default(T);
        }

        public BaseResponse(string message, int errorCode, T data)
        {
            Message = message;
            ErrorCode = errorCode;
            Data = data;
        }

        public static BaseResponse<T> Success(T data, string message = "Operation successful")
        {
            return new BaseResponse<T>(data) { Message = message };
        }

        public static BaseResponse<T> Error(string message, int errorCode = 500)
        {
            return new BaseResponse<T>(message, errorCode);
        }
    }

    // For responses without data
    public class BaseResponse : BaseResponse<object>
    {
        public BaseResponse() : base() { }
        public BaseResponse(string message, int errorCode) : base(message, errorCode) { }

        public static BaseResponse Success(string message = "Operation successful")
        {
            return new BaseResponse { Message = message };
        }

        public static new BaseResponse Error(string message, int errorCode = 500)
        {
            return new BaseResponse(message, errorCode);
        }
    }
}