using System;
using System.Data.Entity.Infrastructure;
using System.Windows.Input;
using shop_pc.db;
using shop_pc.helpers;
using shop_pc.services;

namespace shop_pc.viewmodels
{
    public class WriteOffViewModel : ViewModelBase
    {
        private readonly int _productId;
        private string _errorMessage = "";

        public string ProductInfo { get; private set; }
        public string QuantityText { get; set; }
        public string Reason { get; set; }
        public ICommand SaveCommand { get; private set; }
        public event Action<bool> CloseRequested;

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetField(ref _errorMessage, value); }
        }

        public WriteOffViewModel(Product product)
        {
            _productId = product.Id;
            ProductInfo = product.Name + "\nАртикул: " + product.Sku
                        + "   На складе: " + product.StockQuantity + " шт.";
            QuantityText = "1";
            Reason = "";
            SaveCommand = new RelayCommand(p => Save());
        }

        private void Save()
        {
            ErrorMessage = "";

            int qty;
            if (!int.TryParse((QuantityText ?? "").Trim(), out qty) || qty <= 0)
            {
                ErrorMessage = "Количество должно быть целым числом больше 0";
                return;
            }
            string reason = (Reason ?? "").Trim();
            if (reason == "") { ErrorMessage = "Укажите причину списания"; return; }

            try
            {
                using (var db = new shop_pcEntities())
                {
                    Product product = db.Products.Find(_productId);
                    if (product == null) { ErrorMessage = "Товар не найден"; return; }
                    if (qty > product.StockQuantity)
                    {
                        ErrorMessage = "На складе только " + product.StockQuantity + " шт.";
                        return;
                    }

                    product.StockQuantity -= qty;
                    db.WriteOffs.Add(new WriteOff
                    {
                        ProductId = product.Id,
                        Quantity = qty,
                        Reason = reason,
                        WriteOffDate = DateTime.Now,
                        UserId = AuthService.CurrentUser.Id
                    });

                    db.SaveChanges();   // остаток и запись о списании сохраняются одной транзакцией
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                ErrorMessage = "Остаток уже изменился. Закройте окно и откройте заново.";
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