using Bulky.DataAccess.Repository.IRepository;
using Bulky.DataAccess.Repository;
using Bulky.Models;
using Microsoft.EntityFrameworkCore;
using Bulky.DataAccess.Data;
using System.Collections.Generic;
using System.Linq;

public class ProductRepository : Repository<Product>, IProductRepository
{
    private readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(Product obj)
    {
        var objFromDb = _db.Products.FirstOrDefault(u => u.Id == obj.Id);
        if (objFromDb != null)
        {
            objFromDb.Title = obj.Title;
            objFromDb.ISBN = obj.ISBN;
            objFromDb.Price = obj.Price;
            objFromDb.Price50 = obj.Price50;
            objFromDb.ListPrice = obj.ListPrice;
            objFromDb.Description = obj.Description;
            objFromDb.CategoryId = obj.CategoryId;
            objFromDb.Author = obj.Author;
            objFromDb.ProductImages = obj.ProductImages;
        }
    }

    public IEnumerable<Product> GetAll(string includeProperties)
    {
        IQueryable<Product> query = _db.Products;

        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProperty in includeProperties.Split(','))
            {
                query = query.Include(includeProperty);
            }
        }

        return query.ToList();
    }
}
