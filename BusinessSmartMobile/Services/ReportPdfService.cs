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
        private float PageWidth = 842; // A4 Landscape genişlik
        private float PageHeight = 595; // A4 Landscape yükseklik
        private const float MarginLeft = 30;
        private const float MarginRight = 30;
        private const float MarginTop = 40;
        private const float MarginBottom = 40;
        private static readonly SKColor PrimaryColor = SKColor.Parse("#2c3e50");
        private static readonly SKColor TextGray = SKColor.Parse("#6c757d");

        public byte[] CreateReportPdf<T>(List<T> data, TbParamGenel paramGenel, Dictionary<string, string> columns, string reportName = "")
        {
            // Kolon sayısına göre yönlendirme karar ver
            bool useLandscape = columns.Count > 4;
            
            if (useLandscape)
            {
                PageWidth = 842;  // A4 Landscape
                PageHeight = 595;
            }
            else
            {
                PageWidth = 595;  // A4 Portrait
                PageHeight = 842;
            }

            using var stream = new MemoryStream();
            document = SKDocument.CreatePdf(stream);
            canvas = document.BeginPage(PageWidth, PageHeight);
            currentY = MarginTop;

            AddHeader(paramGenel, reportName);
            AddReportTable(data, columns);
            
            document.EndPage();
            document.Close();
            return stream.ToArray();
        }

        private void AddHeader(TbParamGenel paramGenel, string reportName = "")
        {
            using var companyPaint = new SKPaint { Color = PrimaryColor, TextSize = 18, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), IsAntialias = true };
            using var reportNamePaint = new SKPaint { Color = PrimaryColor, TextSize = 14, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), IsAntialias = true };
            using var dateLabelPaint = new SKPaint { Color = TextGray, TextSize = 9, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), IsAntialias = true };
            using var datePaint = new SKPaint { Color = TextGray, TextSize = 9, Typeface = SKTypeface.FromFamilyName("Arial"), IsAntialias = true };
            
            // Rapor Tarihi (sağ üstte)
            string dateLabelText = "Rapor Tarihi: ";
            string dateText = $"{DateTime.Now:dd.MM.yyyy}";
            float dateLabelWidth = dateLabelPaint.MeasureText(dateLabelText);
            float dateWidth = datePaint.MeasureText(dateText);
            float totalDateWidth = dateLabelWidth + dateWidth;
            canvas.DrawText(dateLabelText, PageWidth - MarginRight - totalDateWidth, currentY, dateLabelPaint);
            canvas.DrawText(dateText, PageWidth - MarginRight - dateWidth, currentY, datePaint);
            
            currentY += 25;
            
            // Firma Adı (ortalanmış, büyük, kalın)
            string companyName = paramGenel.sFirmaAdi;
            float companyWidth = companyPaint.MeasureText(companyName);
            float companyX = (PageWidth - companyWidth) / 2;
            canvas.DrawText(companyName, companyX, currentY, companyPaint);
            
            currentY += 22;
            
            // Rapor İsmi (ortalanmış, firma adından biraz daha küçük)
            if (!string.IsNullOrEmpty(reportName))
            {
                float reportNameWidth = reportNamePaint.MeasureText(reportName);
                float reportNameX = (PageWidth - reportNameWidth) / 2;
                canvas.DrawText(reportName, reportNameX, currentY, reportNamePaint);
                currentY += 20;
            }
            else
            {
                currentY += 10;
            }
        }

        private void AddReportTable<T>(List<T> data, Dictionary<string, string> columns)
        {
            if (data == null || !data.Any()) return;

            using var headerPaint = new SKPaint { Color = SKColors.White, TextSize = 8, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), IsAntialias = true };
            using var cellPaint = new SKPaint { Color = TextGray, TextSize = 7, Typeface = SKTypeface.FromFamilyName("Arial"), IsAntialias = true };
            using var headerBgPaint = new SKPaint { Color = PrimaryColor, Style = SKPaintStyle.Fill };
            using var borderPaint = new SKPaint { Color = SKColor.Parse("#dee2e6"), Style = SKPaintStyle.Stroke, StrokeWidth = 0.5f };

            float contentWidth = PageWidth - MarginLeft - MarginRight;
            float columnWidth = contentWidth / columns.Count;
            float rowHeight = 18;

            // Header
            var headerRect = new SKRect(MarginLeft, currentY, PageWidth - MarginRight, currentY + rowHeight);
            canvas.DrawRect(headerRect, headerBgPaint);
            canvas.DrawRect(headerRect, borderPaint);

            float xPos = MarginLeft;
            foreach (var column in columns)
            {
                string headerText = column.Value;
                // Metni ortala
                float textWidth = headerPaint.MeasureText(headerText);
                float textX = xPos + (columnWidth - textWidth) / 2;
                canvas.DrawText(headerText, textX, currentY + 12, headerPaint);
                canvas.DrawLine(xPos, currentY, xPos, currentY + rowHeight, borderPaint);
                xPos += columnWidth;
            }
            canvas.DrawLine(PageWidth - MarginRight, currentY, PageWidth - MarginRight, currentY + rowHeight, borderPaint);
            currentY += rowHeight;

            // Data rows
            bool alternate = false;
            foreach (var item in data)
            {
                if (currentY > PageHeight - MarginBottom - 30)
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
                foreach (var column in columns)
                {
                    var property = typeof(T).GetProperty(column.Key);
                    if (property != null)
                    {
                        var value = property.GetValue(item);
                        string text = FormatValue(value, property.Name);
                        
                        // Metni kırp eğer çok uzunsa
                        float maxWidth = columnWidth - 4;
                        text = TruncateText(text, cellPaint, maxWidth);
                        
                        float textX = xPos + 2;
                        
                        // Sayıları sağa hizala
                        if (value != null && IsNumericType(value.GetType()))
                        {
                            float textWidth = cellPaint.MeasureText(text);
                            textX = xPos + columnWidth - textWidth - 2;
                        }
                        
                        canvas.DrawText(text, textX, currentY + 11, cellPaint);
                    }
                    canvas.DrawLine(xPos, currentY, xPos, currentY + rowHeight, borderPaint);
                    xPos += columnWidth;
                }
                canvas.DrawLine(PageWidth - MarginRight, currentY, PageWidth - MarginRight, currentY + rowHeight, borderPaint);
                currentY += rowHeight;
                alternate = !alternate;
            }
        }

        private bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(double) || type == typeof(decimal) || 
                   type == typeof(float) || type == typeof(long) || type == typeof(short);
        }

        private string TruncateText(string text, SKPaint paint, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            float textWidth = paint.MeasureText(text);
            if (textWidth <= maxWidth) return text;
            
            // Metni kısalt
            int length = text.Length;
            while (length > 0 && paint.MeasureText(text.Substring(0, length) + "...") > maxWidth)
            {
                length--;
            }
            
            return length > 0 ? text.Substring(0, length) + "..." : "";
        }

        private string FormatValue(object value, string propertyName)
        {
            if (value == null) return "";
            
            // DateTime kontrolü
            if (value is DateTime dt)
                return dt.ToString("dd.MM.yyyy");
            
            // String olarak gelen tarih kontrolü (dteSiparisTarihi, dteTarih vb.)
            string lowerProp = propertyName.ToLower();
            if ((lowerProp.Contains("tarih") || lowerProp.Contains("date")) && value is string strDate)
            {
                if (!string.IsNullOrEmpty(strDate))
                {
                    try
                    {
                        // Türk kültürü ile parse et
                        var turkishCulture = new System.Globalization.CultureInfo("tr-TR");
                        var date = DateTime.Parse(strDate, turkishCulture);
                        return date.ToString("dd.MM.yyyy");
                    }
                    catch
                    {
                        try
                        {
                            // Eğer Türk kültürü ile olmadıysa, Amerikan formatında dene
                            var date = DateTime.ParseExact(strDate, new[] { "M/d/yyyy HH:mm:ss", "M/d/yyyy", "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy" }, 
                                System.Globalization.CultureInfo.InvariantCulture, 
                                System.Globalization.DateTimeStyles.None);
                            return date.ToString("dd.MM.yyyy");
                        }
                        catch
                        {
                            // Parse başarısız olursa sadece tarih kısmını al (ilk 10 karakter)
                            return strDate.Length >= 10 ? strDate.Substring(0, 10) : strDate;
                        }
                    }
                }
            }
            
            // Önce miktar ve adet kontrolü (TL olmaması için)
            if (lowerProp.Contains("miktar") || lowerProp.Contains("adet") || 
                lowerProp.Contains("mevcut") || lowerProp.Contains("kalan") ||
                lowerProp.Contains("sevkiyat") || lowerProp.Contains("siparis") ||
                lowerProp.Contains("bekleyen") || lowerProp.Contains("koli"))
            {
                if (value is double d)
                    return d.ToString("N0");
                if (value is decimal dec)
                    return dec.ToString("N0");
                if (value is int i)
                    return i.ToString("N0");
                if (value is float f)
                    return f.ToString("N0");
            }
            
            // Para formatları (sadece bunlara TL)
            if (lowerProp.Contains("tutar") || lowerProp.Contains("fiyat") ||
                lowerProp.Contains("iskonto") || lowerProp.Contains("ciro") ||
                lowerProp.Contains("bakiye") || lowerProp.Contains("risk") ||
                lowerProp.Contains("kar") || lowerProp.Contains("zarar"))
            {
                if (value is double d)
                    return d.ToString("N0") + " TL";
                if (value is decimal dec)
                    return dec.ToString("N0") + " TL";
                if (value is float f)
                    return f.ToString("N0") + " TL";
            }
            
            // Diğer sayısal değerler (TL olmadan)
            if (value is double d2)
                return d2.ToString("N0");
            if (value is decimal dec2)
                return dec2.ToString("N0");
            if (value is int i2)
                return i2.ToString("N0");
            if (value is float f2)
                return f2.ToString("N0");
            
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
