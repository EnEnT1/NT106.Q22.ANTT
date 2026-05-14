using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Healthcare.Client.UI.Patient
{
    public sealed partial class HealthMetricsPage : Page
    {
        public HealthMetricsPage()
        {
            InitializeComponent();
        }

        // ─── Nút "Cập nhật chỉ số" ───────────────────────────────────────────
        private async void UpdateMetricsButton_Click(object sender, RoutedEventArgs e)
        {
            // Build form content
            var formGrid = new Grid { RowSpacing = 16 };
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Tiêu đề phụ
            var subtitle = new TextBlock
            {
                Text = "Nhập các chỉ số đo được gần nhất của bạn.",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(subtitle, 0);
            formGrid.Children.Add(subtitle);

            // --- Huyết áp ---
            var bpStack = BuildFieldGroup(
                "Huyết áp (mmHg)",
                "Tâm thu / Tâm trương",
                BloodPressureValue.Text,
                out var bpBox);
            Grid.SetRow(bpStack, 1);
            formGrid.Children.Add(bpStack);

            // --- HbA1c ---
            var hbStack = BuildFieldGroup(
                "HbA1c (%)",
                "VD: 5.1",
                HbA1cValue.Text,
                out var hbBox);
            Grid.SetRow(hbStack, 2);
            formGrid.Children.Add(hbStack);

            // --- Cholesterol ---
            var choStack = BuildFieldGroup(
                "Cholesterol (mg/dL)",
                "VD: 190",
                CholesterolValue.Text,
                out var choBox);
            Grid.SetRow(choStack, 3);
            formGrid.Children.Add(choStack);

            // --- Nhịp tim ---
            var hrStack = BuildFieldGroup(
                "Nhịp tim (bpm)",
                "VD: 54",
                HeartRateValue.Text,
                out var hrBox);
            Grid.SetRow(hrStack, 4);
            formGrid.Children.Add(hrStack);

            // ContentDialog
            var dialog = new ContentDialog
            {
                Title = "Cập nhật chỉ số sức khỏe",
                Content = formGrid,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                MinWidth = 420
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                ApplyMetricUpdates(
                    bpValue: bpBox.Text.Trim(),
                    hbValue: hbBox.Text.Trim(),
                    choValue: choBox.Text.Trim(),
                    hrValue: hrBox.Text.Trim());
            }
        }

        // ─── Tạo nhóm label + TextBox ────────────────────────────────────────
        private static StackPanel BuildFieldGroup(
            string label,
            string placeholder,
            string currentValue,
            out TextBox textBox)
        {
            var stack = new StackPanel { Spacing = 6 };

            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 65, 85))
            });

            var box = new TextBox
            {
                PlaceholderText = placeholder,
                Text = currentValue,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                FontSize = 14
            };
            stack.Children.Add(box);

            textBox = box;
            return stack;
        }

        // ─── Áp dụng giá trị mới lên UI ─────────────────────────────────────
        private void ApplyMetricUpdates(
            string bpValue,
            string hbValue,
            string choValue,
            string hrValue)
        {
            // Huyết áp
            if (!string.IsNullOrWhiteSpace(bpValue))
            {
                BloodPressureValue.Text = bpValue;
                BloodPressureStatus.Text = EvaluateBloodPressure(bpValue);
            }

            // HbA1c
            if (double.TryParse(hbValue, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double hbNum))
            {
                HbA1cValue.Text = hbNum.ToString("0.0");
                HbA1cStatus.Text = hbNum < 5.7 ? "Tối ưu" : hbNum < 6.5 ? "Tiền tiểu đường" : "Cần theo dõi";
            }

            // Cholesterol
            if (int.TryParse(choValue, out int choNum))
            {
                CholesterolValue.Text = choNum.ToString();
                var (choStatus, choBrush) = choNum < 180
                    ? ("Bình thường", Color.FromArgb(255, 34, 197, 94))
                    : choNum < 200
                        ? ("Cận cao", Color.FromArgb(255, 245, 158, 11))
                        : ("Cao", Color.FromArgb(255, 239, 68, 68));
                CholesterolStatus.Text = choStatus;
                CholesterolStatus.Foreground = new SolidColorBrush(choBrush);
            }

            // Nhịp tim
            if (int.TryParse(hrValue, out int hrNum))
            {
                HeartRateValue.Text = hrNum.ToString();
                HeartRateTime.Text = DateTime.Now.ToString("hh:mm tt");
            }
        }

        // ─── Phân loại huyết áp đơn giản ─────────────────────────────────────
        private static string EvaluateBloodPressure(string bpText)
        {
            // Định dạng mong đợi: "120/80"
            var parts = bpText.Split('/');
            if (parts.Length != 2) return "Không rõ";
            if (!int.TryParse(parts[0].Trim(), out int sys)) return "Không rõ";
            if (!int.TryParse(parts[1].Trim(), out int dia)) return "Không rõ";

            if (sys < 120 && dia < 80) return "Bình thường";
            if (sys < 130 && dia < 80) return "Cao - Giai đoạn 1";
            if (sys < 140 || dia < 90) return "Cao - Giai đoạn 2";
            return "Tăng huyết áp";
        }
    }
}
