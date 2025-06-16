using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WestMotorsApp.Data;
using WestMotorsApp.Models;
using Microsoft.AspNetCore.Authorization; // Для авторизации
using System.IO; // Для работы с файлами

namespace WestMotorsApp.Controllers
{
    public class CarsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; // Для доступа к путям файлов

        public CarsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Cars - Главный каталог для всех пользователей
        public async Task<IActionResult> Index(string brand, int? minYear, int? maxMileage, decimal? maxPrice)
        {
            var cars = from c in _context.Cars
                       select c;

            if (!string.IsNullOrEmpty(brand))
            {
                cars = cars.Where(c => c.Brand.Contains(brand));
            }

            if (minYear.HasValue)
            {
                cars = cars.Where(c => c.ManufactureYear >= minYear.Value);
            }

            if (maxMileage.HasValue)
            {
                cars = cars.Where(c => c.Mileage <= maxMileage.Value);
            }

            if (maxPrice.HasValue)
            {
                cars = cars.Where(c => c.Price <= maxPrice.Value);
            }

            return View(await cars.ToListAsync());
        }

        // GET: Cars/Details/5 - Подробности об автомобиле
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(m => m.Id == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // GET: Cars/Create - Добавление нового автомобиля (Только для Администратора)
        [Authorize(Roles = "Администратор")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cars/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> Create([Bind("Id,Brand,Model,ManufactureYear,Mileage,VIN,Price,Condition,ArrivalDate,PhotoFile")] Car car)
        {
            if (ModelState.IsValid)
            {
                if (car.PhotoFile != null)
                {
                    // Сохранение файла
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(car.PhotoFile.FileName);
                    string path = Path.Combine(wwwRootPath, "uploads", fileName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await car.PhotoFile.CopyToAsync(fileStream);
                    }
                    car.PhotoUrl = "/uploads/" + fileName; // Сохраняем относительный путь в БД
                }

                _context.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(car);
        }

        // GET: Cars/Edit/5 - Редактирование автомобиля (Только для Администратора)
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound();
            }
            return View(car);
        }

        // POST: Cars/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Brand,Model,ManufactureYear,Mileage,VIN,Price,Condition,ArrivalDate,PhotoUrl,PhotoFile")] Car car)
        {
            if (id != car.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (car.PhotoFile != null)
                    {
                        // Удалить старое фото, если есть
                        if (!string.IsNullOrEmpty(car.PhotoUrl))
                        {
                            string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, car.PhotoUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Сохранить новое фото
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(car.PhotoFile.FileName);
                        string path = Path.Combine(wwwRootPath, "uploads", fileName);

                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await car.PhotoFile.CopyToAsync(fileStream);
                        }
                        car.PhotoUrl = "/uploads/" + fileName;
                    }
                    else if (string.IsNullOrEmpty(car.PhotoUrl)) // Если файл не загружен и PhotoUrl очищен
                    {
                        // Если старое фото было и его удалили
                        var existingCar = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                        if (existingCar != null && !string.IsNullOrEmpty(existingCar.PhotoUrl))
                        {
                            string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingCar.PhotoUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                    }

                    _context.Update(car);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(car);
        }

        // GET: Cars/Delete/5 - Удаление автомобиля (Только для Администратора)
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(m => m.Id == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // POST: Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                // Удаление файла фотографии
                if (!string.IsNullOrEmpty(car.PhotoUrl))
                {
                    string imagePath = Path.Combine(_hostEnvironment.WebRootPath, car.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _context.Cars.Remove(car);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }
    }
}