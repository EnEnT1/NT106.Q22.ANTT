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
using System.Threading.Tasks;
using Windows.Storage.Pickers;

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
                // Sử dụng AiClient đã được refactor
                var aiClient = new AiClient();
                var medicines = await aiClient.AnalyzePrescriptionAsync(_selectedFilePath);

                if (medicines != null && medicines.Count > 0)
                {
                    ListMedicines.ItemsSource = medicines;
                    ListMedicines.Visibility = Visibility.Visible;
                    BtnContinueBooking.IsEnabled = true;
                }
                else
                {
                    EmptyResultState.Visibility = Visibility.Visible;
                    if (EmptyResultState.Children[1] is TextBlock textBlock)
                    {
                        textBlock.Text = "AI không tìm thấy thông tin thuốc trong file này.";
                    }
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Lỗi phân tích",
                    Content = "Không thể phân tích đơn thuốc. Vui lòng kiểm tra kết nối Server (Port 5246).\n\nChi tiết: " + ex.Message,
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
