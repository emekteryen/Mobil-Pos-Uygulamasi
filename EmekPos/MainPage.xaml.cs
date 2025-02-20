using ZXing.Net.Maui.Controls;
using ZXing.Net.Maui;
using System;


namespace EmekPos
{
    public partial class MainPage : ContentPage
    {
        private bool isBarcodeReadingInProgress = false;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
            DisableCameraView();
        }

        private void OpenCamera_Clicked(object sender, EventArgs e)
        {
            if (barcodeView.IsVisible==false) {EnableCameraView();}
            else if (barcodeView.IsVisible == true) { DisableCameraView();}
        }
        

       

        private void DisableCameraView()
        {
            barcodeView.IsVisible = false;
            barcodeView.IsDetecting = false;
            BarcodeViewLayout.IsVisible = false;
        }
        private void EnableCameraView()
        {
            barcodeView.IsVisible = true;
            barcodeView.IsDetecting = true;
            BarcodeViewLayout.IsVisible = true;
        }

        private async void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (isBarcodeReadingInProgress) return;

            isBarcodeReadingInProgress = true;

            try
            {
                if (e.Results.Any())
                {
                    foreach (var barcode in e.Results)
                    {
                        if (BindingContext is MainViewModel viewModel)
                        {
                            await viewModel.AddBarcodeAsync(barcode.Value);
                        }
                    }
                }
            }
            finally
            {
                isBarcodeReadingInProgress = false;
                DisableCameraView();
            }
        }
        /*private async void LoadProducts()
        {
            if (BindingContext is MainViewModel viewModel)
            {
                var products = await viewModel.GetProductsFromDatabase();
                ProductGrid.Children.Clear();
                ProductGrid.RowDefinitions.Clear();

                int columnCount = 3; // 3 sütunlu olacak
                int row = 0, col = 0;

                foreach (var product in products)
                {
                    if (col == columnCount)
                    {
                        col = 0;
                        row++;
                    }

                    Button productButton = new Button
                    {
                        Text = product.ProductName,
                        Command = new Command(() => viewModel.AddProductToCart(product))
                    };

                    ProductGrid.Children.Add(productButton, col, row);
                    col++;
                }
            }
        }*/

        private async void CompleteSaleButton_Clicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel viewModel)
            {
                try
                {
                    await viewModel.CompleteSaleAsync();
                    await DisplayAlert("Başarılı", "Satış işlemi başarıyla tamamlandı.", "Tamam");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hata", ex.Message, "Tamam");
                }
            }
        }

    }
}
