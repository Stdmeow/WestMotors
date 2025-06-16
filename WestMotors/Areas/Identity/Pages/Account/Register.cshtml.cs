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
using Microsoft.EntityFrameworkCore; // Для FirstOrDefaultAsync

// Убедитесь, что это правильные пространства имен для ваших моделей
using WestMotorsApp.Models; // Для ApplicationUser и Client
using WestMotorsApp.Data;   // Для ApplicationDbContext

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
        private readonly IEmailSender _emailSender; // Для отправки писем подтверждения
        private readonly RoleManager<IdentityRole> _roleManager; // Для работы с ролями
        private readonly ApplicationDbContext _dbContext;       // Для работы с базой данных и Client

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
            [Required(ErrorMessage = "Email обязателен")]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            /// <summary>
            /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            /// directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Пароль обязателен")]
            [StringLength(100, ErrorMessage = "Поле {0} должно быть не менее {2} и не более {1} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            /// <summary>
            /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            /// directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Подтверждение пароля")]
            [Compare("Password", ErrorMessage = "Пароль и его подтверждение не совпадают.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            // *** НОВЫЕ ПОЛЯ ДЛЯ РЕГИСТРАЦИИ КЛИЕНТА ***
            [Required(ErrorMessage = "ФИО клиента обязательно")]
            [StringLength(100)]
            [Display(Name = "ФИО клиента")]
            public string ClientFullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Паспортные данные обязательны")]
            [StringLength(200)]
            [Display(Name = "Паспортные данные")]
            public string ClientPassportData { get; set; } = string.Empty;

            [Required(ErrorMessage = "Контактные данные обязательны")]
            [StringLength(200)]
            [Display(Name = "Контактные данные (телефон, email)")]
            public string ClientContactInfo { get; set; } = string.Empty;

            [Display(Name = "Предпочтения клиента")]
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

                // *** Логика для поиска или создания клиента ***
                var client = await _dbContext.Clients
                                             .FirstOrDefaultAsync(c => c.FullName == Input.ClientFullName &&
                                                                       c.PassportData == Input.ClientPassportData);

                if (client == null)
                {
                    // Клиент не найден, создаем нового
                    client = new Client
                    {
                        FullName = Input.ClientFullName,
                        PassportData = Input.ClientPassportData,
                        ContactInfo = Input.ClientContactInfo,
                        Preferences = Input.ClientPreferences
                    };
                    _dbContext.Clients.Add(client);
                    await _dbContext.SaveChangesAsync(); // Сохраняем нового клиента, чтобы получить его Id
                }
                else
                {
                    // Опционально: Обновляем данные существующего клиента, если они могли измениться
                    // client.ContactInfo = Input.ClientContactInfo;
                    // client.Preferences = Input.ClientPreferences;
                    // _dbContext.Clients.Update(client);
                    // await _dbContext.SaveChangesAsync();
                }

                user.ClientId = client.Id; // Присваиваем ID найденного или созданного клиента пользователю
                // ***********************************************

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Пользователь создал новую учетную запись с паролем.");

                    // *** Логика назначения роли "Клиент" по умолчанию ***
                    // 1. Убедимся, что роль "Клиент" существует. Если нет, создадим ее.
                    if (!await _roleManager.RoleExistsAsync("Клиент"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Клиент"));
                        _logger.LogInformation("Создана новая роль: 'Клиент'.");
                    }
                    // 2. Убедимся, что роль "Администратор" существует (если она используется).
                    if (!await _roleManager.RoleExistsAsync("Администратор"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Администратор"));
                        _logger.LogInformation("Создана новая роль: 'Администратор'.");
                    }
                    // 3. Убедимся, что роль "Менеджер" существует (если она используется).
                    if (!await _roleManager.RoleExistsAsync("Менеджер"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Менеджер"));
                        _logger.LogInformation("Создана новая роль: 'Менеджер'.");
                    }

                    // Назначаем роль "Клиент" новому пользователю.
                    await _userManager.AddToRoleAsync(user, "Клиент");
                    _logger.LogInformation($"Пользователю {user.Email} назначена роль 'Клиент'.");
                    // ***************************************************

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Подтвердите свой email",
                        $"Пожалуйста, подтвердите свою учетную запись, <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>нажав здесь</a>.");

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