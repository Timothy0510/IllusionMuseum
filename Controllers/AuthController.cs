using Microsoft.AspNetCore.Mvc;
using IllusionMuseum.Models;
using IllusionMuseum.Services;

namespace IllusionMuseum.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: Страница входа
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_authService.IsAuthenticated())
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: Вход
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Username, string Password, bool RememberMe)
        {
            // 1. Проверяем пустые поля при отправке формы — если они пустые, пишем нашу ошибку
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                TempData["ErrorMessage"] = "Неверный логин или пароль!";
                return View();
            }

            // 2. Формируем модель для вызова вашего AuthService
            var loginModel = new IllusionMuseum.Models.LoginViewModel
            {
                UserNameOrEmail = Username,
                Password = Password,
                RememberMe = RememberMe
            };

            var result = await _authService.LoginAsync(loginModel);

            // 3. Если проверка провалилась (пользователя нет или пароль не совпал)
            if (!result.Success)
            {
                // Записываем ровно одну красивую ошибку строго по вашему ТЗ!
                TempData["ErrorMessage"] = "Неверный логин или пароль!";
                return View();
            }

            // 4. Если всё успешно, извлекаем объект пользователя и пишем сессию
            var user = result.User;
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin ? "True" : "False");

            return RedirectToAction("Index", "Home");
        }

        // GET: Страница регистрации
        [HttpGet]
        public IActionResult Register()
        {
            if (_authService.IsAuthenticated())
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        // POST: Регистрация
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.RegisterAsync(model);

            if (result.Success && result.User != null)
            {
                // Автоматически входим после регистрации
                HttpContext.Session.SetInt32("UserId", result.User.Id);
                HttpContext.Session.SetString("UserName", result.User.UserName);
                HttpContext.Session.SetString("FullName", result.User.FullName);
                HttpContext.Session.SetString("IsAdmin", result.User.IsAdmin.ToString());

                TempData["SuccessMessage"] = "Регистрация успешна! Добро пожаловать!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        // GET: Выход
        [HttpGet]
        public IActionResult Logout()
        {
            _authService.Logout();
            return RedirectToAction("Index", "Home");
        }
    }
}