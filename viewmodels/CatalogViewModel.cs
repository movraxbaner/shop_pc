using shop_pc.db;
using shop_pc.helpers;
using shop_pc.services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows.Input;
using shop_pc.window;

namespace shop_pc.viewmodels
{
    public class CatalogViewModel : ViewModelBase
    {
        private const int PageSize = 10;

        private string _searchText = "";
        private bool _onlyLowStock;
        private string _selectedSort;
        private int _pageNumber = 1;
        private int _totalPages = 1;
        private int _totalCount;
        private List<Product> _products = new List<Product>();
        private Product _selectedProduct;
        private Category _selectedCategory;
        public List<Category> Categories { get; private set; }

        public CatalogViewModel()
        {
            SortOptions = new[] {
                "Название (А–Я)",
                "Цена: дешевле сначала",
                "Цена: дороже сначала",
                "Остаток: меньше сначала"
            };
            _selectedSort = SortOptions[0];

            using (var db = new shop_pcEntities())
            {
                Categories = db.Categories.OrderBy(c => c.Name).ToList();
            }
            Categories.Insert(0, new Category { Id = 0, Name = "Все категории" });
            _selectedCategory = Categories[0];

            PrevPageCommand = new RelayCommand(p => { PageNumber--; Load(); }, p => PageNumber > 1);
            NextPageCommand = new RelayCommand(p => { PageNumber++; Load(); }, p => PageNumber < TotalPages);
            ResetCommand = new RelayCommand(p => Reset());

            AddCommand = new RelayCommand(p => OpenEditor(null), p => CanEdit);
            EditCommand = new RelayCommand(p => OpenEditor(SelectedProduct), p => CanEdit && SelectedProduct != null);
            WriteOffCommand = new RelayCommand(p => OpenWriteOff(SelectedProduct), p => CanEdit && SelectedProduct != null);

            Load();
        }

        // ---------- Данные для интерфейса ----------

        public bool CanEdit { get { return AuthService.CanEditCatalog; } }

        public ICommand AddCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand WriteOffCommand { get; private set; }

        public string[] SortOptions { get; private set; }

        public Category SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                if (SetField(ref _selectedCategory, value)) { PageNumber = 1; Load(); }
            }
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
            set
            {
                if (SetField(ref _searchText, value)) { PageNumber = 1; Load(); }
            }
        }

        public bool OnlyLowStock
        {
            get { return _onlyLowStock; }
            set
            {
                if (SetField(ref _onlyLowStock, value)) { PageNumber = 1; Load(); }
            }
        }

        public string SelectedSort
        {
            get { return _selectedSort; }
            set
            {
                if (SetField(ref _selectedSort, value)) { PageNumber = 1; Load(); }
            }
        }

        public int PageNumber
        {
            get { return _pageNumber; }
            private set { SetField(ref _pageNumber, value); OnPropertyChanged(nameof(PageInfo)); }
        }

        public int TotalPages
        {
            get { return _totalPages; }
            private set { SetField(ref _totalPages, value); OnPropertyChanged(nameof(PageInfo)); }
        }

        public int TotalCount
        {
            get { return _totalCount; }
            private set { SetField(ref _totalCount, value); OnPropertyChanged(nameof(PageInfo)); }
        }

        public string PageInfo
        {
            get { return "Страница " + PageNumber + " из " + TotalPages + "  (товаров: " + TotalCount + ")"; }
        }

        // ---------- Команды ----------

        public ICommand PrevPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        public ICommand ResetCommand { get; private set; }

        // ---------- Логика ----------

        private void OpenEditor(Product product)
        {
            if (!CanEdit) return;
            if (DialogHelper.ShowDialog(new ProductEditWindow(product))) Load();
        }

        private void OpenWriteOff(Product product)
        {
            if (!CanEdit || product == null) return;
            if (DialogHelper.ShowDialog(new WriteOffWindow(product))) Load();
        }

        private void Reset()
        {
            _searchText = "";
            _onlyLowStock = false;
            _selectedSort = SortOptions[0];
            PageNumber = 1;
            _selectedCategory = Categories[0];
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(OnlyLowStock));
            OnPropertyChanged(nameof(SelectedSort));
            OnPropertyChanged(nameof(SelectedCategory));
            Load();
        }

        private void Load()
        {
            using (var db = new shop_pcEntities())
            {
                IQueryable<Product> query = db.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive);

                string text = (SearchText ?? "").Trim();
                if (text != "")
                {
                    query = query.Where(p => p.Name.Contains(text)
                                          || p.Sku.Contains(text)
                                          || p.Specifications.Contains(text));
                }

                if (OnlyLowStock)
                {
                    query = query.Where(p => p.StockQuantity < 3);
                }

                if (SelectedCategory != null && SelectedCategory.Id != 0)
                {
                    int categoryId = SelectedCategory.Id;
                    query = query.Where(p => p.CategoryId == categoryId);
                }

                TotalCount = query.Count();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (PageNumber > TotalPages) PageNumber = TotalPages;

                IOrderedQueryable<Product> ordered;
                switch (SelectedSort)
                {
                    case "Цена: дешевле сначала":
                        ordered = query.OrderBy(p => p.Price); break;
                    case "Цена: дороже сначала":
                        ordered = query.OrderByDescending(p => p.Price); break;
                    case "Остаток: меньше сначала":
                        ordered = query.OrderBy(p => p.StockQuantity); break;
                    default:
                        ordered = query.OrderBy(p => p.Name); break;
                }

                Products = ordered
                    .ThenBy(p => p.Id)                       // чтобы страницы не «прыгали»
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
        }
    }
}