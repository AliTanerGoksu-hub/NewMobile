using SkiaSharp;
using System.Reflection;
using BusinessSmartMobile.Models;

namespace BusinessSmartMobile.Services
{
    public class ReportPdfService
    {
        private SKDocument document;
        private SKCanvas canvas;
        private float currentY;
        private const float PageWidth = 595;
        private const float PageHeight = 842;
        private const float MarginLeft = 40;
        private const float MarginRight = 40;
        private const float MarginTop = 40;
        private static readonly SKColor PrimaryColor = SKColor.Parse("#2c3e50");
        private static readonly SKColor TextGray = SKColor.Parse("#6c757d");

        public byte[] CreateReportPdf<T>(List<T> data, TbParamGenel paramGenel)
        {
            using var stream = new MemoryStream();
            document = SKDocument.CreatePdf(stream);
            canvas = document.BeginPage(PageWidth, PageHeight);
            currentY = MarginTop;

            AddHeader(paramGenel);
            AddReportTable(data);
            
            document.EndPage();
            document.Close();
            return stream.ToArray();
        }

        private void AddHeader(TbParamGenel paramGenel)
        {
            using var titlePaint = new SKPaint { Color = PrimaryColor, TextSize = 13, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), IsAntialias = true };
            using var infoPaint = new SKPaint { Color = TextGray, TextSize = 9, Typeface = SKTypeface.FromFamilyName("Arial"), IsAntialias = true };
            using var companyPaint = new SKPaint { Color = PrimaryColor, TextSize = 11, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), IsAntialias = true };
            
            string dateText = $"Tarih: {DateTime.Now:dd/MM/yyyy}";
            float dateWidth = infoPaint.MeasureText(dateText);
            canvas.DrawText(dateText, PageWidth - MarginRight - dateWidth, currentY, infoPaint);
            currentY += 20;
            canvas.DrawText(paramGenel.sFirmaAdi, MarginLeft, currentY, companyPaint);
            currentY += 15;
            canvas.DrawText($"Adres: {paramGenel.sFirmaAdresi}", MarginLeft, currentY, infoPaint);
            string phoneText = $"Telefon: {paramGenel.sTelefon1}";
            float phoneWidth = infoPaint.MeasureText(phoneText);
            canvas.DrawText(phoneText, PageWidth - MarginRight - phoneWidth, currentY, infoPaint);
            currentY += 20;
        }

        private void AddReportTable<T>(List<T> data)
        {
            if (data == null || !data.Any()) return;

            using var headerPaint = new SKPaint { Color = SKColors.White, TextSize = 9, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), IsAntialias = true };
            using var cellPaint = new SKPaint { Color = TextGray, TextSize = 8, Typeface = SKTypeface.FromFamilyName("Arial"), IsAntialias = true };
            using var headerBgPaint = new SKPaint { Color = PrimaryColor, Style = SKPaintStyle.Fill };
            using var borderPaint = new SKPaint { Color = SKColor.Parse("#dee2e6"), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

            var properties = typeof(T).GetProperties()
                .Where(p => p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(decimal) || p.PropertyType == typeof(double) || p.PropertyType == typeof(DateTime))
                .ToList();

            float contentWidth = PageWidth - MarginLeft - MarginRight;
            float columnWidth = contentWidth / properties.Count;
            float rowHeight = 20;

            // Header
            var headerRect = new SKRect(MarginLeft, currentY, PageWidth - MarginRight, currentY + rowHeight);
            canvas.DrawRect(headerRect, headerBgPaint);
            canvas.DrawRect(headerRect, borderPaint);

            float xPos = MarginLeft;
            foreach (var prop in properties)
            {
                string headerText = GetDisplayName(prop.Name);
                canvas.DrawText(headerText, xPos + 5, currentY + 14, headerPaint);
                canvas.DrawLine(xPos, currentY, xPos, currentY + rowHeight, borderPaint);
                xPos += columnWidth;
            }
            canvas.DrawLine(PageWidth - MarginRight, currentY, PageWidth - MarginRight, currentY + rowHeight, borderPaint);
            currentY += rowHeight;

            // Data rows
            bool alternate = false;
            foreach (var item in data)
            {
                if (currentY > PageHeight - 100)
                {
                    document.EndPage();
                    canvas = document.BeginPage(PageWidth, PageHeight);
                    currentY = MarginTop;
                }

                if (alternate)
                {
                    var rowBgRect = new SKRect(MarginLeft, currentY, PageWidth - MarginRight, currentY + rowHeight);
                    canvas.DrawRect(rowBgRect, new SKPaint { Color = SKColor.Parse("#f8f9fa"), Style = SKPaintStyle.Fill });
                }

                var rowRect = new SKRect(MarginLeft, currentY, PageWidth - MarginRight, currentY + rowHeight);
                canvas.DrawRect(rowRect, borderPaint);

                xPos = MarginLeft;
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item);
                    string text = FormatValue(value);
                    
                    text = text?.Replace("□", "").Replace("☐", "").Replace("▢", "").Replace("◻", "").Trim() ?? "";
                    
                    float textWidth = cellPaint.MeasureText(text);
                    float textX = xPos + 5;
                    
                    if (value != null && (value.GetType() == typeof(int) || value.GetType() == typeof(double) || value.GetType() == typeof(decimal) || value.GetType() == typeof(float)))
                    {
                        textX = xPos + columnWidth - textWidth - 5;
                    }
                    
                    canvas.DrawText(text, textX, currentY + 14, cellPaint);
                    canvas.DrawLine(xPos, currentY, xPos, currentY + rowHeight, borderPaint);
                    xPos += columnWidth;
                }
                canvas.DrawLine(PageWidth - MarginRight, currentY, PageWidth - MarginRight, currentY + rowHeight, borderPaint);
                currentY += rowHeight;
                alternate = !alternate;
            }
        }

        private string GetDisplayName(string propertyName)
        {
            var displayNames = new Dictionary<string, string>
            {
                { "sKodu", "Kodu" },
                { "sAciklama", "Açıklama" },
                { "sStokAciklama", "Stok Adı" },
                { "sFirmaAciklama", "Firma" },
                { "miktar", "Miktar" },
                { "mevcut", "Mevcut" },
                { "Bekleyen", "Bekleyen" },
                { "siparis", "Sipariş" },
                { "tutar", "Tutar" },
                { "iskonto", "İskonto" },
                { "netCiro", "Net Ciro" },
                { "lNetTutar", "Net Tutar" },
                { "Fiyat", "Fiyat" },
                { "periyod", "Periyod" },
                { "lMiktari", "Miktar" },
                { "lSevkiyat", "Sevkiyat" },
                { "lKalan", "Kalan" },
                { "nBirimCarpan", "Koli İçi" },
                { "lMevcut", "Mevcut" }
            };

            return displayNames.ContainsKey(propertyName) ? displayNames[propertyName] : propertyName;
        }

        private string FormatValue(object value)
        {
            if (value == null) return "";
            
            if (value is DateTime dt)
                return dt.ToString("dd.MM.yyyy");
            
            if (value is double d)
                return d.ToString("N0") + " TL";
            
            if (value is decimal dec)
                return dec.ToString("N0") + " TL";
            
            if (value is float f)
                return f.ToString("N0") + " TL";
            
            return value.ToString() ?? "";
        }

        public void SavePdfToFile(byte[] pdfBytes, string fileName)
        {
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.WriteAllBytes(filePath, pdfBytes);
            Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(filePath) });
        }
    }
}
