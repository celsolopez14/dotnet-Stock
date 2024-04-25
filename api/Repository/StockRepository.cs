using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.DTOs.Stock;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class StockRepository : IStockRepository
    {
        private readonly ApplicationDBContext _context;
        public StockRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<Stock> CreateStockAsync(Stock stockModel)
        {
            await _context.Stocks.AddAsync(stockModel);

            await _context.SaveChangesAsync();
            return stockModel;
        }

        public async Task<Stock?> DeleteStockAsync(int id)
        {
            var stockModel = await _context.Stocks.FirstOrDefaultAsync((s) => s.Id == id);

            if (stockModel == null) return null;

            _context.Stocks.Remove(stockModel);

            await _context.SaveChangesAsync();
            return stockModel;
        }

        public async Task<List<Stock>> GetAllASync(QueryObject query)
        {
            var stocks = _context.Stocks.Include((c) => c.Comments).ThenInclude((a) => a.AppUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.CompanyName)) stocks = stocks.Where((s) => s.CompanyName.Contains(query.CompanyName));

            if (!string.IsNullOrWhiteSpace(query.Symbol)) stocks = stocks.Where((s) => s.Symbol.Contains(query.Symbol));

            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {

                if (query.SortBy.Equals("Symbol", StringComparison.OrdinalIgnoreCase))
                    stocks = query.IsDescending ? stocks.OrderByDescending((s) => s.Symbol) : stocks.OrderBy((s) => s.Symbol);
                
                if (query.SortBy.Equals("Company Name", StringComparison.OrdinalIgnoreCase))
                    stocks = query.IsDescending ? stocks.OrderByDescending((s) => s.CompanyName) : stocks.OrderBy((s) => s.CompanyName);
            }
            
            var skip = (query.PageNumber - 1)* query.PageSize;

            return await stocks.Skip(skip).Take(query.PageSize).ToListAsync();
        }

        public async Task<Stock?> GetStockAsync(int id)
        {
            return await _context.Stocks.Include((c) => c.Comments).ThenInclude((a) => a.AppUser).FirstOrDefaultAsync((s) => s.Id == id);
        }

        public async Task<Stock?> GetStockAsync(string symbol)
        {
            return await _context.Stocks.Include((c) => c.Comments).ThenInclude((a) => a.AppUser).FirstOrDefaultAsync((s) => s.Symbol == symbol);
        }

        public async Task<bool> StockExists(int id)
        {
            return await _context.Stocks.AnyAsync((s) => s.Id == id);
        }

        public async Task<Stock?> UpdateStockAsync(int id, Stock stockModel)
        {
            var existingStock = await _context.Stocks.FirstOrDefaultAsync((s) => s.Id == id);

            if (existingStock == null) return null;

            existingStock.Symbol = stockModel.Symbol;
            existingStock.Industry = stockModel.Industry;
            existingStock.CompanyName = stockModel.CompanyName;
            existingStock.Purchase = stockModel.Purchase;
            existingStock.LastDiv = stockModel.LastDiv;
            existingStock.MarketCap = stockModel.MarketCap;

            await _context.SaveChangesAsync();
            return existingStock;
        }
    }
}