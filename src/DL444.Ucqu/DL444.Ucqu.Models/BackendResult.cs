namespace DL444.Ucqu.Models
{
    public class BackendResult<T>
    {
        public BackendResult(bool success, [System.Diagnostics.CodeAnalysis.AllowNull] T resource, string? message)
        {
            Success = success;
            Resource = resource;
            Message = message;
        }
        public BackendResult([System.Diagnostics.CodeAnalysis.AllowNull] T resource)
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
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public T Resource { get; set; }
        public string? Message { get; set; }
    }
}
