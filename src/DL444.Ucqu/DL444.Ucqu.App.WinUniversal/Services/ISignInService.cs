using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface ISignInService
    {
        Task<DataRequestResult<AccessToken>> SignInAsync(bool createAccount);
        Task WaitForUserInitializationAsync(string location, int pollInterval);
    }
}
