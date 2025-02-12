using HlkHelpers;
using MFAServer.Data;
using MFAServer.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Imaging;
using System.Security.Claims;
using static QRCoder.Core.PayloadGenerator;

namespace MFAServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> logger;
    private readonly TwoFactorAuthLogic authLogic;
    private readonly UserManager<User> userManager;
    private readonly SignInManager<User> signInManager;
    private readonly RoleManager<Role> roleManager;
    private readonly IConfiguration config;
    private readonly ApplicationDbContext dbContext;

    public AuthController(ILogger<AuthController> logger, TwoFactorAuthLogic twoFactorAuth, UserManager<User> userManager,
            SignInManager<User> signInManager, RoleManager<Role> roleManager, IConfiguration config, ApplicationDbContext dbContext)
    {
        this.logger = logger;
        this.authLogic = twoFactorAuth;
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.roleManager = roleManager;
        this.config = config;
        this.dbContext = dbContext;
    }

    [HttpPost("users/create")]
    public async Task<IActionResult> Register(AddUserDto dto, CancellationToken cancellation)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(dto.Email);

            if (user is not null)
                return BadRequest(CallResult.Error("Account with this email already exists"));

            user = new()
            {
                PhoneNumber = dto.PhoneNumber.Trim(),
                Email = dto.Email.Trim(),
                FullName = dto.FullName.Trim(),
                CreatedAt = DateTime.UtcNow,
                UserName = dto.Email.Trim(),
            };

            var result = await userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(CallResult.Error(string.Join(" ", result.Errors.Select(x => $"{x.Code} : {x.Description}"))));

            // assign role
            //user = await userManager.FindByEmailAsync(user.Email);

            //var res = await userManager.AddToRoleAsync(user!, Constants.UserRole);

            //if (!res.Succeeded)
            //    return BadRequest(CallResult.Error(string.Join(" ", res.Errors.Select(x => $"{x.Code} : {x.Description}"))));

            // am auto enabling/starting 2FA step here, this could be done on a different api where/when user decide to enable it 
            // Generate a new secret key and save it in db
            var key = authLogic.GenerateSecretKey();
            user.SecretKey = Utils.EncryptWithAES(key);
            user.UpdatedAt = DateTime.UtcNow;
            dbContext.Update(user);
            await dbContext.SaveChangesAsync(cancellation);

            // Generate QR code URI and image
            var qrCodeUri = authLogic.GenerateQrCodeUri(key, user.Email);

            // Get QR code image in Base64
            var qrCodeImage = authLogic.GenerateQrCodeImage(qrCodeUri);

            // this is the idea returned data should u want to give users option to manually enter the details
            // but most system just use the QRCode
            var res = new OtpSetupResponse
            {
                SecretKey = key,
                QrCodeUri = qrCodeUri,
                QrCodeBase64 = qrCodeImage
            };


            return Ok(CallResult.Ok("Account created successfully, please scan QRCode to proceed", new { qrCodeImage }));

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{action} failed: {error}", nameof(Register), ex.GetFullErrorMessage());
            return BadRequest(CallResult.Error(ex));
        }

    }

    [HttpPost("users/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken cancellation)
    {

        try
        {
            dto.Email = dto.Email.Trim();
            dto.Password = dto.Password.Trim();

            var user = await userManager.FindByEmailAsync(dto.Email);

            if (user is null)
                return NotFound(CallResult.Error("Can't find account you are trying to access"));

            if (!user.IsActive)
                return BadRequest(CallResult.Error("account you are trying to access is desactivated"));

            var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, true);

            if (!result.Succeeded)
                return BadRequest(CallResult.Error("wrong user name or password"));

       
            return Ok(CallResult.Ok("Please enter code from authenticator app", new { dto.Email }));

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{action} failed: {error}", nameof(Login), ex.GetFullErrorMessage());
            return BadRequest(CallResult.Error(ex));
        }
    }


    [HttpPost("validate")]
    public async Task<IActionResult> ValidateOtp(OtpValidationDto dto, CancellationToken cancellation)
    {
        try
        {
            var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == dto.Email, cancellation);

            if (user is null)
                return NotFound(CallResult.Error("Can't find account you are trying to access"));

            if (string.IsNullOrEmpty(user.SecretKey))
                return BadRequest(CallResult.Error("MFA is not enabled for this account"));

            var key = Utils.DecryptWithAES(user.SecretKey);

            // Validate the provided OTP code
            var isValid = authLogic.ValidateOtpCode(key, dto.Code);

            if (!isValid)
                return BadRequest(CallResult.Error("invalid or expired code"));

            string secret = config.GetSection("AppSettings:Token").Value ?? "";
            string issuer = config["AppSettings:Issuer"] ?? "";
            string aud = config["AppSettings:Audience"] ?? "";

            var res = new
            {
                user = new { user.Id, user.FullName, user.PhoneNumber },
                token = Utils.GenerateJwtToken(secret, issuer, aud, [new(ClaimTypes.Role, Constants.UserRole)], DateTime.UtcNow.AddHours(1))
            };

            return Ok(CallResult.Ok("Login Successfully", res));

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{action} failed: {error}", nameof(ValidateOtp), ex.GetFullErrorMessage());
            return BadRequest(CallResult.Error(ex));
        }
       
    }

}
