using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using shop_pc.db;
using shop_pc.helpers;

namespace shop_pc.viewmodels
{
    public class ProductEditViewModel : ViewModelBase
    {
        private readonly int _productId;
        private readonly byte[] _rowVersion;
        private string _errorMessage = "";

        // Поля, которые только вводит пользователь, обычные свойства (уведомления не нужны)
        public string Sku { get; set; }
        public string Name { get; set; }
        public string PriceText { get; set; }
        public string StockText { get; set; }
        public string Specifications { get; set; }
        public Category SelectedCategory { get; set; }

        public List<Category> Categories { get; private set; }
        public string WindowTitle { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public event Action<bool> CloseRequested;

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetField(ref _errorMessage, value); }
        }

        // product == null: новый товар; иначе редактируем копию значений
        public ProductEditViewModel(Product product)
        {
            using (var db = new shop_pcEntities())
            {
                Categories = db.Categories.OrderBy(c => c.Name).ToList();
            }

            if (product == null)
            {
                WindowTitle = "Новый товар";
                Sku = ""; Name = ""; PriceText = ""; StockText = "0"; Specifications = "";
            }
            else
            {
                _productId = product.Id;
                _rowVersion = product.RowVersion;
                WindowTitle = "Редактирование товара";
                Sku = product.Sku;
                Name = product.Name;
                PriceText = product.Price.ToString("0.##");
                StockText = product.StockQuantity.ToString();
                Specifications = product.Specifications;
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
            }

            SaveCommand = new RelayCommand(p => Save());
        }

        private void Save()
        {
            ErrorMessage = "";
            string sku = (Sku ?? "").Trim();
            string name = (Name ?? "").Trim();

            if (sku == "") { ErrorMessage = "Введите артикул"; return; }
            if (name == "") { ErrorMessage = "Введите название"; return; }
            if (SelectedCategory == null) { ErrorMessage = "Выберите категорию"; return; }

            // принимаем и «1999.90», и «1999,90»
            string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string priceText = (PriceText ?? "").Trim().Replace(".", sep).Replace(",", sep);
            decimal price;
            if (!decimal.TryParse(priceText, out price) || price < 0 || price > 99999999.99m)
            {
                ErrorMessage = "Цена должна быть числом от 0 (например, 1999,90)";
                return;
            }

            int stock;
            if (!int.TryParse((StockText ?? "").Trim(), out stock) || stock < 0)
            {
                ErrorMessage = "Остаток должен быть целым числом от 0";
                return;
            }

            try
            {
                using (var db = new shop_pcEntities())
                {
                    if (db.Products.Any(x => x.Sku == sku && x.Id != _productId))
                    {
                        ErrorMessage = "Товар с таким артикулом уже есть";
                        return;
                    }

                    Product product;
                    if (_productId == 0)
                    {
                        product = new Product { IsActive = true };
                        db.Products.Add(product);
                    }
                    else
                    {
                        product = db.Products.Find(_productId);
                        if (product == null) { ErrorMessage = "Товар не найден"; return; }
                        // версия строки на момент открытия окна, для проверки «не изменил ли кто-то ещё»
                        db.Entry(product).Property(x => x.RowVersion).OriginalValue = _rowVersion;
                    }

                    product.Sku = sku;
                    product.Name = name;
                    product.CategoryId = SelectedCategory.Id;
                    product.Price = price;
                    product.StockQuantity = stock;
                    product.Specifications = string.IsNullOrWhiteSpace(Specifications)
                        ? null : Specifications.Trim();

                    db.SaveChanges();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                ErrorMessage = "Товар уже изменил другой пользователь. Закройте окно и откройте заново.";
                return;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Не удалось сохранить: " + ex.Message;
                return;
            }

            CloseRequested?.Invoke(true);
        }
    }
}