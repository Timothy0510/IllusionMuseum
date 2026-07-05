using IllusionMuseum.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace IllusionMuseum.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Регистрация нового пользователя
        public async Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterViewModel model)
        {
            // Проверяем, существует ли пользователь
            var existingUser = await _context.users
                .FirstOrDefaultAsync(u => u.UserName == model.UserName || u.Email == model.Email);

            if (existingUser != null)
            {
                if (existingUser.UserName == model.UserName)
                    return (false, "Пользователь с таким логином уже существует", null);
                if (existingUser.Email == model.Email)
                    return (false, "Пользователь с таким email уже существует", null);
            }

            // Хешируем пароль
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                PasswordHash = passwordHash,
                FullName = model.FullName,
                Phone = model.Phone,
                RegistrationDate = DateTime.Now,
                IsAdmin = false
            };

            _context.users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "Регистрация успешна!", user);
        }

        // Вход пользователя
        public async Task<(bool Success, string Message, User? User)> LoginAsync(LoginViewModel model)
        {
            // Ищем пользователя по логину или email
            var user = await _context.users
                .FirstOrDefaultAsync(u => u.UserName == model.UserNameOrEmail ||
                                         u.Email == model.UserNameOrEmail);

            if (user == null)
                return (false, "Неверный логин или пароль", null);

            // Проверяем пароль
            bool passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!passwordValid)
                return (false, "Неверный логин или пароль", null);

            return (true, "Вход выполнен успешно!", user);
        }

        // Получение текущего пользователя из сессии
        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.Session.GetInt32("UserId");

            if (userId == null)
                return null;

            return await _context.users.FindAsync(userId.Value);
        }

        // Проверка, авторизован ли пользователь
        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.Session.GetInt32("UserId") != null;
        }

        // Выход
        public void Logout()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }
    }
}