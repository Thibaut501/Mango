using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mango.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ITokenProvider _tokenProvider;

        public AuthController(IAuthService authService, ITokenProvider tokenProvider)

        {
            _authService = authService;
            _tokenProvider = tokenProvider;
        }

        [HttpGet]
        public IActionResult Login()
        {
            LoginRequestDto loginRequestDto = new LoginRequestDto();
            return View(loginRequestDto);

        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto obj)
        {
            ResponseDto? responseDto = await _authService.LoginAsync(obj);


            if (responseDto != null && responseDto.IsSuccess && responseDto.Result != null)
            {
                var loginResponseDto = JsonConvert.DeserializeObject<LoginResponseDto>(Convert.ToString(responseDto.Result));
                if (loginResponseDto == null || string.IsNullOrWhiteSpace(loginResponseDto.Token))
                {
                    TempData["error"] = "Invalid login response.";
                    return View(obj);
                }

                await SignInUser(loginResponseDto);

                _tokenProvider.SetToken(loginResponseDto.Token);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["error"] = responseDto?.Message ?? "Login failed.";
                return View(obj);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            var roleList = new List<SelectListItem>()
            {
                new SelectListItem{Text=SD.RoleAdmin,Value=SD.RoleAdmin},
                new SelectListItem{Text=SD.RoleCustomer,Value=SD.RoleCustomer},

            };


            ViewBag.RoleList = roleList;
            return View();

        }


        [HttpPost]
        public async Task<IActionResult> Register(RegistrationRequestDto obj)
        {
            ResponseDto? result = await _authService.RegisterAsync(obj);
            ResponseDto? assignRole;

            if (result != null && result.IsSuccess)

            {
                if (string.IsNullOrEmpty(obj.Role))
                {
                    obj.Role = SD.RoleCustomer;
                }
                assignRole = await _authService.AssignRoleAsync(obj);
                if (assignRole != null && assignRole.IsSuccess)

                {
                    TempData["success"] = "Registration Successful";
                    return RedirectToAction(nameof(Login));
                }
                else
                {
                    TempData["error"] = assignRole?.Message ?? "Failed to assign role.";
                }

            }
            else
            {
                TempData["error"] = result?.Message ?? "Registration failed.";
            }

            var roleList = new List<SelectListItem>()
            {
                new SelectListItem{Text=SD.RoleAdmin,Value=SD.RoleAdmin},
                new SelectListItem{Text=SD.RoleCustomer,Value=SD.RoleCustomer},

            };



            ViewBag.RoleList = roleList;
            return View(obj);

        }

        public async Task <ActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            _tokenProvider.ClearToken();
            return RedirectToAction("Index", "Home");

          
        }

        private async Task SignInUser(LoginResponseDto model)
        {
            var handler = new JwtSecurityTokenHandler();

            var jwt = handler.ReadJwtToken(model.Token);

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

            // Extract claims safely
            string? email = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Email)?.Value;
            string? subject = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value;
            string? name = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Name)?.Value;

            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, email));
                identity.AddClaim(new Claim(ClaimTypes.Name, email));
            }

            if (!string.IsNullOrEmpty(subject))
            {
                identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, subject));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
            }

            if (!string.IsNullOrEmpty(name))
            {
                identity.AddClaim(new Claim(JwtRegisteredClaimNames.Name, name));
            }

            // Role(s)
            var roleClaim = jwt.Claims.Where(u => u.Type == "role" || u.Type == ClaimTypes.Role);
            foreach (var rc in roleClaim)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, rc.Value));
            }

            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}