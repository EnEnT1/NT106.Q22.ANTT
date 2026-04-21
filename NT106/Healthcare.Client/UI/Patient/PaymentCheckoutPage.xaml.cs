using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Healthcare.Client.UI.Patient
{
    public class TransactionViewModel
    {
        public string Id { get; set; }
        public string AppointmentId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionRef { get; set; }
        public string Status { get; set; }
        
        public string AmountFormatted => string.Format("{0:N0} VNĐ", Amount);
        public string StatusText => (Status == "Success" || Status == "Paid") ? "Đã thanh toán" : "Chưa thanh toán";
        public string StatusBackgroundColor => (Status == "Success" || Status == "Paid") ? "#DCFCE7" : "#FEF9C3";
        public string StatusTextColor => (Status == "Success" || Status == "Paid") ? "#166534" : "#854D0E";

        public Transaction OriginalModel { get; set; }
    }

    public sealed partial class PaymentCheckoutPage : Page
    {
        public ObservableCollection<TransactionViewModel> TransactionsList { get; set; } = new();
        private TransactionViewModel _selectedTransaction;
        private Supabase.Client _supabase;

        public PaymentCheckoutPage()
        {
            this.InitializeComponent();
            _supabase = SupabaseManager.Instance.Client;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Ignore transactionId parameters since this page is now the history list
            await LoadTransactionsAsync();
        }

        private async Task LoadTransactionsAsync()
        {
            try
            {
                var user = SessionStorage.CurrentUser;
                if (user == null) return;

                var response = await _supabase.From<Transaction>()
                    .Where(x => x.PatientId == user.Id)
                    .Get();

                TransactionsList.Clear();
                // Sắp xếp ID mới nhất hoặc PaidAt. Vì đơn giản ta cứ lấy list. Cần thiết thì OrderByDescending(CreatedAt)
                var sorted = response.Models.OrderByDescending(x => x.Id).ToList();

                foreach (var trans in sorted)
                {
                    TransactionsList.Add(new TransactionViewModel
                    {
                        Id = trans.Id,
                        AppointmentId = "Lịch Khám: " + trans.AppointmentId.Substring(0, 6).ToUpper(), // mock ngắn
                        Amount = trans.Amount,
                        TransactionRef = trans.TransactionRef,
                        Status = trans.Status,
                        OriginalModel = trans
                    });
                }

                TransactionsListView.ItemsSource = TransactionsList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading transactions: " + ex.Message);
            }
        }

        private async void TransactionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = TransactionsListView.SelectedItem as TransactionViewModel;
            if (selected == null) return;

            // Bỏ chọn để user có thể click lại chính item đó nếu tắt popup
            TransactionsListView.SelectedItem = null;
            _selectedTransaction = selected;

            TransactionDetailDialog.XamlRoot = this.XamlRoot;
            DetailAmount.Text = selected.AmountFormatted;
            DetailRef.Text = selected.TransactionRef;
            DetailStatus.Text = selected.StatusText;
            DetailStatus.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(
                255, 
                (selected.Status == "Success" || selected.Status == "Paid") ? (byte)22 : (byte)133, 
                (selected.Status == "Success" || selected.Status == "Paid") ? (byte)101 : (byte)77, 
                (selected.Status == "Success" || selected.Status == "Paid") ? (byte)52 : (byte)14
            ));

            await TransactionDetailDialog.ShowAsync();
        }
    }
}
