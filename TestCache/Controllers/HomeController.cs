using log4net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using TestCache.Models;
using TestCache.Models.Interfaces;
using TestCache.ViewModels;

namespace TestCache.Controllers
{
    public class HomeController : Controller
    {
        private static bool check = false;
        private static readonly ILog log = LogManager.GetLogger(typeof(HomeController));
        private readonly IRep _rep;
        private readonly ICrypt _crypt;

        public HomeController(IRep repository, ICrypt crypt)
        {
            _rep = repository;
            _crypt = crypt;
            if (!check)
            {
                int count = _rep.CountCards();
                if (count < 2)
                {
                    Card card1 = new Card
                    {
                        Number = "1111222233334444",
                        Pin = _crypt.HashPin("1111", "1111222233334444"),
                        Info = new CardInfo { Balance = 2000, Blocked = false }
                    };
                    Card card2 = new Card
                    {
                        Number = "1234123412341234",
                        Pin = _crypt.HashPin("1234", "1234123412341234"),
                        Info = new CardInfo { Balance = 500, Blocked = true }
                    };
                    _rep.AddCard(card1);
                    _rep.AddCard(card2);
                }
                check = true;
            }
        }

        async public Task<IActionResult> Index()
        {
            log.Info("Home Controller -> Index (GET)");
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                await _rep.UnlockCard(HttpContext.User.Identity.Name);
                await HttpContext.SignOutAsync();
            }
            return View();
        }

        [HttpPost]
        async public Task<IActionResult> Index(string Number)
        {
            log.Info("Home -> Index (POST)");
            string mes;
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                await _rep.UnlockCard(HttpContext.User.Identity.Name);
                await HttpContext.SignOutAsync();
            }
            Card card = await _rep.GetCard(Number);
            bool found = (card != null) ? true : false;
            if (found)
            {
                if (card.Info != null)
                {
                    if (!card.Info.Blocked)
                    {
                        if (!card.Info.Locked)
                        {
                            if ((await _rep.CountAttempts(Number)) == 0)
                            {
                                string guid = await _rep.AddAttempt(Number);
                                string Attempt = guid;//Guid.NewGuid().ToString() });
                                HttpContext.Session.Clear();
                                HttpContext.Session.SetString("CardNumber", Number);
                                HttpContext.Session.SetString("Attempt", Attempt);
                                log.Info($"card {Number} accessible");
                                return RedirectToAction("Pin", "Home");
                            }
                            else
                            {
                                mes = "Кто-то пытался ввести pin за последнюю минуту!";
                                log.Info($"card {Number} pin is already tried");
                            }
                        }
                        else
                        {
                            mes = "Карта уже кем-то используется!";
                            log.Info($"card {Number} blocked");
                        }
                    }
                    else
                    {
                        mes = "Карта недоступна!";
                        log.Info($"card {Number} blocked");
                    }
                }
                else
                {
                    mes = "Не удаётся загрузить информацию по карте...";
                    log.Info($"card {Number} info not found");
                }
            }
            else
            {
                mes = "Карта не найдена!";
                log.Info($"card {Number} not found");
            }
            return View("ErrorView", new ErrorVM() { 
                Message = mes,
                BackController = "Home",
                BackAction = "Index"
            });
        }

        [HttpGet]
        async public Task<IActionResult> Pin()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                await _rep.UnlockCard(HttpContext.User.Identity.Name);
                await HttpContext.SignOutAsync();
            }
            string CardNumber = HttpContext.Session.GetString("CardNumber");
            string Attempt = HttpContext.Session.GetString("Attempt");
            if (!string.IsNullOrEmpty(CardNumber) && !string.IsNullOrEmpty(Attempt))
            {
                Attempt at = await _rep.GetAttempt(Attempt);
                if (at != null)
                {
                    PinVM model = new() { Number = $"{Int64.Parse(CardNumber): ####-####-####-####}", Counter = 4 };
                    return View(model);
                }
            }
            return View("ErrorView", new ErrorVM()
            {
                Message = "Сессия прервалась или истекла",
                BackController = "Home",
                BackAction = "Index"
            });
        }

        [HttpPost]
        async public Task<IActionResult> Pin(string Pin)
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                await _rep.UnlockCard(HttpContext.User.Identity.Name);
                await HttpContext.SignOutAsync();
            }
            string CardNumber = HttpContext.Session.GetString("CardNumber");
            string Attempt = HttpContext.Session.GetString("Attempt");
            string mes = "Сессия прервалась или истекла";
            if (!string.IsNullOrEmpty(CardNumber) && !string.IsNullOrEmpty(Attempt))
            {
                Attempt attempt = await _rep.GetAttempt(Attempt);
                if (attempt != null)
                {
                    Card card = await _rep.GetCard(CardNumber);
                    if (card != null && _crypt.Compare(Pin, CardNumber, card.Pin))
                    {
                        await _rep.LockCard(CardNumber);
                        await _rep.DeleteAttempt(attempt);
                        await AuthenticateAsync(CardNumber);
                        HttpContext.Session.Clear();
                        return RedirectToAction("Index", "Operation");
                    }
                    if (--attempt.Counter < 1)
                    {
                        await _rep.BlockCard(card.Number);
                        await _rep.DeleteAttempt(attempt);
                        mes = "Слишком много попыток, карта заблокирована!";
                    }
                    else
                    {
                        await _rep.UpdateAttempt(attempt);
                        PinVM model = new() { Number = $"{Int64.Parse(CardNumber): ####-####-####-####}", Counter = attempt.Counter };
                        return View(model);
                    }
                }
            }
            return View("ErrorView", new ErrorVM()
            {
                Message = mes,
                BackController = "Home",
                BackAction = "Index"
            });
        }

        public async Task AuthenticateAsync(string cardNumber)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, cardNumber)
            };
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie",
                ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
