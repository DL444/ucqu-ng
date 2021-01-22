namespace DL444.Ucqu.App.WinUniversal.Models
{
    internal struct DataRequestResult<T>
    {
        public DataRequestResult(T resource, string message)
        {
            Resource = resource;
            Message = message;
        }

        public T Resource { get; set; }
        public string Message { get; set; }
    }
}
