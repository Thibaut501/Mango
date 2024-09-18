using Mango.Web.Models;
using System.Threading.Tasks;

namespace Mango.Web.Service.IService
{
    public interface IAuthService
    {
        Task<ResponseDto> RegisterAsync(RegistrationRequestDto obj);
        Task<ResponseDto> LoginAsync(LoginRequestDto obj);
        Task<ResponseDto> AssignRoleAsync(RegistrationRequestDto obj);
    }
}

