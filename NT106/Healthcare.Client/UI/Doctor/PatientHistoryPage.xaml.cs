
// ═══════════════════════════════════════════════════════════════════
//  TODO [SUPABASE]: Khi tích hợp Supabase, thêm các using sau:
//
//  using Supabase;
//  using Healthcare.Client.Services;       // SupabaseService hoặc tương đương
//  using Healthcare.Client.Models;         // PatientModel, VitalSignsModel, v.v.
//  using System.Threading.Tasks;
//
//  Khởi tạo client Supabase (thường qua DI / Singleton):
//  var supabase = App.SupabaseClient;   (hoặc inject qua constructor)
// ═══════════════════════════════════════════════════════════════════

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using System;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    /// <summary>
    /// Trang chi tiết hồ sơ bệnh nhân - Tiền sử bệnh lý.
    /// Chỉ chứa phần Main Content; TopBar và SideBar được quản lý bởi Shell.
    ///
    /// TODO [SUPABASE]: Các bảng cần truy vấn:
    ///   - patients           : thông tin cơ bản (tên, mã BN, cân nặng, trạng thái)
    ///   - vital_signs        : chiều cao, cân nặng, huyết áp, nhịp tim, nhiệt độ, BMI
    ///   - allergies          : tiền sử dị ứng (tên dị ứng nguyên, mức độ, biểu hiện)
    ///   - medical_history    : bệnh nền, phẫu thuật, tiêm chủng
    ///   - medications        : tiền sử dùng thuốc (tên, liều, thời gian, trạng thái)
    ///   - family_history     : tiền sử gia đình
    ///   - lifestyle          : lối sống (thuốc lá, rượu bia, vận động, giấc ngủ)
    ///   - visit_history      : lịch sử thăm khám (sidebar)
    /// </summary>
    public sealed partial class PatientHistoryPage : Page
    {
        // ─────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────
        public PatientHistoryPage()
        {
            this.InitializeComponent();
        }

        // ─────────────────────────────────────────────────────────
        //  Navigation
        // ─────────────────────────────────────────────────────────
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // TODO [SUPABASE]: Bỏ comment đoạn dưới khi đã có Supabase.
            //   if (e.Parameter is int patientId)
            //       _ = LoadPatientDataAsync(patientId);
        }

        // ─────────────────────────────────────────────────────────
        //  Save All
        // ─────────────────────────────────────────────────────────
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO [SUPABASE]: Gọi Supabase Update cho tất cả các bảng ở đây.
            await ShowInfoAsync("Lưu hồ sơ", "✅ Tất cả thay đổi đã được lưu thành công.");
        }

        // ─────────────────────────────────────────────────────────
        //  Section 1 – Dấu hiệu sinh tồn
        // ─────────────────────────────────────────────────────────
        private async void BtnEditVitals_Click(object sender, RoutedEventArgs e)
        {
            var heightBox  = CreateTextBox("Chiều cao (cm)", TxtHeight.Text);
            var weightBox  = CreateTextBox("Cân nặng (kg)",  TxtWeight.Text);
            var bmiBox     = CreateTextBox("BMI",            TxtBmi.Text);
            var bpBox      = CreateTextBox("Huyết áp (mmHg)", TxtBloodPressure.Text);
            var hrBox      = CreateTextBox("Nhịp tim (bpm)", TxtHeartRate.Text);
            var tempBox    = CreateTextBox("Nhiệt độ (°C)",  TxtTemperature.Text.Replace("°C", "").Trim());

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(heightBox);
            panel.Children.Add(weightBox);
            panel.Children.Add(bmiBox);
            panel.Children.Add(bpBox);
            panel.Children.Add(hrBox);
            panel.Children.Add(tempBox);

            var dialog = CreateDialog("✏ Sửa dấu hiệu sinh tồn", panel);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TxtHeight.Text        = heightBox.Text;
                TxtWeight.Text        = weightBox.Text;
                TxtBmi.Text           = bmiBox.Text;
                TxtBloodPressure.Text = bpBox.Text;
                TxtHeartRate.Text     = hrBox.Text;
                TxtTemperature.Text   = tempBox.Text + "°C";
                // TODO [SUPABASE]: await SupabaseService.UpdateVitalSignsAsync(patientId, ...);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Section 2 – Thêm dị ứng
        // ─────────────────────────────────────────────────────────
        private async void BtnAddAllergy_Click(object sender, RoutedEventArgs e)
        {
            var nameBox     = CreateTextBox("Tên dị ứng nguyên (vd: Penicillin)", "");
            var symptomsBox = CreateTextBox("Biểu hiện (vd: Nổi mề đay)", "");

            var severityLabel = new TextBlock
            {
                Text     = "Mức độ nghiêm trọng",
                FontSize = 12,
                Margin   = new Thickness(0, 0, 0, 4)
            };
            var severityCombo = new ComboBox
            {
                PlaceholderText = "Chọn mức độ",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            severityCombo.Items.Add("NGHIÊM TRỌNG");
            severityCombo.Items.Add("TRUNG BÌNH");
            severityCombo.Items.Add("NHẸ");
            severityCombo.SelectedIndex = 1;

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(nameBox);
            panel.Children.Add(severityLabel);
            panel.Children.Add(severityCombo);
            panel.Children.Add(symptomsBox);

            var dialog = CreateDialog("+ Thêm dị ứng mới", panel);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary
                && !string.IsNullOrWhiteSpace(nameBox.Text))
            {
                string severity    = severityCombo.SelectedItem?.ToString() ?? "TRUNG BÌNH";
                string allergyName = nameBox.Text.ToUpper();
                string symptoms    = symptomsBox.Text;

                // Thêm UI item mới vào AllergyList
                AddAllergyItem(allergyName, severity, symptoms);
                // TODO [SUPABASE]: await SupabaseService.AddAllergyAsync(patientId, ...);
            }
        }

        /// <summary>Tạo Border item dị ứng và thêm vào AllergyList StackPanel.</summary>
        private void AddAllergyItem(string name, string severity, string symptoms)
        {
            bool   isSevere     = severity == "NGHIÊM TRỌNG";
            string borderColor  = isSevere ? "#FECACA" : "#FED7AA";
            string bgColor      = isSevere ? "#FEE2E2" : "#FFEDD5";
            string badgeColor   = isSevere ? "#DC2626" : "#EA580C";
            string textColor    = isSevere ? "#B91C1C" : "#C2410C";

            var severityBadge = new Border
            {
                CornerRadius = new CornerRadius(3),
                Padding      = new Thickness(5, 2, 5, 2),
            };
            severityBadge.Background = new SolidColorBrush(ParseColor(badgeColor));
            severityBadge.Child = new TextBlock
            {
                Text           = severity,
                FontSize       = 8,
                FontWeight     = Microsoft.UI.Text.FontWeights.Black,
                Foreground     = new SolidColorBrush(Colors.White),
                CharacterSpacing = 50
            };

            var nameBlock = new TextBlock
            {
                Text       = name,
                FontSize   = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(ParseColor("#1E293B"))
            };

            var badgeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            badgeRow.Children.Add(severityBadge);
            badgeRow.Children.Add(nameBlock);
            badgeRow.Margin = new Thickness(0, 0, 0, 4);

            var symptomsBlock = new TextBlock
            {
                Text       = $"Biểu hiện: {symptoms}",
                FontSize   = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(ParseColor(textColor))
            };

            var textStack = new StackPanel();
            textStack.Children.Add(badgeRow);
            textStack.Children.Add(symptomsBlock);

            var iconBorder = new Border
            {
                Width        = 40,
                Height       = 40,
                CornerRadius = new CornerRadius(20)
            };
            iconBorder.Background = new SolidColorBrush(ParseColor(bgColor));
            iconBorder.Child = new FontIcon
            {
                FontFamily          = new FontFamily("Segoe MDL2 Assets"),
                Glyph               = "\uECAA",
                FontSize            = 18,
                Foreground          = new SolidColorBrush(ParseColor(badgeColor)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
            row.Children.Add(iconBorder);
            row.Children.Add(textStack);

            var item = new Border
            {
                Background   = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Padding      = new Thickness(14, 12, 14, 12),
                Child        = row
            };
            item.BorderBrush = new SolidColorBrush(ParseColor(borderColor));

            AllergyList.Children.Add(item);
        }

        // ─────────────────────────────────────────────────────────
        //  Section 3 – Tiền sử bệnh lý
        // ─────────────────────────────────────────────────────────
        private async void BtnEditMedHistory_Click(object sender, RoutedEventArgs e)
        {
            var chronicName  = CreateTextBox("Bệnh nền", TxtChronicName.Text);
            var chronicYear  = CreateTextBox("Phát hiện từ năm", TxtChronicYear.Text.Replace("Phát hiện từ: ", ""));
            var chronicNote  = CreateTextBox("Ghi chú bệnh nền", TxtChronicNote.Text);

            var surgeryName  = CreateTextBox("Phẫu thuật", TxtSurgeryName.Text);
            var surgeryYear  = CreateTextBox("Năm phẫu thuật", TxtSurgeryYear.Text.Replace("Năm thực hiện: ", ""));

            var vaccName     = CreateTextBox("Vaccine đã tiêm", TxtVaccinationName.Text);
            var vaccYear     = CreateTextBox("Gần nhất (năm)", TxtVaccinationYear.Text.Replace("Gần nhất: ", ""));

            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(CreateSectionHeader("🏥 Bệnh nền"));
            panel.Children.Add(chronicName);
            panel.Children.Add(chronicYear);
            panel.Children.Add(chronicNote);
            panel.Children.Add(CreateSectionHeader("🔪 Phẫu thuật"));
            panel.Children.Add(surgeryName);
            panel.Children.Add(surgeryYear);
            panel.Children.Add(CreateSectionHeader("💉 Tiêm chủng"));
            panel.Children.Add(vaccName);
            panel.Children.Add(vaccYear);

            var dialog = CreateDialog("+ Sửa tiền sử bệnh lý", panel, minWidth: 500);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TxtChronicName.Text       = chronicName.Text;
                TxtChronicYear.Text       = "Phát hiện từ: " + chronicYear.Text;
                TxtChronicNote.Text       = chronicNote.Text;
                TxtSurgeryName.Text       = surgeryName.Text;
                TxtSurgeryYear.Text       = "Năm thực hiện: " + surgeryYear.Text;
                TxtVaccinationName.Text   = vaccName.Text;
                TxtVaccinationYear.Text   = "Gần nhất: " + vaccYear.Text;
                // TODO [SUPABASE]: await SupabaseService.UpdateMedicalHistoryAsync(patientId, ...);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Section 4 – Tiền sử dùng thuốc
        // ─────────────────────────────────────────────────────────
        private async void BtnEditMedication_Click(object sender, RoutedEventArgs e)
        {
            var medName   = CreateTextBox("Tên thuốc & hàm lượng", TxtMedName.Text);
            var medDosage = CreateTextBox("Liều dùng", TxtMedDosage.Text);
            var medPeriod = CreateTextBox("Thời gian dùng (vd: 2021 - Hiện tại)", TxtMedPeriod.Text);

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(medName);
            panel.Children.Add(medDosage);
            panel.Children.Add(medPeriod);

            var dialog = CreateDialog("+ Sửa thông tin thuốc", panel);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TxtMedName.Text   = medName.Text;
                TxtMedDosage.Text = medDosage.Text;
                TxtMedPeriod.Text = medPeriod.Text;
                // TODO [SUPABASE]: await SupabaseService.UpdateMedicationAsync(patientId, ...);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Section 5 – Tiền sử gia đình
        // ─────────────────────────────────────────────────────────
        private async void BtnEditFamily_Click(object sender, RoutedEventArgs e)
        {
            var fatherBox = CreateTextBox("Bố đẻ (bệnh lý)", TxtFatherCondition.Text);
            var motherBox = CreateTextBox("Mẹ đẻ (bệnh lý)", TxtMotherCondition.Text);

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(fatherBox);
            panel.Children.Add(motherBox);

            var dialog = CreateDialog("+ Sửa tiền sử gia đình", panel);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TxtFatherCondition.Text = fatherBox.Text;
                TxtMotherCondition.Text = motherBox.Text;
                // TODO [SUPABASE]: await SupabaseService.UpdateFamilyHistoryAsync(patientId, ...);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Section 6 – Lối sống
        // ─────────────────────────────────────────────────────────
        private async void BtnEditLifestyle_Click(object sender, RoutedEventArgs e)
        {
            var smokingBox  = CreateTextBox("Thuốc lá", TxtSmoking.Text);
            var alcoholBox  = CreateTextBox("Rượu bia",  TxtAlcohol.Text);
            var exerciseBox = CreateTextBox("Vận động",  TxtExercise.Text);
            var sleepBox    = CreateTextBox("Giấc ngủ",  TxtSleep.Text);

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(smokingBox);
            panel.Children.Add(alcoholBox);
            panel.Children.Add(exerciseBox);
            panel.Children.Add(sleepBox);

            var dialog = CreateDialog("+ Sửa lối sống", panel);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TxtSmoking.Text  = smokingBox.Text;
                TxtAlcohol.Text  = alcoholBox.Text;
                TxtExercise.Text = exerciseBox.Text;
                TxtSleep.Text    = sleepBox.Text;
                // TODO [SUPABASE]: await SupabaseService.UpdateLifestyleAsync(patientId, ...);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Stubs – Data Loading (Supabase ready)
        // ─────────────────────────────────────────────────────────
        private void LoadPatientData(int patientId)
        {
            // TODO [SUPABASE]: Bỏ comment và triển khai:
            //
            // var patient   = await supabase.From<Patient>().Where(x => x.Id == patientId).Single();
            // var vitals    = await supabase.From<VitalSigns>().Where(x => x.PatientId == patientId)
            //                     .Order(x => x.RecordedAt, Ordering.Descending).Limit(1).Single();
            // var allergies = await supabase.From<Allergy>().Where(x => x.PatientId == patientId).Get();
            // ... v.v.
            // this.DataContext = new PatientHistoryViewModel { Patient = patient, VitalSigns = vitals, ... };
        }

        // ─────────────────────────────────────────────────────────
        //  UI Helpers — tạo ContentDialog & controls động
        // ─────────────────────────────────────────────────────────

        private ContentDialog CreateDialog(string title, UIElement content, double minWidth = 420)
        {
            var scrollView = new ScrollViewer
            {
                Content         = content,
                MaxHeight       = 480,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            return new ContentDialog
            {
                Title               = title,
                Content             = scrollView,
                PrimaryButtonText   = "Lưu",
                CloseButtonText     = "Hủy",
                DefaultButton       = ContentDialogButton.Primary,
                XamlRoot            = this.XamlRoot,
                MinWidth            = minWidth
            };
        }

        private static TextBox CreateTextBox(string header, string value) => new TextBox
        {
            Header              = header,
            Text                = value,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        private static TextBlock CreateSectionHeader(string text) => new TextBlock
        {
            Text       = text,
            FontSize   = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin     = new Thickness(0, 8, 0, 0)
        };

        private static async System.Threading.Tasks.Task ShowInfoAsync(string title, string message)
        { /* no-op here — caller awaits dialog */ }

        private async void ShowInfo(string title, string message)
        {
            await new ContentDialog
            {
                Title           = title,
                Content         = message,
                CloseButtonText = "Đóng",
                XamlRoot        = this.XamlRoot
            }.ShowAsync();
        }

        /// <summary>Parse "#RRGGBB" hex string thành Windows.UI.Color.</summary>
        private static Color ParseColor(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
    }
}
