﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using NTechAuth.Database;
using OtpNet;
using QRCoder;

namespace NTechAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(ApplicationDbContext context, IWebHostEnvironment environment) : Controller
    {
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpGet("GenerateQRCode")]
        public async Task<ActionResult> GenerateQRCode()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.IsMfaEnabled)
            {
                return Unauthorized("MFA has already been enabled on this account.");
            }

            var secretKey = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretKey);

            user.MfaSecretKey = base32Secret;
            await context.SaveChangesAsync();

            // Generer TOTP URI
            var totpUri = $"otpauth://totp/{user.Email}?secret={base32Secret}&issuer=MyApp";

            // Generer QR-koden ved hjælp af QRCoder
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q))
            {
                QRCode qrCode = new QRCode(qrCodeData);
                using (var qrCodeImage = qrCode.GetGraphic(20))
                {
                    using (var ms = new MemoryStream())
                    {
                        qrCodeImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Seek(0, SeekOrigin.Begin);

                        return File(ms.ToArray(), "image/png");
                    }
                }
            }
        }

        [HttpPost("avatar")]
        [Authorize]
        public async Task<ActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Upload en gyldig fil.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var rootPath = Path.Combine(environment.ContentRootPath);
            var filePath = Path.Combine(rootPath, "files", "avatars", $"{userId}.png");

            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { filePath });
        }

        [HttpGet("avatar")]
        public async Task<ActionResult> GetAvatar([FromQuery] string? userId)
        {
            Console.WriteLine(userId ?? "null");
            var rootPath = Path.Combine(environment.ContentRootPath);

            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return NotFound("User has no avatar");
                }
            }

            var avatarPath = Path.Combine(rootPath, "files", "avatars", $"{userId}.png");

            if (System.IO.File.Exists(avatarPath))
            {
                return PhysicalFile(avatarPath, "image/png");
            }

            return NotFound("User has no avatar");
        }
    }
}
