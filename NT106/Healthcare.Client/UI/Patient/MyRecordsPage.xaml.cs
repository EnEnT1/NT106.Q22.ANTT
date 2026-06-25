using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Healthcare.Client.UI.Patient
{
    public class RecordViewModel
    {
        public string Id { get; set; }
        public string DateStr { get; set; }
        public string DoctorName { get; set; }
        public string Diagnosis { get; set; }
        public string Prescription { get; set; }
        public string AppointmentId { get; set; }
        public List<string> AiMedicines { get; set; } = new();
        public string VoiceDiagnosis { get; set; } = "";
        public Visibility VoiceDiagnosisVisibility => string.IsNullOrEmpty(VoiceDiagnosis) ? Visibility.Collapsed : Visibility.Visible;
        public string VoiceAudioBase64 { get; set; } = "";
        public Visibility VoiceAudioVisibility => string.IsNullOrEmpty(VoiceAudioBase64) ? Visibility.Collapsed : Visibility.Visible;
    }

    public sealed partial class MyRecordsPage : Page
    {

        private ObservableCollection<RecordViewModel> _records = new();

        public MyRecordsPage()
        {
            this.InitializeComponent();
            RecordsList.ItemsSource = _records;
            LoadDataAsync();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            LoadingRing.IsActive = true;
            _records.Clear();
            EmptyStateText.Visibility = Visibility.Collapsed;

            try
            {
                var client = SupabaseManager.Instance.Client;
                var currentUserId = SessionStorage.CurrentUser?.Id;

                if (string.IsNullOrEmpty(currentUserId)) return;

                // 1. Fetch Medical Records (History)
                var recordsResponse = await client.From<MedicalRecord>()
                    .Where(x => x.PatientId == currentUserId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                if (recordsResponse.Models.Count == 0)
                {
                    EmptyStateText.Visibility = Visibility.Visible;
                }
                else
                {
                    foreach (var rec in recordsResponse.Models)
                    {
                        var docName = await GetDoctorName(rec.DoctorId);
                        
                        string medList = "Không có đơn thuốc";
                        string voiceDiag = "";
                        string voiceAudio = "";
                        var actualMeds = new List<string>();

                        if (rec.AiMedicines != null && rec.AiMedicines.Count > 0)
                        {
                            foreach (var item in rec.AiMedicines)
                            {
                                if (item.StartsWith("[VoiceDiagnosis] "))
                                {
                                    voiceDiag = item.Substring("[VoiceDiagnosis] ".Length);
                                }
                                else if (item.StartsWith("[VoiceAudio] "))
                                {
                                    voiceAudio = item.Substring("[VoiceAudio] ".Length);
                                }
                                else
                                {
                                    actualMeds.Add(item);
                                }
                            }
                            if (actualMeds.Count > 0)
                            {
                                medList = string.Join(", ", actualMeds);
                            }
                        }

                        _records.Add(new RecordViewModel
                        {
                            Id = rec.Id,
                            DateStr = rec.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                            DoctorName = "BS. " + docName,
                            Diagnosis = rec.Diagnosis,
                            Prescription = string.IsNullOrEmpty(rec.PrescriptionImageUrl) ? medList : "Ghi chú: Xem ảnh đơn thuốc",
                            AppointmentId = rec.AppointmentId,
                            AiMedicines = actualMeds,
                            VoiceDiagnosis = voiceDiag,
                            VoiceAudioBase64 = voiceAudio
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Silence or log error
                System.Diagnostics.Debug.WriteLine("Error loading records: " + ex.Message);
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private async Task<string> GetDoctorName(string doctorId)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                var res = await client.From<User>().Where(x => x.Id == doctorId).Single();
                return res?.FullName ?? "Bác sĩ";
            }
            catch { return "Bác sĩ"; }
        }



        private async void BtnViewDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is RecordViewModel record)
            {
                var dialog = new ContentDialog
                {
                    Title = "Chi tiết chẩn đoán",
                    Content = new TextBlock 
                    { 
                        Text = record.Diagnosis, 
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 16
                    },
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }

        private async void BtnViewPrescription_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is RecordViewModel record)
            {
                var stack = new StackPanel { Spacing = 10, Padding = new Thickness(0, 10, 0, 0) };
                
                if (record.AiMedicines == null || record.AiMedicines.Count == 0)
                {
                    stack.Children.Add(new TextBlock { Text = "Không có đơn thuốc.", FontStyle = Windows.UI.Text.FontStyle.Italic });
                }
                else
                {
                    stack.Children.Add(new TextBlock { Text = "Danh sách thuốc:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 14 });
                    foreach (var med in record.AiMedicines)
                    {
                        stack.Children.Add(new TextBlock 
                        { 
                            Text = "• " + med, 
                            FontSize = 14, 
                            TextWrapping = TextWrapping.Wrap 
                        });
                    }
                }

                var dialog = new ContentDialog
                {
                    Title = "Đơn thuốc",
                    Content = stack,
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }

        private async void BtnDownloadPrescription_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is RecordViewModel record)
            {
                try
                {
                    var medicines = record.AiMedicines;
                    var tableRowsBuilder = new System.Text.StringBuilder();
                    if (medicines == null || medicines.Count == 0)
                    {
                        tableRowsBuilder.AppendLine("<tr><td colspan='2' style='text-align: center; color: #64748b;'>Không có thuốc</td></tr>");
                    }
                    else
                    {
                        for (int i = 0; i < medicines.Count; i++)
                        {
                            var med = medicines[i];
                            tableRowsBuilder.AppendLine($@"
                            <tr>
                                <td style='padding: 12px; border-bottom: 1px solid #e2e8f0; font-weight: bold; color: #64748b;'>{(i + 1):D2}</td>
                                <td style='padding: 12px; border-bottom: 1px solid #e2e8f0;'>{med}</td>
                            </tr>");
                        }
                    }

                    string htmlContent = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Đơn Thuốc - Healthcare Clinic</title>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #334155; }}
        .header {{ border-bottom: 2px solid #2563eb; padding-bottom: 20px; margin-bottom: 30px; }}
        .title {{ text-align: center; color: #1e3a8a; font-size: 24px; font-weight: bold; margin-bottom: 20px; }}
        .info {{ background-color: #f8fafc; padding: 15px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #e2e8f0; }}
        .info p {{ margin: 5px 0; font-size: 14px; }}
        .medicine-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
        .medicine-table th {{ background-color: #f1f5f9; text-align: left; padding: 12px; border-bottom: 2px solid #cbd5e1; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1 style=""color: #1e3a8a; margin: 0; font-size: 22px;"">HỆ THỐNG Y TẾ HEALTHCARE CLINIC</h1>
        <p style=""color: #64748b; margin: 5px 0 0 0; font-size: 12px;"">Lịch sử khám bệnh & Đơn thuốc điện tử</p>
    </div>
    <div class=""title"">ĐƠN THUỐC ĐIỆN TỬ</div>
    <div class=""info"">
        <p><strong>Bác sĩ kê đơn:</strong> {record.DoctorName}</p>
        <p><strong>Ngày khám:</strong> {record.DateStr}</p>
        <p><strong>Chẩn đoán:</strong> {record.Diagnosis}</p>
    </div>
    <h3 style=""color: #1e3a8a; border-bottom: 1px solid #e2e8f0; padding-bottom: 8px;"">CHỈ ĐỊNH DÙNG THUỐC</h3>
    <table class=""medicine-table"">
        <thead>
            <tr>
                <th style=""width: 50px;"">STT</th>
                <th>Tên thuốc & Cách dùng</th>
            </tr>
        </thead>
        <tbody>
            {tableRowsBuilder}
        </tbody>
    </table>
</body>
</html>";

                    var savePicker = new FileSavePicker();
                    var window = MainWindow.Instance;
                    if (window != null)
                    {
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                    }

                    savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                    savePicker.FileTypeChoices.Add("HTML Document", new List<string>() { ".html" });
                    savePicker.SuggestedFileName = "DonThuoc_" + record.DateStr.Replace("/", "").Replace(":", "").Replace(" ", "_");

                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        await Windows.Storage.FileIO.WriteTextAsync(file, htmlContent, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                        
                        var successDialog = new ContentDialog
                        {
                            Title = "Lưu thành công",
                            Content = $"Đã lưu đơn thuốc vào:\n{file.Path}\n\nBạn có muốn mở file này ngay không?",
                            PrimaryButtonText = "Mở file",
                            CloseButtonText = "Đóng",
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.XamlRoot
                        };

                        if (await successDialog.ShowAsync() == ContentDialogResult.Primary)
                        {
                            await Windows.System.Launcher.LaunchFileAsync(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Lỗi khi lưu",
                        Content = "Không thể lưu file đơn thuốc: " + ex.Message,
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void BtnDownloadVoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is RecordViewModel record)
            {
                if (string.IsNullOrEmpty(record.VoiceDiagnosis)) return;

                try
                {
                    var savePicker = new FileSavePicker();
                    var window = MainWindow.Instance;
                    if (window != null)
                    {
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                    }

                    savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                    savePicker.FileTypeChoices.Add("Text Document", new List<string>() { ".txt" });
                    savePicker.SuggestedFileName = "ChanDoanGiongNoi_" + record.DateStr.Replace("/", "").Replace(":", "").Replace(" ", "_");

                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        string content = $"HỆ THỐNG Y TẾ HEALTHCARE CLINIC\n" +
                                         $"CHẨN ĐOÁN DẠNG GIỌNG NÓI CỦA BÁC SĨ\n" +
                                         $"====================================\n\n" +
                                         $"Bác sĩ kê đơn: {record.DoctorName}\n" +
                                         $"Ngày khám: {record.DateStr}\n" +
                                         $"Chẩn đoán xác định: {record.Diagnosis}\n\n" +
                                         $"Nội dung chẩn đoán bằng giọng nói:\n" +
                                         $"------------------------------------\n" +
                                         $"{record.VoiceDiagnosis}\n";

                        await Windows.Storage.FileIO.WriteTextAsync(file, content, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                        
                        var successDialog = new ContentDialog
                        {
                            Title = "Lưu thành công",
                            Content = $"Đã tải và lưu chẩn đoán giọng nói vào:\n{file.Path}",
                            CloseButtonText = "Đóng",
                            XamlRoot = this.XamlRoot
                        };
                        await successDialog.ShowAsync();
                    }
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Lỗi khi lưu",
                        Content = "Không thể lưu file chẩn đoán: " + ex.Message,
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void BtnDownloadAudio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is RecordViewModel record)
            {
                if (string.IsNullOrEmpty(record.VoiceAudioBase64)) return;

                try
                {
                    var savePicker = new FileSavePicker();
                    var window = MainWindow.Instance;
                    if (window != null)
                    {
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                    }

                    savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                    savePicker.FileTypeChoices.Add("M4A Audio", new List<string>() { ".m4a" });
                    savePicker.SuggestedFileName = "GhiAmCuocGoi_" + record.DateStr.Replace("/", "").Replace(":", "").Replace(" ", "_");

                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        byte[] bytes = Convert.FromBase64String(record.VoiceAudioBase64);
                        await Windows.Storage.FileIO.WriteBytesAsync(file, bytes);

                        var successDialog = new ContentDialog
                        {
                            Title = "Lưu thành công",
                            Content = $"Đã tải và lưu file ghi âm vào:\n{file.Path}\n\nBạn có muốn phát file ghi âm này không?",
                            PrimaryButtonText = "Phát file",
                            CloseButtonText = "Đóng",
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.XamlRoot
                        };

                        if (await successDialog.ShowAsync() == ContentDialogResult.Primary)
                        {
                            await Windows.System.Launcher.LaunchFileAsync(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Lỗi khi lưu",
                        Content = "Không thể lưu file ghi âm: " + ex.Message,
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
    }
}
