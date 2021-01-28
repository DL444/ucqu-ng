namespace DL444.Ucqu.Models
{
    public class BackendResult<T>
    {
        public BackendResult() { }

        public BackendResult(bool success, T resource, string message)
        {
            Success = success;
            Resource = resource;
            Message = message;
        }
        public BackendResult(T resource)
        {
            Success = true;
            Resource = resource;
        }
        public BackendResult(string message)
        {
            Success = false;
            Message = message;
        }

        public bool Success { get; set; }
        public T Resource { get; set; }
        public string Message { get; set; }
    }
}
