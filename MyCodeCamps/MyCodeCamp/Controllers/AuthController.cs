﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Filters;
using MyCodeCamp.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MyCodeCamp.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private CampContext _context;
        private SignInManager<CampUser> _signInMananger;
        private UserManager<CampUser> _userManager;
        private IPasswordHasher<CampUser> _passwordHasher;
        private ILogger<AuthController> _logger;

        public AuthController(
            CampContext context, SignInManager<CampUser> signInManager, UserManager<CampUser> userManager, IPasswordHasher<CampUser> passwordHasher, ILogger<AuthController> logger)
        {
            _context = context;
            _signInMananger = signInManager;
            _userManager = userManager;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpPost("login")]
        [ValidateModel]
        public async Task<IActionResult> Login([FromBody] CredentialModel model)
        {
            try
            {
                var result = await _signInMananger.PasswordSignInAsync(model.UserName, model.Password, false, false);
                if (result.Succeeded)
                    return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while logging in: {ex}");
            }

            return BadRequest("Failed to login");
        }

        [HttpPost("token")]
        [ValidateModel]
        public async Task<IActionResult> CreateToken([FromBody] CredentialModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user != null)
                {
                    if (_passwordHasher.VerifyHashedPassword(user, model.Password, model.Password) == PasswordVerificationResult.Success)
                    {
                        var claims = new[]
                        {
                            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                        };

                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("VERYLONGKEYVALUETHATISSECURE"));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        var token = new JwtSecurityToken
                            (
                                issuer: "http://mycodecamp.org",
                                audience: "http://mycodecamp.org",
                                claims: claims,
                                expires: DateTime.UtcNow.AddMinutes(15),
                                signingCredentials: creds
                            );

                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(token),
                            expiration = token.ValidTo
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while creating JWT: {ex}");
            }

            return BadRequest("Failed to generate token");
        }
    }
}
