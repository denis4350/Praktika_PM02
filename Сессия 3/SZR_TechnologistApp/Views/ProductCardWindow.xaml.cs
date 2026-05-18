using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class ProductCardWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly ProductDto _product;

        public ProductCardWindow(ApiService apiService, ProductDto product)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _product = product ?? throw new ArgumentNullException(nameof(product));

            LoadProductInfo();
            LoadProductRecipes();
        }

        private void LoadProductInfo()
        {
            ProductCodeText.Text = _product.Code ?? "—";
            ProductNameText.Text = _product.Name ?? "—";
            ProductTypeText.Text = $"{_product.ProductType ?? "—"} / {_product.Form ?? "—"}";
            ProductStatusText.Text = _product.Status ?? "—";
            ProductCreatedAtText.Text = _product.CreatedAt.ToString("dd.MM.yyyy");
            HeaderProductCodeText.Text = _product.Code ?? "—";
            HeaderSubtitleText.Text = $"Рецептуры, технологические карты и партии продукта «{_product.Name}»";
        }

        private async void LoadProductRecipes()
        {
            try
            {
                var result = await _apiService.GetRecipesAsync(productId: _product.Id);
                RecipesListView.ItemsSource = new ObservableCollection<RecipeDto>(result.Items);
                ItemsCountText.Text = $"{result.Pagination.TotalCount} записей";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки рецептур: " + ex.Message);
            }
        }

        private async void LoadProductTechCards()
        {
            try
            {
                var result = await _apiService.GetTechCardsAsync(productId: _product.Id);
                TechCardsListView.ItemsSource = new ObservableCollection<TechCardDto>(result.Items);
                ItemsCountText.Text = $"{result.Pagination.TotalCount} записей";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки технологических карт: " + ex.Message);
            }
        }

        private async void LoadProductBatches()
        {
            try
            {
                // используем batches из ProductionController, либо загружаем все и фильтруем
                var result = await _apiService.GetBatchesAsync();
                var filtered = result.Items.Where(b => b.ProductId == _product.Id).ToList();
                BatchesListView.ItemsSource = new ObservableCollection<ProductionBatchDto>(filtered);
                ItemsCountText.Text = $"{filtered.Count} записей";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки партий: " + ex.Message);
            }
        }

        private void RecipesTab_Click(object sender, MouseButtonEventArgs e)
        {
            SwitchTab("recipes");
            LoadProductRecipes();
        }

        private void TechCardsTab_Click(object sender, MouseButtonEventArgs e)
        {
            SwitchTab("techcards");
            LoadProductTechCards();
        }

        private void BatchesTab_Click(object sender, MouseButtonEventArgs e)
        {
            SwitchTab("batches");
            LoadProductBatches();
        }

        private void SwitchTab(string tab)
        {
            RecipesListView.Visibility = tab == "recipes" ? Visibility.Visible : Visibility.Collapsed;
            TechCardsListView.Visibility = tab == "techcards" ? Visibility.Visible : Visibility.Collapsed;
            BatchesListView.Visibility = tab == "batches" ? Visibility.Visible : Visibility.Collapsed;

            TabTitleText.Text = tab == "recipes" ? "Рецептуры продукта" :
                               tab == "techcards" ? "Технологические карты продукта" : "Производственные партии продукта";
        }

        private void OpenRecipe_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is RecipeDto recipe)
            {
                var card = new RecipeCardWindow(_apiService, recipe) { Owner = this };
                // При необходимости можно передать recipe в конструктор или свойство
                card.ShowDialog();
                LoadProductRecipes();
            }
        }

        private void OpenTechCard_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is TechCardDto techCard)
            {
                // Аналогично открыть TechCardCardWindow
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipesListView.Visibility == Visibility.Visible) LoadProductRecipes();
            else if (TechCardsListView.Visibility == Visibility.Visible) LoadProductTechCards();
            else LoadProductBatches();
        }

        private void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            var form = new ProductForm(_apiService, _product) { Owner = this };
            if (form.ShowDialog() == true)
            {
                // обновить данные на форме
                LoadProductInfo();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}