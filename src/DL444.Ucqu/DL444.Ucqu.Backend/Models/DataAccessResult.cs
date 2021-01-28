namespace DL444.Ucqu.Backend.Models
{
    public struct DataAccessResult
    {
        public DataAccessResult(bool success, int statusCode)
        {
            Success = success;
            StatusCode = statusCode;
        }
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public static DataAccessResult Ok => new DataAccessResult(true, 200);
    }

    public struct DataAccessResult<T>
    {
        public DataAccessResult(bool success, [System.Diagnostics.CodeAnalysis.AllowNull] T resource, int statusCode)
        {
            Success = success;
            Resource = resource;
            StatusCode = statusCode;
        }

        public bool Success { get; set; }
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public T Resource { get; set; }
        public int StatusCode { get; set; }
        public static DataAccessResult<T> Ok(T resource) => new DataAccessResult<T>(true, resource, 200);
    }
}
