using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DL444.Ucqu.Backend.Services
{
    public interface ITokenService
    {
        string CreateToken(string username);
        string? ReadToken(string token);
    }

    internal class TokenService : ITokenService
    {
        public TokenService(IConfiguration config)
        {
            var signingKey = config.GetValue<string>("Token:SigningKey");
            var issuer = config.GetValue<string>("Token:Issuer");
            this.tokenValidMinutes = config.GetValue<int>("Token:ValidMinutes", 60);
            var key = new SymmetricSecurityKey(Convert.FromBase64String(signingKey));
            signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            this.issuer = issuer;
        }

        public string CreateToken(string username)
        {
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, username) },
                expires: DateTime.Now.AddMinutes(tokenValidMinutes),
                signingCredentials: signingCredentials
            );
            return handler.WriteToken(token);
        }

        public string? ReadToken(string token)
        {
            if (token == null)
            {
                return null;
            }
            var validateParams = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = issuer,
                IssuerSigningKey = signingCredentials.Key
            };
            try
            {
                _ = handler.ValidateToken(token, validateParams, out var validatedToken);
                return validatedToken is JwtSecurityToken jwtToken ? jwtToken.Subject : null;
            }
            catch (SecurityTokenValidationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private SigningCredentials signingCredentials;
        private string issuer;
        private int tokenValidMinutes;
        private JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
    }
}
