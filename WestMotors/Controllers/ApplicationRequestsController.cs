using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WestMotorsApp.Data;
using WestMotorsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace WestMotorsApp.Controllers
{
    public class ApplicationRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationRequestsController> _logger;

        public ApplicationRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<ApplicationRequestsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize(Roles = "Менеджер,Администратор")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ApplicationRequests
                .Include(a => a.Car)
                .Include(a => a.Client);
            return View(await applicationDbContext.ToListAsync());
        }

        [Authorize(Roles = "Менеджер,Администратор")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationRequest = await _context.ApplicationRequests
                .Include(a => a.Car)
                .Include(a => a.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationRequest == null)
            {
                return NotFound();
            }

            return View(applicationRequest);
        }

        [Authorize(Roles = "Клиент")]
        public async Task<IActionResult> CreateForCar(int? carId)
        {
            if (carId == null)
            {
                TempData["ErrorMessage"] = "Автомобиль не выбран для заявки.";
                return RedirectToAction("Index", "Cars");
            }

            var car = await _context.Cars.FindAsync(carId);
            if (car == null)
            {
                TempData["ErrorMessage"] = "Выбранный автомобиль не найден в базе данных.";
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogError("Ошибка: _userManager.GetUserAsync(User) вернул null для залогиненного пользователя {UserName}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Произошла ошибка при определении данных пользователя. Пожалуйста, попробуйте войти снова.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            currentUser = await _context.Users
                                        .Include(u => u.Client)
                                        .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            var applicationRequest = new ApplicationRequest
            {
                CarId = car.Id,
                ClientId = currentUser?.ClientId,
                RequestDate = DateTime.UtcNow,
                Status = "Новая",
                UserEmail = currentUser?.Email,
                ClientFullName = currentUser?.Client?.FullName ?? currentUser?.FullName ?? currentUser?.Email ?? "Неизвестно"
            };

            await PopulateViewBagsForCreateForCar(applicationRequest, currentUser);

            return View(applicationRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Клиент")]
        public async Task<IActionResult> CreateForCar([Bind("CarId,RequestType,ClientFullName,ContactInfo,PreferredContactMethod")] ApplicationRequest applicationRequest)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                ModelState.AddModelError(string.Empty, "Ошибка: Пользователь не найден. Пожалуйста, попробуйте войти снова.");
                await PopulateViewBagsForCreateForCar(applicationRequest, null);
                return View(applicationRequest);
            }

            currentUser = await _context.Users
                                        .Include(u => u.Client)
                                        .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            applicationRequest.ClientId = currentUser?.ClientId;
            applicationRequest.UserEmail = currentUser?.Email;
            applicationRequest.RequestDate = DateTime.UtcNow;
            applicationRequest.Status = "Новая";

            if (applicationRequest.PreferredContactMethod == "Другой")
            {
                if (string.IsNullOrWhiteSpace(applicationRequest.ContactInfo))
                {
                    ModelState.AddModelError("ContactInfo", "Пожалуйста, введите контактные данные, если выбран 'Другой' способ связи.");
                }
            }
            else
            {
                if (applicationRequest.ContactInfo != applicationRequest.PreferredContactMethod)
                {
                    if (string.IsNullOrWhiteSpace(applicationRequest.ContactInfo))
                    {
                        applicationRequest.ContactInfo = applicationRequest.PreferredContactMethod;
                    }
                    else
                    {
                        applicationRequest.ContactInfo = applicationRequest.PreferredContactMethod;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(applicationRequest.ClientFullName))
            {
                ModelState.AddModelError("ClientFullName", "ФИО клиента обязательно.");
            }

            ModelState.Remove(nameof(applicationRequest.Car));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(applicationRequest);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Ваша заявка успешно отправлена!";
                    return RedirectToAction("MyApplications");
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Ошибка базы данных при сохранении заявки: {Message}", dbEx.Message);
                    var innerException = dbEx.InnerException;
                    while (innerException != null)
                    {
                        _logger.LogError("Inner Exception: {Message}", innerException.Message);
                        innerException = innerException.InnerException;
                    }

                    ModelState.AddModelError("", "Произошла ошибка при сохранении заявки в базу данных. Пожалуйста, проверьте вводимые данные или обратитесь в поддержку.");

                    if (applicationRequest.CarId > 0 && !await _context.Cars.AnyAsync(c => c.Id == applicationRequest.CarId))
                    {
                        ModelState.AddModelError("CarId", "Выбранный автомобиль не существует.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Неизвестная ошибка при сохранении заявки: {Message}", ex.Message);
                    ModelState.AddModelError("", "Произошла непредвиденная ошибка при отправке заявки. Пожалуйста, попробуйте еще раз.");
                }
            }

            await PopulateViewBagsForCreateForCar(applicationRequest, currentUser);
            return View(applicationRequest);
        }

        private async Task PopulateViewBagsForCreateForCar(ApplicationRequest model, ApplicationUser? currentUser)
        {
            if (model.CarId > 0)
            {
                ViewBag.Car = await _context.Cars.FindAsync(model.CarId);
            }
            else
            {
                ViewBag.Car = null;
            }

            List<SelectListItem> contactOptions = new List<SelectListItem>();

            if (currentUser != null && !string.IsNullOrEmpty(currentUser.Email))
            {
                contactOptions.Add(new SelectListItem { Value = currentUser.Email, Text = $"Email: {currentUser.Email}" });
            }

            if (currentUser?.Client != null && !string.IsNullOrEmpty(currentUser.Client.ContactInfo) && currentUser.Client.ContactInfo != currentUser?.Email)
            {
                contactOptions.Add(new SelectListItem { Value = currentUser.Client.ContactInfo, Text = $"Телефон/Другой: {currentUser.Client.ContactInfo}" });
            }

            contactOptions.Add(new SelectListItem { Value = "Другой", Text = "Другой (ввести вручную)" });

            ViewBag.ContactOptions = contactOptions;

            if (string.IsNullOrEmpty(model.PreferredContactMethod) && contactOptions.Any())
            {
                model.PreferredContactMethod = contactOptions.First().Value;
                if (model.PreferredContactMethod != "Другой")
                {
                    model.ContactInfo = model.PreferredContactMethod;
                }
            }
            else if (model.PreferredContactMethod != "Другой" && string.IsNullOrEmpty(model.ContactInfo))
            {
                model.ContactInfo = model.PreferredContactMethod;
            }
        }

        [Authorize(Roles = "Клиент")]
        public async Task<IActionResult> MyApplications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Пользователь не найден. Пожалуйста, войдите в систему.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            if (!currentUser.ClientId.HasValue)
            {
                currentUser = await _context.Users.Include(u => u.Client).FirstOrDefaultAsync(u => u.Id == currentUser.Id);
            }

            var clientApplications = await _context.ApplicationRequests
                                                    .Include(ar => ar.Car)
                                                    .Include(ar => ar.Client)
                                                    .Where(ar => ar.UserEmail == currentUser.Email || (ar.ClientId.HasValue && ar.ClientId == currentUser.ClientId))
                                                    .OrderByDescending(a => a.RequestDate)
                                                    .ToListAsync();
            return View(clientApplications);
        }

        [Authorize(Roles = "Менеджер,Администратор")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationRequest = await _context.ApplicationRequests.FindAsync(id);
            if (applicationRequest == null)
            {
                return NotFound();
            }
            ViewData["CarId"] = new SelectList(_context.Cars, "Id", "Brand", applicationRequest.CarId);
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "FullName", applicationRequest.ClientId);
            return View(applicationRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Менеджер,Администратор")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,CarId,RequestType,RequestDate,Status,ManagerNotes")] ApplicationRequest applicationRequest)
        {
            if (id != applicationRequest.Id)
            {
                return NotFound();
            }

            var existingRequest = await _context.ApplicationRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (existingRequest == null)
            {
                return NotFound();
            }

            existingRequest.RequestType = applicationRequest.RequestType;
            existingRequest.RequestDate = applicationRequest.RequestDate;
            existingRequest.Status = applicationRequest.Status;
            existingRequest.ManagerNotes = applicationRequest.ManagerNotes;
            existingRequest.CarId = applicationRequest.CarId;
            existingRequest.ClientId = applicationRequest.ClientId;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(existingRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationRequestExists(applicationRequest.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при редактировании заявки: {Message}", ex.Message);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении изменений. Пожалуйста, попробуйте еще раз.");
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CarId"] = new SelectList(_context.Cars, "Id", "Brand", applicationRequest.CarId);
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "FullName", applicationRequest.ClientId);
            return View(applicationRequest);
        }

        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationRequest = await _context.ApplicationRequests
                .Include(a => a.Car)
                .Include(a => a.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationRequest == null)
            {
                return NotFound();
            }

            return View(applicationRequest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var applicationRequest = await _context.ApplicationRequests.FindAsync(id);
            if (applicationRequest != null)
            {
                _context.ApplicationRequests.Remove(applicationRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationRequestExists(int id)
        {
            return _context.ApplicationRequests.Any(e => e.Id == id);
        }
    }
}