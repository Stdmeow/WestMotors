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
using System.IO;
using Microsoft.AspNetCore.Hosting;

using NPOI.XWPF.UserModel;
using NPOI.OpenXmlFormats.Wordprocessing;

namespace WestMotorsApp.Controllers
{
    [Authorize(Roles = "Менеджер,Администратор")]
    public class DealsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DealsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Deals
                .Include(d => d.Buyer)
                .Include(d => d.Car)
                .Include(d => d.Seller);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var deal = await _context.Deals
                .Include(d => d.Buyer)
                .Include(d => d.Car)
                .Include(d => d.Seller)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (deal == null)
            {
                return NotFound();
            }

            return View(deal);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["BuyerId"] = new SelectList(_context.Clients, "Id", "FullName");
            ViewData["CarId"] = new SelectList(_context.Cars.Where(c => !c.Deals.Any()), "Id", "VIN");

            var currentUser = await _userManager.GetUserAsync(User);
            ViewData["SellerId"] = new SelectList(new List<ApplicationUser> { currentUser }, "Id", "FullName", currentUser.Id);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CarId,SellerId,BuyerId,DealDate,FinalCost,PaymentMethod")] Deal deal)
        {
            ModelState.Remove("Car");
            ModelState.Remove("Buyer");
            ModelState.Remove("Seller");

            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "Не удалось определить продавца. Пожалуйста, войдите снова.";
                    ViewData["BuyerId"] = new SelectList(_context.Clients, "Id", "FullName", deal.BuyerId);
                    ViewData["CarId"] = new SelectList(_context.Cars.Where(c => !c.Deals.Any()), "Id", "VIN", deal.CarId);
                    ViewData["SellerId"] = new SelectList(new List<ApplicationUser> { currentUser }, "Id", "FullName", currentUser.Id);
                    return View(deal);
                }

                deal.SellerId = currentUser.Id;

                var existingDealForCar = await _context.Deals.AnyAsync(d => d.CarId == deal.CarId);
                if (existingDealForCar)
                {
                    ModelState.AddModelError("CarId", "Этот автомобиль уже продан.");
                    ViewData["BuyerId"] = new SelectList(_context.Clients, "Id", "FullName", deal.BuyerId);
                    ViewData["CarId"] = new SelectList(_context.Cars.Where(c => !c.Deals.Any()), "Id", "VIN", deal.CarId);
                    ViewData["SellerId"] = new SelectList(new List<ApplicationUser> { currentUser }, "Id", "FullName", currentUser.Id);
                    return View(deal);
                }

                _context.Add(deal);
                await _context.SaveChangesAsync();

                currentUser.SoldCarsCount++;
                await _userManager.UpdateAsync(currentUser);

                TempData["SuccessMessage"] = "Сделка успешно создана!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["BuyerId"] = new SelectList(_context.Clients, "Id", "FullName", deal.BuyerId);
            ViewData["CarId"] = new SelectList(_context.Cars.Where(c => !c.Deals.Any()), "Id", "VIN", deal.CarId);
            var currentUserForView = await _userManager.GetUserAsync(User);
            ViewData["SellerId"] = new SelectList(new List<ApplicationUser> { currentUserForView }, "Id", "FullName", currentUserForView.Id);

            TempData["ErrorMessage"] = "Не удалось создать сделку. Пожалуйста, проверьте введённые данные.";
            return View(deal);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var deal = await _context.Deals.FindAsync(id);
            if (deal == null)
            {
                return NotFound();
            }
            ViewData["BuyerId"] = new SelectList(_context.Clients, "Id", "FullName", deal.BuyerId);
            ViewData["CarId"] = new SelectList(_context.Cars, "Id", "VIN", deal.CarId);
            ViewData["SellerId"] = new SelectList(_context.Users, "Id", "FullName", deal.SellerId);
            return View(deal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CarId,SellerId,BuyerId,DealDate,FinalCost,PaymentMethod")] Deal deal)
        {
            if (id != deal.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalCarId = (await _context.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id))?.CarId;
                    if (originalCarId != deal.CarId && await _context.Deals.AnyAsync(d => d.CarId == deal.CarId))
                    {
                        ModelState.AddModelError("CarId", "Этот автомобиль уже продан в другой сделке.");
                        ViewData["BuyerId"] = new SelectList(_context.Clients, "Id", "FullName", deal.BuyerId);
                        ViewData["CarId"] = new SelectList(_context.Cars, "Id", "VIN", deal.CarId);
                        ViewData["SellerId"] = new SelectList(_context.Users, "Id", "FullName", deal.SellerId);
                        return View(deal);
                    }

                    _context.Update(deal);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DealExists(deal.Id))
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
            ViewData["BuyerId"] = new SelectList(_context.Clients, "Id", "FullName", deal.BuyerId);
            ViewData["CarId"] = new SelectList(_context.Cars, "Id", "VIN", deal.CarId);
            ViewData["SellerId"] = new SelectList(_context.Users, "Id", "FullName", deal.SellerId);
            return View(deal);
        }

        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var deal = await _context.Deals
                .Include(d => d.Buyer)
                .Include(d => d.Car)
                .Include(d => d.Seller)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (deal == null)
            {
                return NotFound();
            }

            return View(deal);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal != null)
            {
                _context.Deals.Remove(deal);
                var seller = await _userManager.FindByIdAsync(deal.SellerId);
                if (seller != null)
                {
                    seller.SoldCarsCount--;
                    await _userManager.UpdateAsync(seller);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DealExists(int id)
        {
            return _context.Deals.Any(e => e.Id == id);
        }

        // Метод для генерации договора купли-продажи в Word
        public async Task<IActionResult> GenerateContract(int id)
        {
            var deal = await _context.Deals
                .Include(d => d.Buyer)
                .Include(d => d.Car)
                .Include(d => d.Seller)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (deal == null)
            {
                return NotFound();
            }

            XWPFDocument document = new XWPFDocument();

            // Заголовок договора
            XWPFParagraph titleParagraph = document.CreateParagraph();
            titleParagraph.Alignment = ParagraphAlignment.CENTER;
            XWPFRun titleRun = titleParagraph.CreateRun();
            titleRun.SetText("ДОГОВОР КУПЛИ-ПРОДАЖИ АВТОМОБИЛЯ №" + deal.Id);
            titleRun.FontSize = 16;
            titleRun.IsBold = true;

            document.CreateParagraph(); // Пустая строка

            // Место и дата
            XWPFParagraph headerParagraph = document.CreateParagraph();
            headerParagraph.Alignment = ParagraphAlignment.RIGHT;
            XWPFRun headerRun = headerParagraph.CreateRun();
            headerRun.SetText($"г. [Город], {deal.DealDate:«dd» MMMM yyyy г.}");

            document.CreateParagraph(); // Пустая строка

            // Стороны договора
            XWPFParagraph partiesParagraph = document.CreateParagraph();
            XWPFRun partiesRun = partiesParagraph.CreateRun();
            partiesRun.SetText($"Мы, нижеподписавшиеся, {deal.Seller.FullName}, именуемый в дальнейшем «Продавец», с одной стороны, и {deal.Buyer.FullName}, именуемый в дальнейшем «Покупатель», с другой стороны, заключили настоящий Договор о нижеследующем:");

            document.CreateParagraph(); // Пустая строка

            // Предмет договора
            XWPFParagraph subjectTitle = document.CreateParagraph();
            XWPFRun subjectTitleRun = subjectTitle.CreateRun();
            subjectTitleRun.IsBold = true;
            subjectTitleRun.SetText("1. ПРЕДМЕТ ДОГОВОРА");

            document.CreateParagraph().CreateRun().SetText($"1.1. Продавец обязуется передать в собственность Покупателю, а Покупатель обязуется принять и оплатить автомобиль марки {deal.Car.Brand}, модель {deal.Car.Model}, VIN {deal.Car.VIN}, год выпуска {deal.Car.ManufactureYear}, пробег {deal.Car.Mileage} км, в состоянии «{deal.Car.Condition}» (далее – «Автомобиль»).");
            document.CreateParagraph().CreateRun().SetText("1.2. Автомобиль принадлежит Продавцу на праве собственности.");
            document.CreateParagraph();

            // Цена и порядок расчетов
            XWPFParagraph priceTitle = document.CreateParagraph();
            XWPFRun priceTitleRun = priceTitle.CreateRun();
            priceTitleRun.IsBold = true;
            priceTitleRun.SetText("2. ЦЕНА И ПОРЯДОК РАСЧЕТОВ");

            document.CreateParagraph().CreateRun().SetText($"2.1. Стоимость Автомобиля по настоящему Договору составляет {deal.FinalCost:C}.");
            document.CreateParagraph().CreateRun().SetText($"2.2. Оплата производится Покупателем Продавцу в размере {deal.FinalCost:C} путем {deal.PaymentMethod}.");
            document.CreateParagraph();

            // Права и обязанности сторон
            XWPFParagraph rightsTitle = document.CreateParagraph();
            XWPFRun rightsTitleRun = rightsTitle.CreateRun();
            rightsTitleRun.IsBold = true;
            rightsTitleRun.SetText("3. ПРАВА И ОБЯЗАННОСТИ СТОРОН");
            document.CreateParagraph().CreateRun().SetText("3.1. Продавец обязуется передать Автомобиль Покупателю в срок и на условиях, предусмотренных настоящим Договором.");
            document.CreateParagraph().CreateRun().SetText("3.2. Покупатель обязуется принять и оплатить Автомобиль в соответствии с условиями настоящего Договора.");
            document.CreateParagraph();

            // Заключительные положения
            XWPFParagraph finalTitle = document.CreateParagraph();
            XWPFRun finalTitleRun = finalTitle.CreateRun();
            finalTitleRun.IsBold = true;
            finalTitleRun.SetText("4. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ");
            document.CreateParagraph().CreateRun().SetText("4.1. Настоящий Договор вступает в силу с момента его подписания Сторонами.");
            document.CreateParagraph().CreateRun().SetText("4.2. Все изменения и дополнения к настоящему Договору действительны лишь в том случае, если они совершены в письменной форме и подписаны уполномоченными представителями Сторон.");
            document.CreateParagraph();

            // Реквизиты сторон
            XWPFParagraph requisitesTitle = document.CreateParagraph();
            XWPFRun requisitesTitleRun = requisitesTitle.CreateRun();
            requisitesTitleRun.IsBold = true;
            requisitesTitleRun.SetText("РЕКВИЗИТЫ СТОРОН:");

            XWPFTable table = document.CreateTable(1, 2); // 1 строка, 2 столбца для начала
            table.SetColumnWidth(0, 4500); // Ширина для первой колонки
            table.SetColumnWidth(1, 4500); // Ширина для второй колонки

            // Продавец
            XWPFParagraph p1 = table.GetRow(0).GetCell(0).AddParagraph();
            XWPFRun r1 = p1.CreateRun();
            r1.IsBold = true;
            r1.SetText("Продавец:");
            table.GetRow(0).GetCell(0).AddParagraph().CreateRun().SetText($"ФИО: {deal.Seller.FullName}");
            table.GetRow(0).GetCell(0).AddParagraph().CreateRun().SetText($"Должность: {deal.Seller.Position}");
            table.GetRow(0).GetCell(0).AddParagraph().CreateRun().SetText($"Email: {deal.Seller.Email}");
            table.GetRow(0).GetCell(0).AddParagraph().CreateRun().SetText("Подпись: __________________");


            // Покупатель
            XWPFParagraph p2 = table.GetRow(0).GetCell(1).AddParagraph();
            XWPFRun r2 = p2.CreateRun();
            r2.IsBold = true;
            r2.SetText("Покупатель:");
            table.GetRow(0).GetCell(1).AddParagraph().CreateRun().SetText($"ФИО: {deal.Buyer.FullName}");
            table.GetRow(0).GetCell(1).AddParagraph().CreateRun().SetText($"Контакт: {deal.Buyer.ContactInfo}");
            table.GetRow(0).GetCell(1).AddParagraph().CreateRun().SetText($"Паспорт: {deal.Buyer.PassportData}");
            table.GetRow(0).GetCell(1).AddParagraph().CreateRun().SetText("Подпись: __________________");


            byte[] fileBytes;
            using (MemoryStream outputStream = new MemoryStream())
            {
                document.Write(outputStream);
                fileBytes = outputStream.ToArray();
            }

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Договор_Купли-Продажи_{deal.Id}.docx");
        }

        // Метод для генерации HTML-предварительного просмотра договора
        public async Task<IActionResult> GetContractHtmlPreview(int id)
        {
            var deal = await _context.Deals
                .Include(d => d.Buyer)
                .Include(d => d.Car)
                .Include(d => d.Seller)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (deal == null)
            {
                return NotFound();
            }

            // Формируем HTML-строку для предварительного просмотра
            string htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Предварительный просмотр договора</title>
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; color: #333; font-size: 14px; }}
                        h2 {{ text-align: center; color: #0056b3; margin-bottom: 20px; }}
                        h3 {{ color: #0056b3; border-bottom: 1px solid #eee; padding-bottom: 5px; margin-top: 20px; }}
                        p {{ margin-bottom: 10px; line-height: 1.6; }}
                        .section-title {{ font-weight: bold; margin-top: 15px; margin-bottom: 5px; }}
                        .details-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                        .details-table td {{ padding: 8px; border: 1px solid #ddd; vertical-align: top; }}
                        .details-table .label {{ font-weight: bold; width: 30%; }}
                        .signature-block {{ margin-top: 40px; display: flex; justify-content: space-around; }}
                        .signature-item {{ flex: 1; text-align: center; margin: 0 10px; }}
                        .signature-line {{ border-bottom: 1px solid #000; width: 80%; margin: 20px auto 5px auto; }}
                    </style>
                </head>
                <body>
                    <h2>ДОГОВОР КУПЛИ-ПРОДАЖИ АВТОМОБИЛЯ №{deal.Id}</h2>
                    <p style='text-align: right;'>г. [Город], {deal.DealDate:«dd» MMMM yyyy г.}</p>
                    <p>Мы, нижеподписавшиеся, <strong>{deal.Seller.FullName}</strong>, именуемый в дальнейшем «Продавец», с одной стороны, и <strong>{deal.Buyer.FullName}</strong>, именуемый в дальнейшем «Покупатель», с другой стороны, заключили настоящий Договор о нижеследующем:</p>

                    <h3>1. ПРЕДМЕТ ДОГОВОРА</h3>
                    <p>1.1. Продавец обязуется передать в собственность Покупателю, а Покупатель обязуется принять и оплатить автомобиль марки <strong>{deal.Car.Brand}</strong>, модель <strong>{deal.Car.Model}</strong>, VIN <strong>{deal.Car.VIN}</strong>, год выпуска <strong>{deal.Car.ManufactureYear}</strong>, пробег <strong>{deal.Car.Mileage} км</strong>, в состоянии «{deal.Car.Condition}» (далее – «Автомобиль»).</p>
                    <p>1.2. Автомобиль принадлежит Продавцу на праве собственности.</p>

                    <h3>2. ЦЕНА И ПОРЯДОК РАСЧЕТОВ</h3>
                    <p>2.1. Стоимость Автомобиля по настоящему Договору составляет <strong>{deal.FinalCost:C}</strong>.</p>
                    <p>2.2. Оплата производится Покупателем Продавцу в размере <strong>{deal.FinalCost:C}</strong> путем {deal.PaymentMethod}.</p>

                    <h3>3. ПРАВА И ОБЯЗАННОСТИ СТОРОН</h3>
                    <p>3.1. Продавец обязуется передать Автомобиль Покупателю в срок и на условиях, предусмотренных настоящим Договором.</p>
                    <p>3.2. Покупатель обязуется принять и оплатить Автомобиль в соответствии с условиями настоящего Договора.</p>

                    <h3>4. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ</h3>
                    <p>4.1. Настоящий Договор вступает в силу с момента его подписания Сторонами.</p>
                    <p>4.2. Все изменения и дополнения к настоящему Договору действительны лишь в том случае, если они совершены в письменной форме и подписаны уполномоченными представителями Сторон.</p>

                    <h3>РЕКВИЗИТЫ СТОРОН:</h3>
                    <table class='details-table'>
                        <tr>
                            <td style='width: 50%;'>
                                <p><strong>Продавец:</strong></p>
                                <p>ФИО: {deal.Seller.FullName}</p>
                                <p>Должность: {deal.Seller.Position}</p>
                                <p>Email: {deal.Seller.Email}</p>
                                <p style='margin-top: 30px;'>Подпись: __________________</p>
                            </td>
                            <td style='width: 50%;'>
                                <p><strong>Покупатель:</strong></p>
                                <p>ФИО: {deal.Buyer.FullName}</p>
                                <p>Контакт: {deal.Buyer.ContactInfo}</p>
                                <p>Паспорт: {deal.Buyer.PassportData}</p>
                                <p style='margin-top: 30px;'>Подпись: __________________</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            return Content(htmlContent, "text/html");
        }

        private PictureType GetNPOIPictureType(string imagePath)
        {
            string extension = Path.GetExtension(imagePath)?.ToLowerInvariant();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return PictureType.JPEG;
                case ".png":
                    return PictureType.PNG;
                case ".gif":
                    return PictureType.GIF;
                case ".bmp":
                    return PictureType.BMP;
                case ".tiff":
                    return PictureType.TIFF;
                default:
                    return PictureType.PNG;
            }
        }
    }
}