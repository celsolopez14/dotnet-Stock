using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/portfolio")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IStockRepository _stockRepo;
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IFMPService _fmpService;

        public PortfolioController(UserManager<AppUser> userManager, IStockRepository stockRepo, IPortfolioRepository portfolioRepo, IFMPService fMPService)
        {
            _userManager = userManager;
            _stockRepo = stockRepo;
            _portfolioRepo = portfolioRepo;
            _fmpService = fMPService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserPortfolio()
        {
            var username = User.GetUserName();

            var existingUser = await _userManager.FindByNameAsync(username);

            var userPorfolio = await _portfolioRepo.GetUserPortfolio(existingUser);

            return Ok(userPorfolio);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostPortfolio(string symbol)
        {
            var username = User.GetUserName();

            var existingUser = await _userManager.FindByNameAsync(username);

            var stock = await _stockRepo.GetStockAsync(symbol);

            if (stock == null)
            {
                stock = await _fmpService.FindStockAsync(symbol);
                if (stock != null)
                {
                    await _stockRepo.CreateStockAsync(stock);
                }
                else
                {
                    return BadRequest("Stock does not exists.");
                }
            }

            var userPorfolio = await _portfolioRepo.GetUserPortfolio(existingUser);

            if (userPorfolio.Any((s) => s.Symbol.ToLower() == symbol.ToLower())) return BadRequest("Stock already exists");

            var portfolioModel = new Portfolio
            {
                StockId = stock.Id,
                AppUserId = existingUser.Id
            };

            portfolioModel = await _portfolioRepo.CreatePortfolio(portfolioModel);

            if (portfolioModel == null) return StatusCode(500, "Could not create");

            return Created();
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeletePortfolio(string symbol)
        {
            var username = User.GetUserName();
            var existingUser = await _userManager.FindByNameAsync(username);

            var userPorfolio = await _portfolioRepo.GetUserPortfolio(existingUser);

            var filteredStock = userPorfolio.Where((s) => s.Symbol.ToLower() == symbol.ToLower()).ToList();

            if (filteredStock.Count() == 0) return BadRequest("Stock not in your portfolio");

            await _portfolioRepo.DeletePortfolio(existingUser, symbol);
            return NoContent();
        }
    }
}