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
    }

    public class ActiveApptViewModel
    {
        public string Id { get; set; }
        public string DoctorName { get; set; }
    }

    public sealed partial class MyRecordsPage : Page
    {

        private ObservableCollection<RecordViewModel> _records = new();
        private ObservableCollection<ActiveApptViewModel> _activeAppts = new();

        public MyRecordsPage()
        {
            this.InitializeComponent();
            RecordsList.ItemsSource = _records;
            ActiveApptsList.ItemsSource = _activeAppts;
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
            _activeAppts.Clear();
            EmptyStateText.Visibility = Visibility.Collapsed;
            ActiveApptsSection.Visibility = Visibility.Collapsed;

            try
            {
                var client = SupabaseManager.Instance.Client;
                var currentUserId = SessionStorage.CurrentUser?.Id;

                if (string.IsNullOrEmpty(currentUserId)) return;

                // 1. Fetch active online appointments to join
                var apptsResponse = await client.From<Appointment>()
                    .Where(x => x.PatientId == currentUserId)
                    .Where(x => x.Status == "Confirmed")
                    .Where(x => x.ExaminationType == "Online")
                    .Get();

                // Simple check: if appointment is today, show it
                var today = DateTime.Today;
                var activeList = apptsResponse.Models
                    .Where(a => a.AppointmentDate.Date == today)
                    .ToList();

                if (activeList.Any())
                {
                    ActiveApptsSection.Visibility = Visibility.Visible;
                    foreach (var appt in activeList)
                    {
                        var doc = await GetDoctorName(appt.DoctorId);
                        _activeAppts.Add(new ActiveApptViewModel { Id = appt.Id, DoctorName = "Bác sĩ: " + doc });
                    }
                }

                // 2. Fetch Medical Records (History)
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
                        if (!string.IsNullOrEmpty(rec.AiMedicines))
                        {
                            medList = rec.AiMedicines;
                        }

                        _records.Add(new RecordViewModel
                        {
                            Id = rec.Id,
                            DateStr = rec.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                            DoctorName = "BS. " + docName,
                            Diagnosis = rec.Diagnosis,
                            Prescription = string.IsNullOrEmpty(rec.PrescriptionImageUrl) ? medList : "Ghi chú: Xem ảnh đơn thuốc",
                            AppointmentId = rec.AppointmentId
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

        private void BtnJoinCall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string apptId)
            {
                this.Frame.Navigate(typeof(OnlineConsultationPage), apptId);
            }
        }
    }
}
