using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReadCity.Models
{
    public class ProductItem
    {
        public int Id { get; set; }
        public string Article { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public string ManufacturerName { get; set; }
        public string SupplierName { get; set; }
        public decimal Price { get; set; }
        public string UnitName { get; set; }
        public int StockQuantity { get; set; }
        public int Discount { get; set; }
        public string Photo { get; set; }

        //итоговая цена с учётом скидки
        public decimal FinalPrice => Math.Round(Price * (100 - Discount) / 100m, 2);

        //оформление цены при наличии скидки
        public bool HasDiscount => Discount > 0;
        public Visibility DiscountPriceVisibility => HasDiscount ? Visibility.Visible : Visibility.Collapsed;
        public TextDecorationCollection OldPriceDecorations => HasDiscount ? TextDecorations.Strikethrough : null;
        public Brush OldPriceForeground => HasDiscount ? Brushes.Red : Brushes.Black;

        //цвет фона карточки
        public Brush CardBackground
        {
            get
            {
                if (StockQuantity == 0)
                    return Brushes.LightGray;
                if (Discount > 25)
                    return (SolidColorBrush)new BrushConverter().ConvertFrom("#23E1EF");
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#ABCFCE");
            }
        }

        //загрузка картинки или заглушки
        public BitmapImage ImageSource
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = null;

                //картинка
                if (!string.IsNullOrWhiteSpace(Photo))
                {
                    string customPath = Path.Combine(baseDir, "Images", Photo);
                    if (File.Exists(customPath))
                        filePath = customPath;
                }

                //заглушка
                if (filePath == null)
                {
                    string p1 = Path.Combine(baseDir, "Images", "picture.png");
                    string p2 = Path.Combine(baseDir, "Resources", "picture.png");
                    if (File.Exists(p1)) filePath = p1;
                    else if (File.Exists(p2)) filePath = p2;
                }

                if (filePath == null)
                    return null;

                //подгрузка фото товара во время работы приложения
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
        }
    }
}
