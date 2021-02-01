using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Windows.Security.Credentials;
using Windows.Storage;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class LocalCacheService : IDataService, ILocalCacheService, IDisposable
    {
        public LocalCacheService(IConfiguration config)
        {
            databaseName = config.GetValue<string>("LocalData:DatabaseName");
            string keyString = GetEncryptionKey();
            key = Convert.FromBase64String(keyString);
        }

        public DataSource DataSource => DataSource.LocalCache;

        public async Task<DataRequestResult<WellknownData>> GetWellknownDataAsync()
            => new DataRequestResult<WellknownData>(await GetRecordDataAsync<WellknownData>(RecordType.Wellknown), null);

        public async Task<DataRequestResult<StudentInfo>> GetStudentInfoAsync()
            => new DataRequestResult<StudentInfo>(await GetRecordDataAsync<StudentInfo>(RecordType.StudentInfo), null);

        public async Task<DataRequestResult<Schedule>> GetScheduleAsync()
            => new DataRequestResult<Schedule>(await GetRecordDataAsync<Schedule>(RecordType.Schedule), null);

        public async Task<DataRequestResult<ExamSchedule>> GetExamsAsync()
            => new DataRequestResult<ExamSchedule>(await GetRecordDataAsync<ExamSchedule>(RecordType.Exams), null);

        public async Task<DataRequestResult<ScoreSet>> GetScoreAsync(bool isSecondMajor)
            => new DataRequestResult<ScoreSet>(await GetRecordDataAsync<ScoreSet>(isSecondMajor ? RecordType.ScoreSecondMajor : RecordType.ScoreMajor), null);

        public async Task<DataRequestResult<object>> DeleteUserAsync()
        {
            await ConnectDatabaseAsync();
            SqliteCommand command = new SqliteCommand("DELETE FROM cachedData", connection);
            await command.ExecuteNonQueryAsync();
            return new DataRequestResult<object>();
        }

        public async Task<DataRequestResult<DeveloperMessage>> GetDeveloperMessagesAsync()
            => new DataRequestResult<DeveloperMessage>(await GetRecordDataAsync<DeveloperMessage>(RecordType.DeveloperMessages), null);

        public Task SetWellknownDataAsync(WellknownData data) => SetRecordDataAsync(RecordType.Wellknown, data);

        public Task SetStudentInfoAsync(StudentInfo info) => SetRecordDataAsync(RecordType.StudentInfo, info);

        public Task SetScheduleAsync(Schedule schedule) => SetRecordDataAsync(RecordType.Schedule, schedule);

        public Task SetExamsAsync(ExamSchedule exams) => SetRecordDataAsync(RecordType.Exams, exams);

        public Task SetScoreAsync(ScoreSet score) => SetRecordDataAsync(score.IsSecondMajor ? RecordType.ScoreSecondMajor : RecordType.ScoreMajor, score);

        public Task SetDeveloperMessagesAsync(DeveloperMessage messages) => SetRecordDataAsync(RecordType.DeveloperMessages, messages);

        public Task ClearCacheAsync() => DeleteUserAsync();

        private async Task ConnectDatabaseAsync()
        {
            if (connection == null)
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync(databaseName, CreationCollisionOption.OpenIfExists);
                string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, databaseName);
                lock (connectionCreationLock)
                {
                    if (connection == null)
                    {
                        connection = new SqliteConnection($"Filename={path}");
                        connection.Open();
                        SqliteCommand command = new SqliteCommand("CREATE TABLE IF NOT EXISTS cachedData (recordType INTEGER PRIMARY KEY, iv TEXT, data BLOB)", connection);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private async Task<T> GetRecordDataAsync<T>(RecordType type)
        {
            await ConnectDatabaseAsync();
            SqliteCommand command = new SqliteCommand("SELECT recordType, iv, data FROM cachedData WHERE recordType = @type", connection);
            command.Parameters.AddWithValue("@type", (int)type);
            using (SqliteDataReader reader = await command.ExecuteReaderAsync())
            {
                if (reader.Read() == false)
                {
                    throw new LocalCacheInexistException($"Local cache for type {type} does not exist.");
                }
                string iv = reader.GetString(1);
                Aes aes = Aes.Create();
                aes.Key = key;
                aes.IV = Convert.FromBase64String(iv);
                try
                {
                    using (Stream stream = reader.GetStream(2))
                    using (var cryptoStream = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var streamReader = new StreamReader(cryptoStream))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        return new JsonSerializer().Deserialize<T>(jsonReader);
                    }
                }
                catch (CryptographicException ex)
                {
                    throw new LocalCacheRequestFailedException($"Local cache for type {type} cannot be decrypted.", ex);
                }
                catch (JsonException ex)
                {
                    throw new LocalCacheRequestFailedException($"Local cache for type {type} cannot be deserialized.", ex);
                }
            }
        }

        private async Task SetRecordDataAsync<T>(RecordType type, T data)
        {
            await ConnectDatabaseAsync();
            Aes aes = Aes.Create();
            aes.Key = key;
            using (var inputStream = new MemoryStream())
            using (var cryptoStream = new CryptoStream(inputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                new JsonSerializer().Serialize(streamWriter, data);
                await streamWriter.FlushAsync();
                cryptoStream.FlushFinalBlock();
                SqliteCommand command = new SqliteCommand(
                    "INSERT INTO cachedData (recordType, iv, data) VALUES (@type, @iv, zeroblob(@length))" +
                    "ON CONFLICT (recordType) DO UPDATE SET iv = @iv, data = zeroblob(@length)", connection);
                command.Parameters.AddWithValue("@type", (int)type);
                command.Parameters.AddWithValue("@iv", Convert.ToBase64String(aes.IV));
                command.Parameters.AddWithValue("@length", inputStream.Length);
                var updatedRows = await command.ExecuteNonQueryAsync();
                if (updatedRows == 0)
                {
                    throw new LocalCacheRequestFailedException($"Failed to update cache for type {type}.");
                }

                using (var blob = new SqliteBlob(connection, "cachedData", "data", (long)type))
                {
                    inputStream.Seek(0, SeekOrigin.Begin);
                    await inputStream.CopyToAsync(blob);
                }
            }
        }

        private static string GetEncryptionKey()
        {
            PasswordVault vault = new PasswordVault();
            try
            {
                return vault.Retrieve("CacheKey", "Key").Password;
            }
            catch (Exception ex) when (ex.HResult == -2147023728)
            {
                return GenerateEncryptionKey(vault);
            }
        }

        private static string GenerateEncryptionKey(PasswordVault vault)
        {
            var aes = Aes.Create();
            string key = Convert.ToBase64String(aes.Key);
            vault.Add(new PasswordCredential("CacheKey", "Key", key));
            return key;
        }

        private SqliteConnection connection;
        private byte[] key;
        private string databaseName;
        private object connectionCreationLock = new object();

        private enum RecordType
        {
            Wellknown = 0,
            StudentInfo = 1,
            Schedule = 2,
            Exams = 3,
            ScoreMajor = 4,
            ScoreSecondMajor = 5,
            DeveloperMessages = 6
        }

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    connection.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
