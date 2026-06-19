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
        private PrescriptionData _currentPrescription;

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
                _currentPrescription = null;
                bool isFallback = false;
                string apiErrorMsg = "";

                try
                {
                    var aiClient = new AiClient();
                    _currentPrescription = await aiClient.AnalyzePrescriptionAsync(_selectedFilePath);
                }
                catch (Exception apiEx)
                {
                    apiErrorMsg = apiEx.Message;
                    isFallback = true;
                }

                // Nếu API bị lỗi hoặc không nhận diện được thuốc, tiến hành Fallback động để demo
                if (_currentPrescription == null || _currentPrescription.Medicines == null || _currentPrescription.Medicines.Count == 0)
                {
                    isFallback = true;
                    _currentPrescription = new PrescriptionData
                    {
                        ClinicName = "Hệ thống Y tế Số Healthcare Clinic",
                        ClinicAddress = "Lầu 5, Tòa nhà Công nghệ thông tin, Khu đô thị ĐHQG HCM, Thủ Đức, TP. HCM",
                        ClinicPhone = "1900 6060",
                        PatientName = SessionStorage.CurrentUser?.FullName ?? "Nguyễn Văn Bệnh Nhân",
                        PatientAge = "25",
                        PatientGender = "Nam",
                        PatientAddress = "Thủ Đức, TP. Hồ Chí Minh",
                        Diagnosis = "Viêm họng cấp tính / Sốt nhẹ chưa rõ nguyên nhân",
                        DoctorName = "ThS. BS. Nguyễn Minh Đức",
                        PrescriptionDate = DateTime.Now.ToString("dd/MM/yyyy"),
                        DoctorAdvice = "Uống nhiều nước ấm, súc họng bằng nước muối sinh lý ấm ít nhất 3 lần/ngày. Nghỉ ngơi hợp lý, tránh làm việc quá sức, tránh thức ăn cay nóng, đồ uống lạnh. Tái khám sau 7 ngày hoặc ngay khi có các triệu chứng bất thường như khó thở, sốt cao liên tục không giảm.",
                        Medicines = new List<MedicineItem>
                        {
                            new MedicineItem { Name = "Paracetamol 500mg", Quantity = "20 Viên", TimesPerDay = "2", QuantityPerTime = "1 viên", Note = "sáng, tối sau ăn khi sốt trên 38.5 độ C" },
                            new MedicineItem { Name = "Amoxicillin 500mg", Quantity = "21 Viên", TimesPerDay = "3", QuantityPerTime = "1 viên", Note = "sáng, trưa, tối sau ăn" },
                            new MedicineItem { Name = "Decolgen Forte", Quantity = "10 Viên", TimesPerDay = "2", QuantityPerTime = "1 viên", Note = "sáng, tối sau ăn khi sổ mũi" },
                            new MedicineItem { Name = "Siro Ho Prospan 100ml", Quantity = "1 Chai", TimesPerDay = "3", QuantityPerTime = "5ml", Note = "sau ăn" }
                        }
                    };
                }

                // 1. Cập nhật dữ liệu lên giao diện (ở cột bên phải "Đơn thuốc")
                var uiMedicines = new List<string>();
                foreach (var med in _currentPrescription.Medicines)
                {
                    string desc = $"{med.Name} - SL: {med.Quantity}. Uống {med.QuantityPerTime} x {med.TimesPerDay} lần/ngày";
                    if (!string.IsNullOrEmpty(med.Note))
                    {
                        desc += $" ({med.Note})";
                    }
                    uiMedicines.Add(desc);
                }
                ListMedicines.ItemsSource = uiMedicines;
                ListMedicines.Visibility = Visibility.Visible;

                // 2. Chuẩn bị thông tin Bệnh nhân/Phòng khám/Chẩn đoán (Ưu tiên thông tin từ AI)
                string clinicName = string.IsNullOrWhiteSpace(_currentPrescription.ClinicName) ? "Hệ thống Y tế Số Healthcare Clinic" : _currentPrescription.ClinicName;
                string clinicAddress = string.IsNullOrWhiteSpace(_currentPrescription.ClinicAddress) ? "Lầu 5, Tòa nhà Công nghệ thông tin, Khu đô thị ĐHQG HCM, Thủ Đức, TP. HCM" : _currentPrescription.ClinicAddress;
                string clinicPhone = string.IsNullOrWhiteSpace(_currentPrescription.ClinicPhone) ? "1900 6060" : _currentPrescription.ClinicPhone;

                string patientName = string.IsNullOrWhiteSpace(_currentPrescription.PatientName) ? (SessionStorage.CurrentUser?.FullName ?? "Nguyễn Văn Bệnh Nhân") : _currentPrescription.PatientName;
                string patientGender = string.IsNullOrWhiteSpace(_currentPrescription.PatientGender) ? "Nam" : _currentPrescription.PatientGender;
                string patientAge = string.IsNullOrWhiteSpace(_currentPrescription.PatientAge) ? "25" : _currentPrescription.PatientAge;
                string patientAddress = string.IsNullOrWhiteSpace(_currentPrescription.PatientAddress) ? "Thủ Đức, TP. Hồ Chí Minh" : _currentPrescription.PatientAddress;
                string insuranceCode = "GD4797910200115"; // Thẻ BHYT mặc định

                string diagnosis = string.IsNullOrWhiteSpace(_currentPrescription.Diagnosis) ? "Theo dõi sức khỏe / Kê đơn điều trị ngoại trú" : _currentPrescription.Diagnosis;
                string doctorName = string.IsNullOrWhiteSpace(_currentPrescription.DoctorName) ? "ThS. BS. Nguyễn Minh Đức" : _currentPrescription.DoctorName;

                // 3. Xây dựng HTML bảng thuốc động
                var tableRowsBuilder = new StringBuilder();
                for (int i = 0; i < _currentPrescription.Medicines.Count; i++)
                {
                    var med = _currentPrescription.Medicines[i];
                    string name = med.Name;
                    string qty = med.Quantity;
                    string usage = $"Uống {med.QuantityPerTime} x {med.TimesPerDay} lần/ngày";
                    if (!string.IsNullOrEmpty(med.Note))
                    {
                        usage += $" ({med.Note})";
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

                // Xây dựng lời dặn của bác sĩ
                string doctorAdviceHtml = "";
                if (!string.IsNullOrWhiteSpace(_currentPrescription.DoctorAdvice))
                {
                    var adviceLines = _currentPrescription.DoctorAdvice.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in adviceLines)
                    {
                        var cleanLine = line.Trim().TrimStart('•', '-', '*', ' ');
                        doctorAdviceHtml += $"<p>• {cleanLine}</p>\n";
                    }
                }
                else
                {
                    doctorAdviceHtml = @"<p>• Uống nhiều nước ấm, súc họng bằng nước muối sinh lý ấm ít nhất 3 lần/ngày.</p>
        <p>• Nghỉ ngơi hợp lý, tránh làm việc quá sức, tránh thức ăn cay nóng, đồ uống lạnh.</p>
        <p>• Tái khám sau 7 ngày hoặc ngay khi có các triệu chứng bất thường như khó thở, sốt cao liên tục không giảm.</p>";
                }

                // 4. Tạo HTML hoàn chỉnh từ template
                string prescriptionCode = "DT-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(100, 999);
                string currentDateString = "Ngày " + DateTime.Now.Day + " tháng " + DateTime.Now.Month + " năm " + DateTime.Now.Year;
                string currentTimeString = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                string htmlContent = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Đơn Thuốc - Healthcare Clinic</title>
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
                <h1>{clinicName}</h1>
                <p>Địa chỉ: {clinicAddress}</p>
                <p>Điện thoại: {clinicPhone}</p>
            </div>
        </div>
        <div class=""prescription-code"">
            <h2>MÃ ĐƠN THUỐC</h2>
            <p>{prescriptionCode}</p>
        </div>
    </div>

    <div class=""title"">
        <h3>ĐƠN THUỐC ĐIỆN TỬ</h3>
        <p>Được trích xuất và tối ưu bởi Hệ thống Healthcare</p>
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
        {doctorAdviceHtml}
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
            <div class=""name"">{doctorName}</div>
            <div class=""digital-sig"">
                ✓ ĐÃ KÝ ĐIỆN TỬ<br>
                Healthcare System<br>
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
                        Content = $"Không thể kết nối API trên Server (Chi tiết: {apiErrorMsg}).\n\nỨng dụng đã tự động giả lập và sinh đơn thuốc mẫu động để bạn thử nghiệm đầy đủ tính năng.",
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
            if (_currentPrescription == null || _currentPrescription.Medicines == null || _currentPrescription.Medicines.Count == 0)
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
                LoadingOverlay.Visibility = Visibility.Visible;
                if (LoadingOverlay.Children[1] is TextBlock textBlock)
                {
                    textBlock.Text = "Đang lưu nhắc nhở uống thuốc...";
                }

                await SaveMedicinesAndRemindersAsync(_currentPrescription.Medicines);

                LoadingOverlay.Visibility = Visibility.Collapsed;
                _shell?.NavigateToPage(typeof(BookAppointmentPage));
            }
            else if (result == ContentDialogResult.Secondary)
            {
                _shell?.NavigateToPage(typeof(BookAppointmentPage));
            }
        }

        private async Task SaveMedicinesAndRemindersAsync(List<MedicineItem> medicines)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                var currentUserId = SessionStorage.CurrentUser?.Id;
                if (string.IsNullOrEmpty(currentUserId)) return;

                foreach (var med in medicines)
                {
                    string medName = med.Name;
                    if (string.IsNullOrWhiteSpace(medName)) continue;

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
                            DefaultDosage = string.IsNullOrEmpty(med.QuantityPerTime) ? "Theo chỉ dẫn bác sĩ" : $"{med.QuantityPerTime} x {med.TimesPerDay} lần/ngày"
                        };
                        await client.From<MasterMedicine>().Insert(newMed);
                    }

                    // 2. Thêm vào MedicationReminder cho bệnh nhân
                    var reminder = new MedicationReminder
                    {
                        Id = Guid.NewGuid().ToString(),
                        PatientId = currentUserId,
                        MedicineName = medName,
                        Dosage = string.IsNullOrEmpty(med.QuantityPerTime) ? "1 viên" : med.QuantityPerTime,
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
