using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class PatientHistoryPage : Page
    {
        private readonly ObservableCollection<PatientListItemViewModel> _patients = new();
        private readonly ObservableCollection<VisitHistoryViewModel> _visits = new();

        private List<PatientListItemViewModel> _allPatients = new();
        private List<PatientProfile> _profiles = new();
        private List<MedicalRecord> _records = new();
        private List<Appointment> _appointments = new();

        private string _selectedPatientId = string.Empty;

        public PatientHistoryPage()
        {
            this.InitializeComponent();

            PatientListView.ItemsSource = _patients;
            VisitHistoryListView.ItemsSource = _visits;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await LoadPatientsAsync();

            if (e.Parameter is string id && !string.IsNullOrWhiteSpace(id))
            {
                await TrySelectPatientFromParameterAsync(id);
            }
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                string? doctorId = SessionStorage.CurrentUser?.Id;

                var userResponse = await client.From<User>().Get();
                var users = userResponse.Models ?? new List<User>();

                var profileResponse = await client.From<PatientProfile>().Get();
                _profiles = profileResponse.Models ?? new List<PatientProfile>();

                var recordResponse = await client.From<MedicalRecord>().Get();
                _records = recordResponse.Models ?? new List<MedicalRecord>();

                var appointmentResponse = await client.From<Appointment>().Get();
                _appointments = appointmentResponse.Models ?? new List<Appointment>();

                var patientIdsByDoctor = string.IsNullOrWhiteSpace(doctorId)
                    ? new HashSet<string>()
                    : _appointments
                        .Where(a => a.DoctorId == doctorId)
                        .Select(a => a.PatientId)
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .ToHashSet();

                var patientUsers = users
                    .Where(u =>
                        (!string.IsNullOrWhiteSpace(u.Role) &&
                         u.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                        || patientIdsByDoctor.Contains(u.Id))
                    .ToList();

                if (patientIdsByDoctor.Count > 0)
                {
                    patientUsers = patientUsers
                        .Where(u => patientIdsByDoctor.Contains(u.Id))
                        .ToList();
                }

                _allPatients = patientUsers
                    .OrderBy(u => u.FullName)
                    .Select(user =>
                    {
                        var profile = _profiles.FirstOrDefault(p => p.PatientId == user.Id);
                        var records = _records.Where(r => r.PatientId == user.Id).ToList();

                        bool needFollow = profile?.ChronicDiseases != null && profile.ChronicDiseases.Count > 0;
                        bool hasAllergy = profile?.Allergies != null && profile.Allergies.Count > 0;

                        string status = needFollow || hasAllergy ? "Cần theo dõi" : "Bình thường";

                        return new PatientListItemViewModel
                        {
                            PatientId = user.Id,
                            FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Bệnh nhân" : user.FullName,
                            PatientCode = "#" + ShortId(user.Id),
                            Initials = GetInitials(user.FullName),
                            StatusText = status,
                            VisitCountText = $"{records.Count} lần khám",
                            AvatarBackground = new SolidColorBrush(ParseColor("#E0F2FE")),
                            AvatarForeground = new SolidColorBrush(ParseColor("#0369A1")),
                            StatusBackground = new SolidColorBrush(ParseColor(status == "Cần theo dõi" ? "#FEF3C7" : "#DCFCE7")),
                            StatusForeground = new SolidColorBrush(ParseColor(status == "Cần theo dõi" ? "#B45309" : "#15803D"))
                        };
                    })
                    .ToList();

                ApplyPatientFilter();
                TxtPatientCount.Text = $"{_allPatients.Count} bệnh nhân";

                if (_allPatients.Count == 0)
                {
                    ShowEmptyState("Chưa có bệnh nhân trong danh sách.");
                }
            }
            catch (Exception ex)
            {
                await ShowInfoAsync("Lỗi", "Không tải được danh sách bệnh nhân: " + ex.Message);
            }
        }

        private async Task TrySelectPatientFromParameterAsync(string id)
        {
            var patient = _allPatients.FirstOrDefault(p => p.PatientId == id);

            if (patient == null)
            {
                var appointment = _appointments.FirstOrDefault(a => a.Id == id);
                if (appointment != null)
                {
                    patient = _allPatients.FirstOrDefault(p => p.PatientId == appointment.PatientId);
                }
            }

            if (patient != null)
            {
                PatientListView.SelectedItem = patient;
                await LoadPatientDetailAsync(patient.PatientId);
            }
        }

        private async void PatientListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PatientListItemViewModel patient)
            {
                PatientListView.SelectedItem = patient;
                await LoadPatientDetailAsync(patient.PatientId);
            }
        }

        private void SearchPatientBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyPatientFilter();
        }

        private void ApplyPatientFilter()
        {
            string keyword = SearchPatientBox.Text.Trim().ToLower();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allPatients
                : _allPatients
                    .Where(p =>
                        p.FullName.ToLower().Contains(keyword) ||
                        p.PatientCode.ToLower().Contains(keyword))
                    .ToList();

            _patients.Clear();

            foreach (var patient in filtered)
            {
                _patients.Add(patient);
            }

            TxtPatientCount.Text = $"{filtered.Count} bệnh nhân";
        }

        private async Task LoadPatientDetailAsync(string patientId)
        {
            try
            {
                _selectedPatientId = patientId;

                var patient = _allPatients.FirstOrDefault(p => p.PatientId == patientId);
                var profile = _profiles.FirstOrDefault(p => p.PatientId == patientId);
                var records = _records
                    .Where(r => r.PatientId == patientId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();

                var latestRecord = records.FirstOrDefault();

                EmptyStatePanel.Visibility = Visibility.Collapsed;
                PatientDetailScrollViewer.Visibility = Visibility.Visible;

                TxtHeaderPatientName.Text = patient?.FullName ?? "Bệnh nhân";
                TxtPatientCode.Text = patient?.PatientCode ?? "#N/A";
                TxtHeaderWeight.Text = profile?.WeightKg != null ? $"{profile.WeightKg:0.#}kg" : "N/A";

                bool needFollow = profile?.ChronicDiseases != null && profile.ChronicDiseases.Count > 0;
                bool hasAllergy = profile?.Allergies != null && profile.Allergies.Count > 0;

                TxtPatientStatus.Text = needFollow || hasAllergy ? "Cần theo dõi" : "Bình thường";

                TxtAllergyAlertSummary.Text = profile?.Allergies != null && profile.Allergies.Count > 0
                    ? string.Join(", ", profile.Allergies)
                    : "Không ghi nhận";

                BindVitals(profile);
                BindAllergies(profile);
                BindMedicalHistory(profile, latestRecord);
                BindMedication(latestRecord);
                BindFamilyHistory();
                BindLifestyle();
                BindVisitHistory(records);

                PatientDetailScrollViewer.ChangeView(null, 0, null);
            }
            catch (Exception ex)
            {
                await ShowInfoAsync("Lỗi", "Không tải được hồ sơ bệnh nhân: " + ex.Message);
            }
        }

        private void ShowEmptyState(string message)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            PatientDetailScrollViewer.Visibility = Visibility.Collapsed;
            TxtVisitSubtitle.Text = message;
            _visits.Clear();
        }

        private void BindVitals(PatientProfile? profile)
        {
            TxtHeight.Text = profile?.HeightCm?.ToString("0.#") ?? "-";
            TxtWeight.Text = profile?.WeightKg?.ToString("0.#") ?? "-";

            if (profile?.HeightCm != null && profile.WeightKg != null && profile.HeightCm.Value > 0)
            {
                float heightM = profile.HeightCm.Value / 100f;
                float bmi = profile.WeightKg.Value / (heightM * heightM);
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

        private void BindMedicalHistory(PatientProfile? profile, MedicalRecord? latestRecord)
        {
            if (profile?.ChronicDiseases != null && profile.ChronicDiseases.Count > 0)
            {
                TxtChronicName.Text = string.Join(", ", profile.ChronicDiseases);
                TxtChronicYear.Text = latestRecord != null
                    ? "Phát hiện từ: " + latestRecord.CreatedAt.Year
                    : "Phát hiện từ: --";
                TxtChronicNote.Text = latestRecord?.Diagnosis ?? "Không có ghi chú";
            }
            else if (latestRecord != null && !string.IsNullOrWhiteSpace(latestRecord.Diagnosis))
            {
                TxtChronicName.Text = latestRecord.Diagnosis;
                TxtChronicYear.Text = "Phát hiện từ: " + latestRecord.CreatedAt.Year;
                TxtChronicNote.Text = "Ghi nhận từ lần khám gần nhất.";
            }
            else
            {
                TxtChronicName.Text = "Chưa cập nhật";
                TxtChronicYear.Text = "Phát hiện từ: --";
                TxtChronicNote.Text = "Không có ghi chú";
            }

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

        private void BindVisitHistory(List<MedicalRecord> records)
        {
            _visits.Clear();

            TxtVisitSubtitle.Text = string.IsNullOrWhiteSpace(_selectedPatientId)
                ? "Chưa chọn bệnh nhân"
                : $"{records.Count} lần khám được ghi nhận";

            if (records.Count == 0)
            {
                _visits.Add(new VisitHistoryViewModel
                {
                    Title = "Chưa có dữ liệu",
                    DateText = "--",
                    DoctorText = "--",
                    Summary = "Bệnh nhân chưa có hồ sơ khám.",
                    Background = new SolidColorBrush(ParseColor("#F8FAFC"))
                });
                return;
            }

            foreach (var record in records)
            {
                _visits.Add(new VisitHistoryViewModel
                {
                    Title = "Lần khám",
                    DateText = record.CreatedAt.ToString("dd/MM/yyyy"),
                    DoctorText = "BS. " + ShortId(record.DoctorId),
                    Summary = string.IsNullOrWhiteSpace(record.Diagnosis)
                        ? "Không có tóm tắt"
                        : record.Diagnosis,
                    Background = new SolidColorBrush(ParseColor("#EFF6FF"))
                });
            }
        }

        private void AddAllergyItem(string name, string severity, string symptoms)
        {
            var item = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(ParseColor("#FECACA")),
                Padding = new Thickness(14, 12, 14, 12)
            };

            var stack = new StackPanel { Spacing = 4 };

            stack.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 13,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(ParseColor("#B91C1C"))
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Mức độ: {severity}",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(ParseColor("#DC2626"))
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Biểu hiện: {symptoms}",
                FontSize = 11,
                Foreground = new SolidColorBrush(ParseColor("#64748B"))
            });

            item.Child = stack;
            AllergyList.Children.Add(item);
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

        private static string ShortId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "N/A";

            return id.Length > 6 ? id.Substring(0, 6).ToUpper() : id.ToUpper();
        }

        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "BN";

            var parts = fullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (parts.Count == 1)
                return parts[0].Substring(0, 1).ToUpper();

            return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpper();
        }

        private static Color ParseColor(string hex)
        {
            hex = hex.TrimStart('#');

            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)
            );
        }
    }

    public class PatientListItemViewModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PatientCode { get; set; } = string.Empty;
        public string Initials { get; set; } = "BN";
        public string StatusText { get; set; } = "Bình thường";
        public string VisitCountText { get; set; } = "0 lần khám";

        public Brush AvatarBackground { get; set; } = new SolidColorBrush(Colors.LightGray);
        public Brush AvatarForeground { get; set; } = new SolidColorBrush(Colors.Black);
        public Brush StatusBackground { get; set; } = new SolidColorBrush(Colors.LightGray);
        public Brush StatusForeground { get; set; } = new SolidColorBrush(Colors.Black);
    }

    public class VisitHistoryViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string DoctorText { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public Brush Background { get; set; } = new SolidColorBrush(Colors.White);
    }
}