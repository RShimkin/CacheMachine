using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestCache.Models;
using TestCache.Models.Interfaces;
using TestCache.ViewModels;

namespace TestCache.Controllers
{
    public class OperationController : Controller
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(OperationController));
        private readonly IRep _rep;

        public OperationController(IRep repository)
        {
            _rep = repository;
        }

        [Authorize]
        public IActionResult Index()
        {
            log.Info("Operation -> Index (GET)");
            ViewBag.number = HttpContext.User.Identity.Name;
            return View();
        }

        [Authorize]
        [HttpGet]
        async public Task<IActionResult> Balance()
        {
            string id = HttpContext.User.Identity.Name;
            double? balance = await _rep.GetBalance(id);
            if (balance.HasValue)
            {
                log.Info($"card {id} balance success");
                return View((balance.Value, $"{Int64.Parse(id): ####-####-####-####}"));
            }
            log.Info($"card {id} balance fail");
            return View("ErrorView", new ErrorVM()
            {
                Message = "Не удалось найти данные...",
                BackController = "Operation",
                BackAction = "Index"
            });
        }

        [Authorize]
        [HttpGet]
        public IActionResult Take()
        {
            ViewBag.number = HttpContext.User.Identity.Name;
            return View();
        }

        [Authorize]
        [HttpPost]
        async public Task<IActionResult> Take(string Sumstr)
        {
            string CardNumber = HttpContext.User.Identity.Name;
            double sum = double.Parse(Sumstr);
            bool success;
            double residue;
            DateTime dt;
            string mes = "Ошибка транзакции:(";
            (success, residue, dt) = await _rep.TakeMoney(CardNumber, sum);
            if (success)
            {
                log.Info($"card {CardNumber} taken {sum} residue {residue}");
                var model = (sum, residue, CardNumber, dt.ToString());
                return View("Result", model);
            }
            else if (residue < 0)
            {
                log.Info($"card {CardNumber} not enough sum {sum}");
                mes = "Слишком мало денег на карте";
            }
            else
            {
                log.Info($"card {CardNumber} error taking {sum}");
            }
            return View("ErrorView", new ErrorVM()
            {
                Message = mes,
                BackController = "Operation",
                BackAction = "Index"
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
