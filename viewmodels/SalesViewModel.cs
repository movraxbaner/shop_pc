using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows.Input;
using shop_pc.db;
using shop_pc.helpers;
using shop_pc.services;
using shop_pc.window;

namespace shop_pc.viewmodels
{
    public class SalesViewModel : ViewModelBase
    {
        private string _searchText = "";
        private string _errorMessage = "";
        private List<Product> _products = new List<Product>();
        private Product _selectedProduct;

        public SalesViewModel()
        {
            Cart = new ObservableCollection<CartItem>();
            Cart.CollectionChanged += (s, e) => RecalcTotals();

            AddToCartCommand = new RelayCommand(p => AddToCart(), p => SelectedProduct != null);
            ClearCommand = new RelayCommand(p => { Cart.Clear(); ErrorMessage = ""; }, p => Cart.Count > 0);
            CheckoutCommand = new RelayCommand(p => Checkout(), p => Cart.Count > 0);

            Load();
        }

        // ---------- Данные для интерфейса ----------

        public ObservableCollection<CartItem> Cart { get; private set; }

        public decimal Total
        {
            get { return Cart.Sum(c => c.LineTotal); }
        }

        public List<Product> Products
        {
            get { return _products; }
            private set { SetField(ref _products, value); }
        }

        public Product SelectedProduct
        {
            get { return _selectedProduct; }
            set { SetField(ref _selectedProduct, value); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { if (SetField(ref _searchText, value)) Load(); }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetField(ref _errorMessage, value); }
        }

        public ICommand AddToCartCommand { get; private set; }
        public ICommand ClearCommand { get; private set; }
        public ICommand CheckoutCommand { get; private set; }

        // ---------- Логика ----------

        private void RecalcTotals()
        {
            OnPropertyChanged(nameof(Total));
        }

        private void RemoveFromCart(CartItem item)
        {
            Cart.Remove(item);
        }

        private void Load()
        {
            string text = (SearchText ?? "").Trim();

            using (var db = new shop_pcEntities())
            {
                IQueryable<Product> query = db.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.StockQuantity > 0);

                if (text != "")
                {
                    query = query.Where(p => p.Name.Contains(text)
                                          || p.Sku.Contains(text)
                                          || p.Specifications.Contains(text));
                }

                Products = query.OrderBy(p => p.Name).Take(50).ToList();
            }
        }

        private void AddToCart()
        {
            ErrorMessage = "";
            Product product = SelectedProduct;
            if (product == null) return;

            CartItem existing = Cart.FirstOrDefault(c => c.ProductId == product.Id);
            if (existing != null)
            {
                if (existing.Quantity >= existing.MaxQuantity)
                {
                    ErrorMessage = "На складе больше нет: " + product.Name;
                    return;
                }
                existing.Quantity++;
            }
            else
            {
                Cart.Add(new CartItem(product, RecalcTotals, RemoveFromCart));
            }
        }

        private void Checkout()
        {
            ErrorMessage = "";
            if (Cart.Count == 0 || !AuthService.CanSell) return;

            string receipt;
            try
            {
                receipt = ProcessSale();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Не удалось оформить продажу: " + ex.Message;
                return;
            }

            if (receipt == null) return;     // причина уже записана в ErrorMessage

            Cart.Clear();
            Load();                          // остатки изменились
            DialogHelper.ShowDialog(new ReceiptWindow(receipt));
        }

        // Вся продажа в одной транзакции: либо всё, либо ничего.
        // Возвращает текст чека или null, если продажу оформить нельзя.
        private string ProcessSale()
        {
            List<CartItem> items = Cart.ToList();
            List<int> ids = items.Select(c => c.ProductId).ToList();

            using (var db = new shop_pcEntities())
            using (var tx = db.Database.BeginTransaction())
            {

                // 1. Проверяем актуальные цены
                Dictionary<int, decimal> prices = db.Products
                    .Where(p => ids.Contains(p.Id) && p.IsActive)
                    .Select(p => new { p.Id, p.Price })
                    .ToList()
                    .ToDictionary(x => x.Id, x => x.Price);

                bool priceChanged = false;
                foreach (CartItem item in items)
                {
                    decimal current;
                    if (!prices.TryGetValue(item.ProductId, out current))
                    {
                        ErrorMessage = "Товар «" + item.Name + "» больше недоступен. Удалите его из корзины.";
                        return null;
                    }
                    if (current != item.Price)
                    {
                        item.Price = current;
                        priceChanged = true;
                    }
                }
                if (priceChanged)
                {
                    ErrorMessage = "Цены некоторых товаров изменились. Проверьте сумму и оформите продажу ещё раз.";
                    return null;
                }

                // 2. Списываем остатки. Условие StockQuantity >= количество проверяется
                //    прямо в базе, поэтому два продавца не смогут продать один и тот же последний товар.
                foreach (CartItem item in items)
                {
                    int affected = db.Database.ExecuteSqlCommand(
                        "UPDATE Products SET StockQuantity = StockQuantity - {0} " +
                        "WHERE Id = {1} AND StockQuantity >= {0}",
                        item.Quantity, item.ProductId);

                    if (affected == 0)
                    {
                        ErrorMessage = "Недостаточно товара «" + item.Name + "» на складе";
                        return null;         // выход без Commit откатывает всё
                    }
                }

                // 3. Заказ и позиции
                var order = new Order
                {
                    OrderDate = DateTime.Now,
                    UserId = AuthService.CurrentUser.Id,
                    Status = "Проведён",
                    TotalAmount = items.Sum(c => c.LineTotal)
                };
                foreach (CartItem item in items)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    });
                }
                db.Orders.Add(order);
                db.SaveChanges();

                tx.Commit();

                return BuildReceipt(order, items);
            }
        }

        private static string BuildReceipt(Order order, List<CartItem> items)
        {
            string line = new string('-', 44);
            var sb = new StringBuilder();
            sb.AppendLine("IT-Expert");
            sb.AppendLine("Чек № " + order.Id);
            sb.AppendLine("Дата: " + order.OrderDate.ToString("dd.MM.yyyy HH:mm"));
            sb.AppendLine("Продавец: " + AuthService.CurrentUser.FullName);
            sb.AppendLine(line);
            foreach (CartItem item in items)
            {
                sb.AppendLine(item.Name);
                sb.AppendLine("   " + item.Quantity + " x " + item.Price.ToString("N2")
                              + " = " + item.LineTotal.ToString("N2"));
            }
            sb.AppendLine(line);
            sb.AppendLine("ИТОГО: " + order.TotalAmount.ToString("N2") + " ₽");
            return sb.ToString();
        }
    }
}