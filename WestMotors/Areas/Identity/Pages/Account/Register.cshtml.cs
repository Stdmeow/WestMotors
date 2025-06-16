using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // ��� FirstOrDefaultAsync

// ���������, ��� ��� ���������� ������������ ���� ��� ����� �������
using WestMotorsApp.Models; // ��� ApplicationUser � Client
using WestMotorsApp.Data;   // ��� ApplicationDbContext

namespace WestMotors.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender; // ��� �������� ����� �������������
        private readonly RoleManager<IdentityRole> _roleManager; // ��� ������ � ������
        private readonly ApplicationDbContext _dbContext;       // ��� ������ � ����� ������ � Client

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        /// <summary>
        /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        /// <summary>
        /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>
        /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        /// <summary>
        /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            /// directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Email ����������")]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            /// <summary>
            /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            /// directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "������ ����������")]
            [StringLength(100, ErrorMessage = "���� {0} ������ ���� �� ����� {2} � �� ����� {1} ��������.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "������")]
            public string Password { get; set; } = string.Empty;

            /// <summary>
            /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            /// directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "������������� ������")]
            [Compare("Password", ErrorMessage = "������ � ��� ������������� �� ���������.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            // *** ����� ���� ��� ����������� ������� ***
            [Required(ErrorMessage = "��� ������� �����������")]
            [StringLength(100)]
            [Display(Name = "��� �������")]
            public string ClientFullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "���������� ������ �����������")]
            [StringLength(200)]
            [Display(Name = "���������� ������")]
            public string ClientPassportData { get; set; } = string.Empty;

            [Required(ErrorMessage = "���������� ������ �����������")]
            [StringLength(200)]
            [Display(Name = "���������� ������ (�������, email)")]
            public string ClientContactInfo { get; set; } = string.Empty;

            [Display(Name = "������������ �������")]
            public string? ClientPreferences { get; set; }
            // ***************************************
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // *** ������ ��� ������ ��� �������� ������� ***
                var client = await _dbContext.Clients
                                             .FirstOrDefaultAsync(c => c.FullName == Input.ClientFullName &&
                                                                       c.PassportData == Input.ClientPassportData);

                if (client == null)
                {
                    // ������ �� ������, ������� ������
                    client = new Client
                    {
                        FullName = Input.ClientFullName,
                        PassportData = Input.ClientPassportData,
                        ContactInfo = Input.ClientContactInfo,
                        Preferences = Input.ClientPreferences
                    };
                    _dbContext.Clients.Add(client);
                    await _dbContext.SaveChangesAsync(); // ��������� ������ �������, ����� �������� ��� Id
                }
                else
                {
                    // �����������: ��������� ������ ������������� �������, ���� ��� ����� ����������
                    // client.ContactInfo = Input.ClientContactInfo;
                    // client.Preferences = Input.ClientPreferences;
                    // _dbContext.Clients.Update(client);
                    // await _dbContext.SaveChangesAsync();
                }

                user.ClientId = client.Id; // ����������� ID ���������� ��� ���������� ������� ������������
                // ***********************************************

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("������������ ������ ����� ������� ������ � �������.");

                    // *** ������ ���������� ���� "������" �� ��������� ***
                    // 1. ��������, ��� ���� "������" ����������. ���� ���, �������� ��.
                    if (!await _roleManager.RoleExistsAsync("������"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("������"));
                        _logger.LogInformation("������� ����� ����: '������'.");
                    }
                    // 2. ��������, ��� ���� "�������������" ���������� (���� ��� ������������).
                    if (!await _roleManager.RoleExistsAsync("�������������"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("�������������"));
                        _logger.LogInformation("������� ����� ����: '�������������'.");
                    }
                    // 3. ��������, ��� ���� "��������" ���������� (���� ��� ������������).
                    if (!await _roleManager.RoleExistsAsync("��������"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("��������"));
                        _logger.LogInformation("������� ����� ����: '��������'.");
                    }

                    // ��������� ���� "������" ������ ������������.
                    await _userManager.AddToRoleAsync(user, "������");
                    _logger.LogInformation($"������������ {user.Email} ��������� ���� '������'.");
                    // ***************************************************

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "����������� ���� email",
                        $"����������, ����������� ���� ������� ������, <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>����� �����</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}