﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportsWeb.Models;
using SportsWeb.Repositories;

namespace SportsWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = SD.Role_Admin)]
    public class ProductsController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        public IActionResult Test()
        {
            return View();
        }

        public ProductsController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }
        private void AssignCategories(Product product, int[] selectedCategoryIds)
        {
            if (selectedCategoryIds != null && selectedCategoryIds.Length > 0)
            {
                foreach (var categoryId in selectedCategoryIds)
                {
                    product.ProductCategories.Add(new ProductCategory { ProductId = product.Id, CategoryId = categoryId });
                }
            }
        }
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
        private bool IsImage(IFormFile file)
        {
            string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return !string.IsNullOrEmpty(ext) && permittedExtensions.Contains(ext);
        }
        private async Task<string> SaveImage(IFormFile image)
        {
            if (!IsImage(image))
            {
                throw new InvalidOperationException("Invalid image file");
            }
            var savePath = Path.Combine("wwwroot/product-images", image.FileName);
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            return "/product-images/" + Path.GetFileName(image.FileName);
        }
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Price,Description,ImageUrl")] Product product, IFormFile imageUrl, int[] selectedCategoryIds)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null)
                {
                    product.ImageUrl = await SaveImage(imageUrl);

                }
                if (product.ProductCategories == null)
                {
                    product.ProductCategories = new List<ProductCategory>();
                }
                if (selectedCategoryIds != null && selectedCategoryIds.Length > 0)
                {
                    foreach (var categoryId in selectedCategoryIds)
                    {
                        product.ProductCategories.Add(new ProductCategory { CategoryId = categoryId });
                    }
                }
                await _productRepository.AddAsync(product);
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _categoryRepository.GetAllAsync();
            var selectedCategoryIds = product.ProductCategories.Select(pc => pc.CategoryId).ToArray();
            ViewBag.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = selectedCategoryIds.Contains(c.Id)
            }).ToList();
            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Description,ImageUrl")] Product product, IFormFile imageUrl, int[] selectedCategoryIds)
        {
            ModelState.Remove("ImageUrl");
            if (id != product.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (imageUrl == null)
                {
                    product.ImageUrl = existingProduct.ImageUrl;
                }
                else
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.ProductCategories.Clear();
                AssignCategories(existingProduct, selectedCategoryIds);
                existingProduct.ImageUrl = product.ImageUrl;
                await _productRepository.UpdateAsync(existingProduct, selectedCategoryIds);
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.ProductCategories.Select(pc => pc.CategoryId));
            return View(product);
        }
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
