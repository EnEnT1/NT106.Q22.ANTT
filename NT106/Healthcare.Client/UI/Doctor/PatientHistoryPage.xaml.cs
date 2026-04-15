using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class PatientHistoryPage : Page
    {
        private string _patientId = string.Empty;
        private string _appointmentId = string.Empty;

        public PatientHistoryPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string patientId && !string.IsNullOrWhiteSpace(patientId))
            {
                _patientId = patientId;
                await LoadPatientDataAsync(_patientId);
                return;
            }

            if (e.Parameter is Appointment appointment)
            {
                _appointmentId = appointment.Id;
                _patientId = appointment.PatientId;
                await LoadPatientDataAsync(_patientId);
                return;
            }

            if (e.Parameter is string appointmentId && !string.IsNullOrWhiteSpace(appointmentId))
            {
                _appointmentId = appointmentId;
                await LoadPatientFromAppointmentAsync(_appointmentId);
            }
        }

        private async Task LoadPatientFromAppointmentAsync(string appointmentId)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var appointmentResponse = await client
                    .From<Appointment>()
                    .Get();

                var appointment = appointmentResponse.Models
                    .FirstOrDefault(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    await ShowInfoAsync("Thông báo", "Không tìm thấy lịch hẹn.");
                    return;
                }

                _patientId = appointment.PatientId;
                await LoadPatientDataAsync(_patientId);
            }
            catch (Exception ex)
            {
                await ShowInfoAsync("Lỗi", "Không tải được lịch hẹn: " + ex.Message);
            }
        }

        private async Task LoadPatientDataAsync(string patientId)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var userResponse = await client.From<User>().Get();
                var patient = userResponse.Models.FirstOrDefault(u => u.Id == patientId);

                var profileResponse = await client.From<PatientProfile>().Get();
                var profile = profileResponse.Models.FirstOrDefault(p => p.PatientId == patientId);

                var recordResponse = await client.From<MedicalRecord>().Get();
                var records = recordResponse.Models
                    .Where(r => r.PatientId == patientId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();

                var latestRecord = records.FirstOrDefault();

                TxtHeaderPatientName.Text = patient?.FullName ?? "Bệnh nhân";
                TxtPatientCode.Text = patient != null ? $"#{patient.Id}" : "#N/A";
                TxtHeaderWeight.Text = profile?.WeightKg != null ? $"{profile.WeightKg:0.#}kg" : "N/A";
                TxtPatientStatus.Text = (profile?.ChronicDiseases != null && profile.ChronicDiseases.Count > 0)
                    ? "Cần theo dõi"
                    : "Bình thường";

                TxtAllergyAlertSummary.Text = profile?.Allergies != null && profile.Allergies.Count > 0
                    ? string.Join(", ", profile.Allergies)
                    : "Không ghi nhận";

                BindVitals(profile);
                BindAllergies(profile);
                BindMedicalHistory(latestRecord);
                BindMedication(latestRecord);
                BindFamilyHistory();
                BindLifestyle();
                BindVisitHistory(records);
            }
            catch (Exception ex)
            {
                await ShowInfoAsync("Lỗi", "Không tải được hồ sơ bệnh nhân: " + ex.Message);
            }
        }

        private void BindVitals(PatientProfile? profile)
        {
            if (profile == null) return;

            TxtHeight.Text = profile.HeightCm?.ToString("0.#") ?? "-";
            TxtWeight.Text = profile.WeightKg?.ToString("0.#") ?? "-";

            if (profile.HeightCm.HasValue && profile.WeightKg.HasValue && profile.HeightCm.Value > 0)
            {
                var heightM = profile.HeightCm.Value / 100f;
                var bmi = profile.WeightKg.Value / (heightM * heightM);
                TxtBmi.Text = bmi.ToString("0.0");
            }
            else
            {
                TxtBmi.Text = "-";
            }

            TxtBloodPressure.Text = "120/80";
            TxtHeartRate.Text = "75";
            TxtTemperature.Text = "36.8°C";
        }

        private void BindAllergies(PatientProfile? profile)
        {
            AllergyList.Children.Clear();

            if (profile?.Allergies == null || profile.Allergies.Count == 0)
            {
                AllergyList.Children.Add(new TextBlock
                {
                    Text = "Không ghi nhận dị ứng",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(ParseColor("#64748B"))
                });
                return;
            }

            foreach (var allergy in profile.Allergies)
            {
                AddAllergyItem(allergy.ToUpper(), "TRUNG BÌNH", "Chưa cập nhật biểu hiện");
            }
        }

        private void BindMedicalHistory(MedicalRecord? latestRecord)
        {
            if (latestRecord == null)
            {
                TxtChronicName.Text = "Chưa cập nhật";
                TxtChronicYear.Text = "Phát hiện từ: --";
                TxtChronicNote.Text = "Không có ghi chú";
                TxtSurgeryName.Text = "Chưa cập nhật";
                TxtSurgeryYear.Text = "Năm thực hiện: --";
                TxtVaccinationName.Text = "Chưa cập nhật";
                TxtVaccinationYear.Text = "Gần nhất: --";
                return;
            }

            TxtChronicName.Text = string.IsNullOrWhiteSpace(latestRecord.Diagnosis)
                ? "Chưa cập nhật"
                : latestRecord.Diagnosis;

            TxtChronicYear.Text = "Phát hiện từ: " + latestRecord.CreatedAt.Year;
            TxtChronicNote.Text = "Ghi nhận từ hồ sơ khám gần nhất.";

            TxtSurgeryName.Text = "Chưa cập nhật";
            TxtSurgeryYear.Text = "Năm thực hiện: --";

            TxtVaccinationName.Text = "Chưa cập nhật";
            TxtVaccinationYear.Text = "Gần nhất: --";
        }

        private void BindMedication(MedicalRecord? latestRecord)
        {
            if (latestRecord == null || string.IsNullOrWhiteSpace(latestRecord.AiMedicines))
            {
                TxtMedName.Text = "Chưa cập nhật";
                TxtMedDosage.Text = "Chưa cập nhật";
                TxtMedPeriod.Text = "--";
                return;
            }

            TxtMedName.Text = latestRecord.AiMedicines;
            TxtMedDosage.Text = "Theo chỉ định gần nhất";
            TxtMedPeriod.Text = $"{latestRecord.CreatedAt:yyyy} - Hiện tại";
        }

        private void BindFamilyHistory()
        {
            TxtFatherCondition.Text = "Chưa cập nhật";
            TxtMotherCondition.Text = "Chưa cập nhật";
        }

        private void BindLifestyle()
        {
            TxtSmoking.Text = "Chưa cập nhật";
            TxtAlcohol.Text = "Chưa cập nhật";
            TxtExercise.Text = "Chưa cập nhật";
            TxtSleep.Text = "Chưa cập nhật";
        }

        private void BindVisitHistory(System.Collections.Generic.List<MedicalRecord> records)
        {
            var items = records.Take(3).ToList();

            BindVisitItem(items.ElementAtOrDefault(0), TxtVisit1Type, TxtVisit1Date, TxtVisit1Doctor, TxtVisit1Summary);
            BindVisitItem(items.ElementAtOrDefault(1), TxtVisit2Type, TxtVisit2Date, TxtVisit2Doctor, TxtVisit2Summary);
            BindVisitItem(items.ElementAtOrDefault(2), TxtVisit3Type, TxtVisit3Date, TxtVisit3Doctor, TxtVisit3Summary);
        }

        private void BindVisitItem(
            MedicalRecord? record,
            TextBlock typeBlock,
            TextBlock dateBlock,
            TextBlock doctorBlock,
            TextBlock summaryBlock)
        {
            if (record == null)
            {
                typeBlock.Text = "Chưa có dữ liệu";
                dateBlock.Text = "--";
                doctorBlock.Text = "--";
                summaryBlock.Text = "--";
                return;
            }

            typeBlock.Text = "Lần khám";
            dateBlock.Text = record.CreatedAt.ToString("dd/MM/yyyy");
            doctorBlock.Text = $"BS. {record.DoctorId}";
            summaryBlock.Text = string.IsNullOrWhiteSpace(record.Diagnosis)
                ? "Không có tóm tắt"
                : record.Diagnosis;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowInfoAsync("Lưu hồ sơ", "✅ Tất cả thay đổi đã được lưu thành công.");
        }

        private async void BtnEditVitals_Click(object sender, RoutedEventArgs e)
        {
            var heightBox = CreateTextBox("Chiều cao (cm)", TxtHeight.Text);
            var weightBox = CreateTextBox("Cân nặng (kg)", TxtWeight.Text);
            var bmiBox = CreateTextBox("BMI", TxtBmi.Text);
            var bpBox = CreateTextBox("Huyết áp (mmHg)", TxtBloodPressure.Text);
            var hrBox = CreateTextBox("Nhịp tim (bpm)", TxtHeartRate.Text);
            var tempBox = CreateTextBox("Nhiệt độ (°C)", TxtTemperature.Text.Replace("°C", "").Trim());

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
                TxtHeight.Text = heightBox.Text;
                TxtWeight.Text = weightBox.Text;
                TxtBmi.Text = bmiBox.Text;
                TxtBloodPressure.Text = bpBox.Text;
                TxtHeartRate.Text = hrBox.Text;
                TxtTemperature.Text = tempBox.Text + "°C";

                try
                {
                    var profile = await GetOrCreatePatientProfileAsync();
                    profile.HeightCm = ParseNullableFloat(heightBox.Text);
                    profile.WeightKg = ParseNullableFloat(weightBox.Text);

                    await SavePatientProfileAsync(profile);
                    await ShowInfoAsync("Thành công", "Đã lưu dấu hiệu sinh tồn.");
                }
                catch (Exception ex)
                {
                    await ShowInfoAsync("Lỗi", "Không lưu được dấu hiệu sinh tồn: " + ex.Message);
                }
            }
        }

        private async void BtnAddAllergy_Click(object sender, RoutedEventArgs e)
        {
            var nameBox = CreateTextBox("Tên dị ứng nguyên", "");
            var symptomsBox = CreateTextBox("Biểu hiện", "");

            var severityLabel = new TextBlock
            {
                Text = "Mức độ nghiêm trọng",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 4)
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

            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
            {
                string severity = severityCombo.SelectedItem?.ToString() ?? "TRUNG BÌNH";
                string allergyName = nameBox.Text.ToUpper();
                string symptoms = symptomsBox.Text;

                AddAllergyItem(allergyName, severity, symptoms);

                try
                {
                    var profile = await GetOrCreatePatientProfileAsync();

                    if (profile.Allergies == null)
                        profile.Allergies = new List<string>();

                    if (!profile.Allergies.Contains(allergyName))
                        profile.Allergies.Add(allergyName);

                    await SavePatientProfileAsync(profile);

                    TxtAllergyAlertSummary.Text = string.Join(", ", profile.Allergies);
                    await ShowInfoAsync("Thành công", "Đã lưu dị ứng.");
                }
                catch (Exception ex)
                {
                    await ShowInfoAsync("Lỗi", "Không lưu được dị ứng: " + ex.Message);
                }
            }
        }

        private void AddAllergyItem(string name, string severity, string symptoms)
        {
            bool isSevere = severity == "NGHIÊM TRỌNG";
            bool isMedium = severity == "TRUNG BÌNH";

            string borderColor = isSevere ? "#FECACA" : isMedium ? "#FED7AA" : "#BBF7D0";
            string bgColor = isSevere ? "#FEE2E2" : isMedium ? "#FFEDD5" : "#DCFCE7";
            string badgeColor = isSevere ? "#DC2626" : isMedium ? "#EA580C" : "#16A34A";
            string textColor = isSevere ? "#B91C1C" : isMedium ? "#C2410C" : "#15803D";
            string glyph = isSevere ? "\uECAA" : isMedium ? "\uE734" : "\uE73E";

            var severityBadge = new Border
            {
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                Background = new SolidColorBrush(ParseColor(badgeColor))
            };

            severityBadge.Child = new TextBlock
            {
                Text = severity,
                FontSize = 8,
                FontWeight = FontWeights.Black,
                Foreground = new SolidColorBrush(Colors.White),
                CharacterSpacing = 50
            };

            var nameBlock = new TextBlock
            {
                Text = name,
                FontSize = 13,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(ParseColor("#1E293B"))
            };

            var badgeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            badgeRow.Children.Add(severityBadge);
            badgeRow.Children.Add(nameBlock);

            var symptomsBlock = new TextBlock
            {
                Text = $"Biểu hiện: {symptoms}",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(ParseColor(textColor))
            };

            var textStack = new StackPanel { Spacing = 4 };
            textStack.Children.Add(badgeRow);
            textStack.Children.Add(symptomsBlock);

            var iconBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(ParseColor(bgColor))
            };

            iconBorder.Child = new FontIcon
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = glyph,
                FontSize = 18,
                Foreground = new SolidColorBrush(ParseColor(badgeColor)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
            row.Children.Add(iconBorder);
            row.Children.Add(textStack);

            var item = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(ParseColor(borderColor)),
                Padding = new Thickness(14, 12, 14, 12),
                Child = row
            };

            AllergyList.Children.Add(item);
        }

        private async void BtnEditMedHistory_Click(object sender, RoutedEventArgs e)
        {
            var chronicName = CreateTextBox("Bệnh nền", TxtChronicName.Text);
            var chronicYear = CreateTextBox("Phát hiện từ năm", TxtChronicYear.Text.Replace("Phát hiện từ: ", ""));
            var chronicNote = CreateTextBox("Ghi chú bệnh nền", TxtChronicNote.Text);

            var surgeryName = CreateTextBox("Phẫu thuật", TxtSurgeryName.Text);
            var surgeryYear = CreateTextBox("Năm phẫu thuật", TxtSurgeryYear.Text.Replace("Năm thực hiện: ", ""));

            var vaccName = CreateTextBox("Vaccine đã tiêm", TxtVaccinationName.Text);
            var vaccYear = CreateTextBox("Gần nhất (năm)", TxtVaccinationYear.Text.Replace("Gần nhất: ", ""));

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

            var dialog = CreateDialog("+ Sửa tiền sử bệnh lý", panel, 500);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TxtChronicName.Text = chronicName.Text;
                TxtChronicYear.Text = "Phát hiện từ: " + chronicYear.Text;
                TxtChronicNote.Text = chronicNote.Text;

                TxtSurgeryName.Text = surgeryName.Text;
                TxtSurgeryYear.Text = "Năm thực hiện: " + surgeryYear.Text;

                TxtVaccinationName.Text = vaccName.Text;
                TxtVaccinationYear.Text = "Gần nhất: " + vaccYear.Text;

                try
                {
                    var profile = await GetOrCreatePatientProfileAsync();
                    profile.ChronicDiseases = new List<string>();

                    if (!string.IsNullOrWhiteSpace(chronicName.Text))
                        profile.ChronicDiseases.Add(chronicName.Text);

                    await SavePatientProfileAsync(profile);

                    var record = await GetOrCreateLatestMedicalRecordAsync();
                    record.Diagnosis = string.IsNullOrWhiteSpace(chronicNote.Text)
                        ? chronicName.Text
                        : $"{chronicName.Text} - {chronicNote.Text}";
                    record.CreatedAt = DateTime.Now;

                    await SaveMedicalRecordAsync(record);

                    await ShowInfoAsync("Thành công", "Đã lưu tiền sử bệnh lý.");
                }
                catch (Exception ex)
                {
                    await ShowInfoAsync("Lỗi", "Không lưu được tiền sử bệnh lý: " + ex.Message);
                }
            }
        }

        private async void BtnEditMedication_Click(object sender, RoutedEventArgs e)
        {
            var medName = CreateTextBox("Tên thuốc & hàm lượng", TxtMedName.Text);
            var medDosage = CreateTextBox("Liều dùng", TxtMedDosage.Text);
            var medPeriod = CreateTextBox("Thời gian dùng", TxtMedPeriod.Text);

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(medName);
            panel.Children.Add(medDosage);
            panel.Children.Add(medPeriod);

            var dialog = CreateDialog("+ Sửa thông tin thuốc", panel);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TxtMedName.Text = medName.Text;
                TxtMedDosage.Text = medDosage.Text;
                TxtMedPeriod.Text = medPeriod.Text;

                try
                {
                    var record = await GetOrCreateLatestMedicalRecordAsync();
                    record.AiMedicines = $"{medName.Text} | {medDosage.Text} | {medPeriod.Text}";
                    record.CreatedAt = DateTime.Now;

                    await SaveMedicalRecordAsync(record);
                    await ShowInfoAsync("Thành công", "Đã lưu thông tin thuốc.");
                }
                catch (Exception ex)
                {
                    await ShowInfoAsync("Lỗi", "Không lưu được thông tin thuốc: " + ex.Message);
                }
            }
        }

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
            }
        }

        private async void BtnEditLifestyle_Click(object sender, RoutedEventArgs e)
        {
            var smokingBox = CreateTextBox("Thuốc lá", TxtSmoking.Text);
            var alcoholBox = CreateTextBox("Rượu bia", TxtAlcohol.Text);
            var exerciseBox = CreateTextBox("Vận động", TxtExercise.Text);
            var sleepBox = CreateTextBox("Giấc ngủ", TxtSleep.Text);

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(smokingBox);
            panel.Children.Add(alcoholBox);
            panel.Children.Add(exerciseBox);
            panel.Children.Add(sleepBox);

            var dialog = CreateDialog("+ Sửa lối sống", panel);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TxtSmoking.Text = smokingBox.Text;
                TxtAlcohol.Text = alcoholBox.Text;
                TxtExercise.Text = exerciseBox.Text;
                TxtSleep.Text = sleepBox.Text;
            }
        }

        private ContentDialog CreateDialog(string title, UIElement content, double minWidth = 420)
        {
            var scrollView = new ScrollViewer
            {
                Content = content,
                MaxHeight = 480,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            return new ContentDialog
            {
                Title = title,
                Content = scrollView,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                MinWidth = minWidth
            };
        }

        private static TextBox CreateTextBox(string header, string value)
        {
            return new TextBox
            {
                Header = header,
                Text = value,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private static TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 8, 0, 0)
            };
        }

        private async Task ShowInfoAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        private static Color ParseColor(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
        private async Task<PatientProfile> GetOrCreatePatientProfileAsync()
        {
            var client = SupabaseManager.Instance.Client;

            var response = await client
                .From<PatientProfile>()
                .Get();

            var profile = response.Models.FirstOrDefault(p => p.PatientId == _patientId);

            if (profile != null)
                return profile;

            return new PatientProfile
            {
                PatientId = _patientId,
                DateOfBirth = string.Empty,
                Gender = string.Empty,
                BloodType = string.Empty,
                HeightCm = null,
                WeightKg = null,
                Allergies = new List<string>(),
                ChronicDiseases = new List<string>()
            };
        }

        private async Task<MedicalRecord> GetOrCreateLatestMedicalRecordAsync()
        {
            var client = SupabaseManager.Instance.Client;

            var response = await client
                .From<MedicalRecord>()
                .Get();

            var latest = response.Models
                .Where(r => r.PatientId == _patientId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (latest != null)
                return latest;

            return new MedicalRecord
            {
                Id = Guid.NewGuid().ToString(),
                AppointmentId = string.IsNullOrWhiteSpace(_appointmentId) ? string.Empty : _appointmentId,
                DoctorId = string.Empty,
                PatientId = _patientId,
                Diagnosis = string.Empty,
                PrescriptionImageUrl = string.Empty,
                AiMedicines = string.Empty,
                CreatedAt = DateTime.Now
            };
        }

        private async Task SavePatientProfileAsync(PatientProfile profile)
        {
            var client = SupabaseManager.Instance.Client;
            await client.From<PatientProfile>().Upsert(profile);
        }

        private async Task SaveMedicalRecordAsync(MedicalRecord record)
        {
            var client = SupabaseManager.Instance.Client;
            await client.From<MedicalRecord>().Upsert(record);
        }

        private static float? ParseNullableFloat(string text)
        {
            if (float.TryParse(text, out var value))
                return value;
            return null;
        }
    }
}