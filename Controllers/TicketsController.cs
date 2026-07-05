using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IllusionMuseum.Models;
using IllusionMuseum.Services;
using SixLabors.ImageSharp;
using ZXing;
using ZXing.ImageSharp;
using System.Linq;

namespace IllusionMuseum.Controllers
{
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        public TicketsController(AppDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // Проверка авторизации
        private async Task<bool> EnsureAuthenticatedAsync()
        {
            if (!_authService.IsAuthenticated())
            {
                return false;
            }
            return true;
        }

        // GET: Страница покупки билета
        [HttpGet]
        public async Task<IActionResult> Buy()
        {
            // Проверяем авторизацию
            if (!await EnsureAuthenticatedAsync())
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Tickets/Buy" });
            }

            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var museum = await _context.museuminfo.FirstOrDefaultAsync();
            ViewBag.Museum = museum;
            ViewBag.User = user;

            var ticket = new Ticket
            {
                UserId = user.Id,
                VisitDate = DateTime.Today.AddDays(1),
                VisitTime = "12:00",
                TicketType = "adult",
                Quantity = 1
            };

            return View(ticket);
        }

        // POST: Оформление билета
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(Ticket ticket)
        {
            if (await EnsureAuthenticatedAsync()==false)
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Tickets/Buy" });
            }

            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Убираем проверку для вычисляемых сервером полей
            ModelState.Remove("TotalPrice");
            ModelState.Remove("BookingDate");
            ModelState.Remove("Status");
            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                if (DateTime.TryParse($"{ticket.VisitDate.ToString("yyyy-MM-dd")} {ticket.VisitTime}", out DateTime sessionDateTime))
                {
                    // Если до сеанса осталось меньше 1 часа (или сеанс уже в прошлом)
                    if (sessionDateTime < DateTime.Now.AddHours(1))
                    {
                        ModelState.AddModelError("", "Билет можно купить не позднее, чем за 1 час до начала сеанса! Пожалуйста, выберите другое время или дату.");

                        // Перезагружаем данные музея и пользователя для возврата на форму
                        var currentMuseum = await _context.museuminfo.FirstOrDefaultAsync();
                        ViewBag.Museum = currentMuseum;
                        ViewBag.User = await _authService.GetCurrentUserAsync();
                        return View(ticket);
                    }
                }
                // Сохраняем количество, которое ввёл пользователь
                int totalQuantity = ticket.Quantity;

                // Массив для хранения ID созданных билетов, чтобы показать первый из них на экране успеха
                int firstTicketId = 0;

                // СОЗДАЕМ ОДНО ТОЧНОЕ ВРЕМЯ ДЛЯ ВСЕГО ЗАКАЗА ПЕРЕД ЦИКЛОМ FOR
                DateTime currentOrderTime = DateTime.Now;

                // Запускаем цикл: создаем столько отдельных записей в БД, сколько билетов указал пользователь
                for (int i = 0; i < totalQuantity; i++)
                {
                    var singleTicket = new Ticket
                    {
                        UserId = user.Id,
                        VisitDate = ticket.VisitDate,
                        VisitTime = ticket.VisitTime,
                        TicketType = ticket.TicketType,
                        Quantity = 1, // Каждый билет строго в количестве 1 шт.
                        TotalPrice = ticket.UnitPrice, // Итоговая цена одного билета равна цене за штуку
                        BookingDate = currentOrderTime, // ИСПОЛЬЗУЕМ ОБЩЕЕ ВРЕМЯ ДЛЯ ВСЕХ БИЛЕТОВ В ЧЕКЕ!
                        Status = "confirmed"
                    };

                    _context.tickets.Add(singleTicket);
                    await _context.SaveChangesAsync(); // Сохраняем в базу, чтобы сгенерировался уникальный Id

                    if (i == 0)
                    {
                        firstTicketId = singleTicket.Id; // Запоминаем ID самого первого билета для страницы успеха
                    }
                }

                TempData["SuccessMessage"] = $"Успешно оформлено билетов: {totalQuantity} шт.!";
                return RedirectToAction("Success", new { id = firstTicketId });
            }

            var museum = await _context.museuminfo.FirstOrDefaultAsync();
            ViewBag.Museum = museum;
            ViewBag.User = user;
            return View(ticket);
        }

        // GET: Успешное бронирование
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            // Проверяем, авторизован ли пользователь через метод с await
            if (await EnsureAuthenticatedAsync() == false)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Ищем тот билет, на который нас перенаправил сервер
            var ticket = await _context.tickets
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound();

            // НАХОДИМ ВСЕ БИЛЕТЫ ИЗ ЭТОГО ЖЕ ЗАКАЗА (купленные в ту же секунду)
            var orderTickets = await _context.tickets
                .Where(t => t.UserId == ticket.UserId && t.BookingDate == ticket.BookingDate)
                .ToListAsync();

            // Считаем общее количество и сумму для вывода на экран чека
            ViewBag.TotalQuantity = orderTickets.Count;
            ViewBag.OrderSum = orderTickets.Sum(t => t.TotalPrice);

            var museum = await _context.museuminfo.FirstOrDefaultAsync();
            ViewBag.Museum = museum;

            return View(ticket);
        }

        // GET: Мои билеты
        [HttpGet]
        public async Task<IActionResult> MyTickets()
        {
            // 1. Проверяем авторизацию через сессию
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int currentUserId = HttpContext.Session.GetInt32("UserId").Value;

            // 2. Достаем из базы MySQL ВСЕ билеты конкретного пользователя
            var userTickets = await _context.tickets
                .Where(t => t.UserId == currentUserId)
                .OrderByDescending(t => t.BookingDate) // Свежие билеты будут вверху списка
                .ToListAsync();

            // 3. Передаем список билетов в HTML-представление
            return View(userTickets);
        }

        // POST: Отмена билета
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!await EnsureAuthenticatedAsync())
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var ticket = await _context.tickets
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

            if (ticket != null && ticket.Status == "confirmed")
            {
                ticket.Status = "cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Билет успешно отменён";
            }

            return RedirectToAction("MyTickets");
        }
        [HttpGet]
        public async Task<IActionResult> Scanner()
        {
            // Проверяем, что пользователь авторизован и является администратором (IsAdmin == true)
            var user = await _authService.GetCurrentUserAsync();
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index", "Home"); // Запрещаем доступ обычным клиентам
            }

            var museum = await _context.museuminfo.FirstOrDefaultAsync();
            ViewBag.Museum = museum;
            return View();
        }

        // POST: /Tickets/Activate (API для сканера)
        [HttpPost]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null || !user.IsAdmin) return Forbid();

            var ticket = await _context.tickets.FindAsync(id);
            if (ticket == null)
                return Json(new { success = false, message = "Билет с таким номером не найден в базе данных!" });

            if (ticket.Status == "cancelled")
                return Json(new { success = false, message = "Внимание! Этот билет был ранее отменен покупателем!" });

            if (ticket.Status == "used")
                return Json(new { success = false, message = "Этот билет уже был использован на входе!" });

            // Меняем статус билета на использован
            ticket.Status = "used";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Билет №{id} успешно активирован! Проход разрешен." });
        }
        // POST: /Tickets/ScanRowImage (Серверное автономное декодирование QR)
        [HttpPost]
        public async Task<IActionResult> ScanRowImage(string base64Image)
        {
            if (string.IsNullOrEmpty(base64Image)) return Json(new { found = false });

            try
            {
                var base64Data = base64Image.Substring(base64Image.IndexOf(",") + 1);
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                using (var ms = new MemoryStream(imageBytes))
                {
                    using (var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgb24>(ms))
                    {
                        var reader = new ZXing.ImageSharp.BarcodeReader<SixLabors.ImageSharp.PixelFormats.Rgb24>();
                        var result = reader.Decode(image);

                        if (result != null && result.Text.Contains("Музей Иллюзий"))
                        {
                            var parts = result.Text.Split('|').Select(p => p.Trim()).ToArray();
                            string idVal = "-", dateVal = "-", timeVal = "-", typeVal = "-";

                            foreach (var part in parts)
                            {
                                if (part.Contains("Билет №")) idVal = part.Replace("Билет №", "").Trim();
                                if (part.Contains("Дата:")) dateVal = part.Replace("Дата:", "").Trim();
                                if (part.Contains("Время:")) timeVal = part.Replace("Время:", "").Trim();
                                if (part.Contains("Категория:")) typeVal = part.Replace("Категория:", "").Trim();
                            }

                            // --- НАЧАЛО ПРОВЕРКИ ВРЕМЕННОГО КОРИДОРА +-30 МИНУТ (Этап 3) ---
                            // Парсим дату и время сеанса из билета в один объект DateTime
                            if (DateTime.TryParse($"{dateVal} {timeVal}", out DateTime sessionDateTime))
                            {
                                DateTime now = DateTime.Now;

                                // Считаем разницу в минутах между текущим временем и сеансом
                                double minuteDifference = (now - sessionDateTime).TotalMinutes;

                                // Если разница больше 30 минут (человек опоздал) или меньше -30 минут (пришел слишком рано)
                                if (minuteDifference > 30 || minuteDifference < -30)
                                {
                                    return Json(new
                                    {
                                        found = true,
                                        isTimeValid = false, // Показывает фронтенду, что время некорректно
                                        id = idVal,
                                        date = dateVal,
                                        time = timeVal,
                                        type = typeVal,
                                        errorMessage = $"Проход запрещен! Время сеанса: {timeVal}. Допускается вход строго за 30 мин до/после сеанса."
                                    });
                                }
                            }
                            // --- КОНЕЦ ПРОВЕРКИ ---

                            return Json(new { found = true, isTimeValid = true, id = idVal, date = dateVal, time = timeVal, type = typeVal });
                        }
                    }
                }
            }
            catch { }

            return Json(new { found = false });
        }
    }
}