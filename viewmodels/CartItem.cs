using System;
using System.Windows.Input;
using shop_pc.db;
using shop_pc.helpers;

namespace shop_pc.viewmodels
{
    public class CartItem : ViewModelBase
    {
        private readonly Action _changed;
        private int _quantity;
        private decimal _price;

        public int ProductId { get; private set; }
        public string Sku { get; private set; }
        public string Name { get; private set; }
        public int MaxQuantity { get; private set; }   // остаток на момент добавления

        public ICommand IncreaseCommand { get; private set; }
        public ICommand DecreaseCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }

        public CartItem(Product product, Action changed, Action<CartItem> remove)
        {
            ProductId = product.Id;
            Sku = product.Sku;
            Name = product.Name;
            MaxQuantity = product.StockQuantity;
            _price = product.Price;
            _quantity = 1;
            _changed = changed;

            IncreaseCommand = new RelayCommand(p => Quantity++, p => Quantity < MaxQuantity);
            DecreaseCommand = new RelayCommand(p => Quantity--, p => Quantity > 1);
            RemoveCommand = new RelayCommand(p => remove(this));
        }

        public int Quantity
        {
            get { return _quantity; }
            set
            {
                if (SetField(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                    _changed();
                }
            }
        }

        public decimal Price
        {
            get { return _price; }
            set
            {
                if (SetField(ref _price, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                    _changed();
                }
            }
        }

        public decimal LineTotal
        {
            get { return Price * Quantity; }
        }
    }
}