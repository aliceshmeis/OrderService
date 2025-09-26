namespace Domain.Entities
{
    public class StoredProcedureResult<T>
    {
        public int ErrorCode { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
    }
}