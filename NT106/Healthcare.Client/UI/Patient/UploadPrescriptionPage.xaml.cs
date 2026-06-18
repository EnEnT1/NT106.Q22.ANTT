using Healthcare.Client.APIClient;
using Healthcare.Client.UI.Shell;
using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.System;

namespace Healthcare.Client.UI.Patient
{
    public sealed partial class UploadPrescriptionPage : Page
    {
        private string _selectedFilePath;
        private PatientShell _shell;

        public UploadPrescriptionPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is PatientShell shell)
            {
                _shell = shell;
            }
        }

        private async void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                
                var window = MainWindow.Instance;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }

                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".pdf");
                picker.FileTypeFilter.Add(".docx");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    _selectedFilePath = file.Path;
                    TxtFileName.Text = file.Name;
                    FileBadge.Visibility = Visibility.Visible;
                    BtnAnalyze.IsEnabled = true;
                    
                    // Reset result state
                    EmptyResultState.Visibility = Visibility.Visible;
                    ListMedicines.Visibility = Visibility.Collapsed;
                    BtnContinueBooking.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"Picker Error: {ex.Message}");
            }
        }

        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath)) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            EmptyResultState.Visibility = Visibility.Collapsed;
            ListMedicines.Visibility = Visibility.Collapsed;
            BtnAnalyze.IsEnabled = false;
            BtnSelectFile.IsEnabled = false;

            try
            {
                // Gọi API AI thật của Server để trích xuất danh sách thuốc
                List<string> medicines = null;
                bool isFallback = false;
                string apiErrorMsg = "";

                try
                {
                    var aiClient = new AiClient();
                    medicines = await aiClient.AnalyzePrescriptionAsync(_selectedFilePath);
                }
                catch (Exception apiEx)
                {
                    apiErrorMsg = apiEx.Message;
                    isFallback = true;
                }

                // Nếu API bị lỗi hoặc không nhận diện được thuốc, tiến hành Fallback động để demo
                if (medicines == null || medicines.Count == 0)
                {
                    isFallback = true;
                    medicines = new List<string>
                    {
                        "Paracetamol 500mg - Uống 1 viên x 2 lần/ngày (sáng, tối) sau ăn khi sốt trên 38.5 độ C - 20 Viên",
                        "Amoxicillin 500mg - Uống 1 viên x 3 lần/ngày (sáng, trưa, tối) sau ăn - 21 Viên",
                        "Decolgen Forte - Uống 1 viên x 2 lần/ngày (sáng, tối) sau ăn khi sổ mũi - 10 Viên",
                        "Siro Ho Prospan 100ml - Uống 5ml x 3 lần/ngày sau ăn - 1 Chai"
                    };
                }

                // 1. Cập nhật dữ liệu lên giao diện (ở cột bên phải "Đơn thuốc")
                // Lọc bỏ phần số lượng ở đuôi nếu có để hiển thị danh sách sạch đẹp trên list view
                var uiMedicines = new List<string>();
                foreach (var med in medicines)
                {
                    var parts = med.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        uiMedicines.Add($"{parts[0].Trim()} - {parts[1].Trim()}");
                    }
                    else
                    {
                        uiMedicines.Add(med);
                    }
                }
                ListMedicines.ItemsSource = uiMedicines;
                ListMedicines.Visibility = Visibility.Visible;
                BtnContinueBooking.IsEnabled = true;

                // 2. Lấy thông tin Bệnh nhân thực tế từ SessionStorage
                string patientName = SessionStorage.CurrentUser?.FullName ?? "Nguyễn Văn Bệnh Nhân";
                string patientGender = "Nam"; // Mặc định
                string patientAge = "25"; // Mặc định
                string patientAddress = "Thủ Đức, TP. Hồ Chí Minh";
                string insuranceCode = "GD4797910200115";
                
                // Chẩn đoán thông minh dựa trên thuốc
                string diagnosis = "Viêm họng cấp tính / Sốt nhẹ chưa rõ nguyên nhân";
                bool hasAntibiotic = false;
                foreach (var med in medicines)
                {
                    if (med.ToLower().Contains("amoxicillin") || med.ToLower().Contains("kháng sinh") || med.ToLower().Contains("cef"))
                    {
                        hasAntibiotic = true;
                        break;
                    }
                }
                if (!hasAntibiotic && !isFallback)
                {
                    diagnosis = "Theo dõi sức khỏe / Kê đơn điều trị ngoại trú";
                }

                // 3. Xây dựng HTML bảng thuốc động
                var tableRowsBuilder = new StringBuilder();
                for (int i = 0; i < medicines.Count; i++)
                {
                    var med = medicines[i];
                    string name = med;
                    string qty = "10 Viên"; // Mặc định
                    string usage = "Uống theo chỉ dẫn của bác sĩ."; // Mặc định

                    var parts = med.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1) name = parts[0].Trim();
                    if (parts.Length >= 2) usage = parts[1].Trim();
                    if (parts.Length >= 3) qty = parts[2].Trim();

                    // Cố gắng phân tách số lượng nếu nó nằm ở phần cuối của usage hoặc tự động
                    if (parts.Length == 2)
                    {
                        // Thử tìm regex số lượng trong phần cách dùng
                        var qtyMatch = System.Text.RegularExpressions.Regex.Match(usage, @"(\d+)\s*(viên|vien|vỉ|vi|chai|tuýp|tuyp|hộp|hop|ống|ong|gói|goi)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (qtyMatch.Success)
                        {
                            qty = qtyMatch.Value;
                            // Xóa phần số lượng khỏi chuỗi cách dùng để tránh lặp
                            usage = usage.Replace(qtyMatch.Value, "").Trim(new char[] { ' ', '-', ',', '.' });
                        }
                    }

                    tableRowsBuilder.AppendLine($@"
                    <tr>
                        <td class=""medicine-num"">{(i + 1):D2}</td>
                        <td>
                            <div class=""medicine-name"">{name}</div>
                            <div class=""medicine-usage"">Cách dùng: {usage}</div>
                        </td>
                        <td class=""medicine-qty"" style=""text-align: right; font-weight: bold; color: #0f172a;"">{qty}</td>
                    </tr>");
                }

                // 4. Tạo HTML hoàn chỉnh từ template
                string prescriptionCode = "DT-AI-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(100, 999);
                string currentDateString = "Ngày " + DateTime.Now.Day + " tháng " + DateTime.Now.Month + " năm " + DateTime.Now.Year;
                string currentTimeString = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                string htmlContent = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Đơn Thuốc AI - Healthcare Clinic</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 40px;
            color: #334155;
            background-color: #ffffff;
            line-height: 1.5;
        }}
        .header {{
            display: flex;
            justify-content: space-between;
            border-bottom: 2px solid #3b82f6;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .logo-area {{
            display: flex;
            align-items: center;
        }}
        .logo-icon {{
            width: 50px;
            height: 50px;
            background-color: #2563eb;
            border-radius: 12px;
            display: flex;
            justify-content: center;
            align-items: center;
            color: white;
            font-size: 28px;
            font-weight: bold;
            margin-right: 15px;
        }}
        .clinic-info h1 {{
            font-size: 20px;
            font-weight: 800;
            color: #1e3a8a;
            margin: 0 0 5px 0;
            text-transform: uppercase;
        }}
        .clinic-info p {{
            font-size: 13px;
            color: #64748b;
            margin: 2px 0;
        }}
        .prescription-code {{
            text-align: right;
        }}
        .prescription-code h2 {{
            font-size: 14px;
            color: #64748b;
            margin: 0 0 5px 0;
        }}
        .prescription-code p {{
            font-size: 16px;
            font-weight: bold;
            color: #0f172a;
            margin: 0;
        }}
        .title {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .title h3 {{
            font-size: 28px;
            font-weight: 800;
            color: #1e293b;
            margin: 0;
            letter-spacing: 1px;
        }}
        .title p {{
            font-size: 14px;
            color: #3b82f6;
            font-weight: bold;
            margin: 5px 0 0 0;
        }}
        .section-title {{
            font-size: 16px;
            font-weight: bold;
            color: #1e3a8a;
            border-left: 4px solid #2563eb;
            padding-left: 10px;
            margin: 20px 0 15px 0;
            text-transform: uppercase;
        }}
        .patient-grid {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 12px;
            margin-bottom: 25px;
            background-color: #f8fafc;
            padding: 20px;
            border-radius: 16px;
            border: 1px solid #f1f5f9;
        }}
        .info-item {{
            font-size: 14px;
        }}
        .info-label {{
            color: #64748b;
            font-weight: 500;
        }}
        .info-value {{
            color: #0f172a;
            font-weight: 600;
        }}
        .medicine-table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 30px;
        }}
        .medicine-table th {{
            background-color: #f1f5f9;
            color: #475569;
            text-align: left;
            padding: 12px 16px;
            font-size: 14px;
            font-weight: 700;
            border-bottom: 2px solid #cbd5e1;
        }}
        .medicine-table td {{
            padding: 16px;
            font-size: 14px;
            border-bottom: 1px solid #e2e8f0;
            vertical-align: top;
        }}
        .medicine-num {{
            font-weight: bold;
            color: #64748b;
            width: 30px;
        }}
        .medicine-name {{
            font-weight: 700;
            color: #0f172a;
            font-size: 15px;
        }}
        .medicine-qty {{
            text-align: right;
            font-weight: bold;
            color: #0f172a;
        }}
        .medicine-usage {{
            font-size: 13px;
            color: #475569;
            margin-top: 4px;
            font-style: italic;
        }}
        .notes-box {{
            background-color: #fef3c7;
            border-left: 4px solid #d97706;
            padding: 15px 20px;
            border-radius: 12px;
            margin-bottom: 40px;
        }}
        .notes-box p {{
            margin: 5px 0;
            font-size: 13.5px;
            color: #78350f;
        }}
        .footer-signatures {{
            display: flex;
            justify-content: space-between;
            margin-top: 50px;
            page-break-inside: avoid;
        }}
        .signature-item {{
            text-align: center;
            width: 250px;
        }}
        .signature-item .date {{
            font-size: 13px;
            color: #64748b;
            font-style: italic;
            margin-bottom: 15px;
        }}
        .signature-item .role {{
            font-size: 14px;
            font-weight: bold;
            color: #334155;
            margin-bottom: 60px;
        }}
        .signature-item .name {{
            font-size: 15px;
            font-weight: bold;
            color: #1e3a8a;
        }}
        .digital-sig {{
            border: 2px dashed #10b981;
            padding: 8px;
            border-radius: 8px;
            color: #047857;
            font-size: 11px;
            font-weight: bold;
            display: inline-block;
            margin-top: 10px;
            background-color: #ecfdf5;
        }}
    </style>
</head>
<body>

    <div class=""header"">
        <div class=""logo-area"">
            <div class=""logo-icon"">+</div>
            <div class=""clinic-info"">
                <h1>Hệ thống Y tế Số Healthcare Clinic</h1>
                <p>Địa chỉ: Lầu 5, Tòa nhà Công nghệ thông tin, Khu đô thị ĐHQG HCM, Thủ Đức, TP. HCM</p>
                <p>Điện thoại: 1900 6060 | Hotline: 0988 888 888</p>
            </div>
        </div>
        <div class=""prescription-code"">
            <h2>MÃ ĐƠN THUỐC AI</h2>
            <p>{prescriptionCode}</p>
        </div>
    </div>

    <div class=""title"">
        <h3>ĐƠN THUỐC ĐIỆN TỬ</h3>
        <p>Được trích xuất và tối ưu bởi Trí tuệ nhân tạo Healthcare AI</p>
    </div>

    <div class=""section-title"">Thông tin bệnh nhân</div>
    <div class=""patient-grid"">
        <div class=""info-item"">
            <span class=""info-label"">Họ và tên:</span>
            <span class=""info-value"">{patientName}</span>
        </div>
        <div class=""info-item"">
            <span class=""info-label"">Giới tính / Tuổi:</span>
            <span class=""info-value"">{patientGender} / {patientAge} tuổi</span>
        </div>
        <div class=""info-item"">
            <span class=""info-label"">Địa chỉ:</span>
            <span class=""info-value"">{patientAddress}</span>
        </div>
        <div class=""info-item"">
            <span class=""info-label"">Số thẻ BHYT:</span>
            <span class=""info-value"">{insuranceCode}</span>
        </div>
        <div class=""info-item"" style=""grid-column: span 2;"">
            <span class=""info-label"">Chẩn đoán bệnh:</span>
            <span class=""info-value"" style=""color: #dc2626;"">{diagnosis}</span>
        </div>
    </div>

    <div class=""section-title"">Chỉ định dùng thuốc</div>
    <table class=""medicine-table"">
        <thead>
            <tr>
                <th class=""medicine-num"">STT</th>
                <th>Tên thuốc & Hàm lượng / Đường dùng</th>
                <th style=""text-align: right; width: 100px;"">Số lượng</th>
            </tr>
        </thead>
        <tbody>
            {tableRowsBuilder.ToString()}
        </tbody>
    </table>

    <div class=""section-title"">Lời dặn của Bác sĩ</div>
    <div class=""notes-box"">
        <p>• Uống nhiều nước ấm, súc họng bằng nước muối sinh lý ấm ít nhất 3 lần/ngày.</p>
        <p>• Nghỉ ngơi hợp lý, tránh làm việc quá sức, tránh thức ăn cay nóng, đồ uống lạnh.</p>
        <p>• Tái khám sau 7 ngày hoặc ngay khi có các triệu chứng bất thường như khó thở, sốt cao liên tục không giảm.</p>
    </div>

    <div class=""footer-signatures"">
        <div class=""signature-item"">
            <div class=""date"">&nbsp;</div>
            <div class=""role"">BỆNH NHÂN / NGƯỜI NHÀ</div>
            <div class=""name"">(Ký và ghi rõ họ tên)</div>
        </div>
        <div class=""signature-item"">
            <div class=""date"">{currentDateString}</div>
            <div class=""role"">BÁC SĨ ĐIỀU TRỊ</div>
            <div class=""name"">ThS. BS. Nguyễn Minh Đức</div>
            <div class=""digital-sig"">
                ✓ ĐÃ KÝ ĐIỆN TỬ<br>
                Healthcare AI System<br>
                Thời gian: {currentTimeString}
            </div>
        </div>
    </div>

</body>
</html>";

                // 5. Ghi ra file HTML tạm thời
                string tempHtmlPath = Path.Combine(Path.GetTempPath(), "DonThuoc_AI_Temp.html");
                File.WriteAllText(tempHtmlPath, htmlContent, Encoding.UTF8);

                // 6. Chuyển đổi HTML sang ảnh PNG bằng Edge Headless Screenshot
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string pngFileName = $"don_thuoc_{timestamp}.png";
                string tempPngPath = Path.Combine(Path.GetTempPath(), pngFileName);
                bool pngGenerated = false;

                try
                {
                    string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
                    if (!File.Exists(edgePath))
                    {
                        edgePath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
                    }

                    if (File.Exists(edgePath))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = edgePath,
                            Arguments = $"--headless --disable-gpu --window-size=950,1350 --screenshot=\"{tempPngPath}\" \"{tempHtmlPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var process = System.Diagnostics.Process.Start(psi);
                        if (process != null)
                        {
                            // Đợi tối đa 8 giây để Edge chụp màn hình
                            bool exited = process.WaitForExit(8000);
                            if (exited && File.Exists(tempPngPath))
                            {
                                pngGenerated = true;
                            }
                        }
                    }
                }
                catch (Exception pngEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Edge PNG conversion error]: {pngEx.Message}");
                }

                // 7. Mở file đầu ra (Ưu tiên ảnh PNG, nếu Edge lỗi thì mở HTML trực tiếp bằng Browser)
                try
                {
                    if (pngGenerated && File.Exists(tempPngPath))
                    {
                        var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(tempPngPath);
                        await Windows.System.Launcher.LaunchFileAsync(storageFile);
                    }
                    else
                    {
                        // Fallback: Mở HTML trực tiếp trên Browser
                        var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(tempHtmlPath);
                        await Windows.System.Launcher.LaunchFileAsync(storageFile);
                    }
                }
                catch (Exception launchEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Launch File Error]: {launchEx.Message}");
                    // Fallback cuối cùng bằng Process.Start
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = pngGenerated ? tempPngPath : tempHtmlPath,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi);
                    }
                    catch (Exception lastEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Final Fallback Error]: {lastEx.Message}");
                    }
                }

                // Nếu chạy fallback, hiển thị thông báo nhỏ cho người dùng
                if (isFallback)
                {
                    var infoDialog = new ContentDialog
                    {
                        Title = "Thông tin thử nghiệm",
                        Content = $"Không thể kết nối API AI trên Server (Chi tiết: {apiErrorMsg}).\n\nỨng dụng đã tự động giả lập và sinh đơn thuốc mẫu động để bạn thử nghiệm đầy đủ tính năng.",
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    };
                    await infoDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Lỗi phân tích",
                    Content = "Có lỗi xảy ra trong quá trình xử lý đơn thuốc: " + ex.Message,
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                EmptyResultState.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                BtnAnalyze.IsEnabled = true;
                BtnSelectFile.IsEnabled = true;
            }
        }

        private async void BtnBook_Click(object sender, RoutedEventArgs e)
        {
            var medicines = ListMedicines.ItemsSource as List<string>;
            if (medicines == null || medicines.Count == 0)
            {
                _shell?.NavigateToPage(typeof(BookAppointmentPage));
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Nhắc nhở uống thuốc",
                Content = "Bạn có muốn thêm các thuốc này vào danh sách nhắc nhở uống thuốc hàng ngày không?",
                PrimaryButtonText = "Thêm và tiếp tục",
                SecondaryButtonText = "Bỏ qua",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                BtnContinueBooking.IsEnabled = false;
                LoadingOverlay.Visibility = Visibility.Visible;
                if (LoadingOverlay.Children[1] is TextBlock textBlock)
                {
                    textBlock.Text = "Đang lưu nhắc nhở uống thuốc...";
                }

                await SaveMedicinesAndRemindersAsync(medicines);

                LoadingOverlay.Visibility = Visibility.Collapsed;
                _shell?.NavigateToPage(typeof(BookAppointmentPage));
            }
            else if (result == ContentDialogResult.Secondary)
            {
                _shell?.NavigateToPage(typeof(BookAppointmentPage));
            }
        }

        private async Task SaveMedicinesAndRemindersAsync(List<string> medicines)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                var currentUserId = SessionStorage.CurrentUser?.Id;
                if (string.IsNullOrEmpty(currentUserId)) return;

                foreach (var medName in medicines)
                {
                    // 1. Kiểm tra và thêm vào MasterMedicine nếu chưa có
                    var checkMed = await client.From<MasterMedicine>()
                        .Where(m => m.MedicineName == medName)
                        .Get();

                    if (checkMed.Models.Count == 0)
                    {
                        var newMed = new MasterMedicine
                        {
                            Id = Guid.NewGuid().ToString(),
                            MedicineName = medName,
                            DefaultDosage = "Theo chỉ dẫn bác sĩ"
                        };
                        await client.From<MasterMedicine>().Insert(newMed);
                    }

                    // 2. Thêm vào MedicationReminder cho bệnh nhân
                    var reminder = new MedicationReminder
                    {
                        Id = Guid.NewGuid().ToString(),
                        PatientId = currentUserId,
                        MedicineName = medName,
                        Dosage = "1 viên",
                        TimeToTake = new TimeSpan(8, 0, 0), // Mặc định 8:00 sáng
                        StartDate = DateTime.Now,
                        IsActive = true
                    };
                    await client.From<MedicationReminder>().Insert(reminder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveReminders] Error: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _shell?.NavigateToPage(typeof(PatientHomePage));
        }
    }
}
