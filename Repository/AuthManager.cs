using AutoMapper;
using HotelListing.API.Contracts;
using HotelListing.API.Data;
using HotelListing.API.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace HotelListing.API.Repository
{
    public class AuthManager : IAuthManager
    {
        private readonly IMapper _mapper;
        private readonly UserManager<ApiUser> _userManager;
        private readonly IConfiguration _configuration;
        private ApiUser _apiUser;

        private const string _loginProvider = "HotelListingApi";
        private const string _refreshToken = "RefreshToken";

        public AuthManager(IMapper mapper, UserManager<ApiUser> userManager, IConfiguration configuration)
        {
            this._mapper = mapper;
            this._userManager = userManager;
            this._configuration = configuration;
        }

        public async Task<AuthResponseDto> Login(LoginDto loginDto)
        {
            _apiUser = await _userManager.FindByEmailAsync(loginDto.Email);
            var validPassword = await _userManager.CheckPasswordAsync(_apiUser, loginDto.Password);

            if (_apiUser == null || validPassword == false)
            {
                return null;
            }

            var token = await GenerateToken();

            return new AuthResponseDto
            {
                UserId = _apiUser.Id,
                Token = token,
                RefreshToken = await CreateRefreshToken()
            };
        }

        public async Task<IEnumerable<IdentityError>> Register(ApiUserDto userDto)
        {
            _apiUser = _mapper.Map<ApiUser>(userDto);
            _apiUser.UserName = userDto.Email;

            var result = await _userManager.CreateAsync(_apiUser, userDto.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(_apiUser, "User");
            }

            return result.Errors;
        }

        private async Task<string> GenerateToken()
        {
            // Generate data for token.
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var roles = await _userManager.GetRolesAsync(_apiUser);
            var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
            var userClaims = await _userManager.GetClaimsAsync(_apiUser);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, _apiUser.Email), // To whom is issued.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Prevent playback
                new Claim(JwtRegisteredClaimNames.Email, _apiUser.Email),
                new Claim("uid", _apiUser.Id)
            }.Union(userClaims).Union(roleClaims);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToInt32(_configuration["JwtSettings:DurationInMinutes"])),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> CreateRefreshToken()
        {
            await _userManager.RemoveAuthenticationTokenAsync(_apiUser, _loginProvider, _refreshToken);
            var newToken = await _userManager.GenerateUserTokenAsync(_apiUser, _loginProvider, _refreshToken);
            var result = await _userManager.SetAuthenticationTokenAsync(_apiUser, _loginProvider, _refreshToken, newToken);

            return newToken;
        }

        public async Task<AuthResponseDto> VerifyRefreshToken(AuthResponseDto request)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(request.Token);
            var username = tokenContent.Claims.ToList().FirstOrDefault(claim =>
                claim.Type == JwtRegisteredClaimNames.Email
            )?.Value;

            _apiUser = await _userManager.FindByEmailAsync(username);

            if (_apiUser == null || _apiUser.Id != request.UserId)
            {
                return null;
            }

            var isValidRefreshedToken = await _userManager.VerifyUserTokenAsync(_apiUser, _loginProvider, _refreshToken, request.Token);
            if (isValidRefreshedToken)
            {
                var token = await GenerateToken();
                return new AuthResponseDto
                {
                    Token = token,
                    UserId = _apiUser.Id,
                    RefreshToken = await CreateRefreshToken()
                };
            }

            await _userManager.UpdateSecurityStampAsync(_apiUser);
            return null;
        }
    }
}
