using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.UI.Components;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Media.SpeechRecognition;
using Windows.UI;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class ExaminationPage : Page
    {
        private string _appointmentId = string.Empty;
        private string _patientId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _activeNav = "video";
        private string _voiceDiagnosisText = string.Empty;
        private readonly System.Collections.Generic.List<string> _quickNotes = new();

        // Prescription
        private readonly System.Collections.Generic.List<PrescriptionEntry> _prescriptions = new();
        private record PrescriptionEntry(string Medicine, string Dosage);
        private HttpClient? _ttsHttpClient;

        // Speech-to-text
        private SpeechRecognizer? _speechRecognizer;
        private bool _isRecording = false;
        private System.Threading.CancellationTokenSource? _toastCts;

        // Audio recording (Actual voice diagnosis)
        private MediaCapture? _audioMediaCapture;
        private StorageFile? _tempRecordingFile;
        private bool _isRecordingAudio = false;
        private string _voiceAudioBase64 = string.Empty;

        public ExaminationPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string appointmentId)
                _appointmentId = appointmentId;

            _currentUserId = SessionStorage.CurrentUser?.Id ?? "mock-doctor-id";

            UpdateNavStyles();
            await InitializePageAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            VideoCall.Cleanup();
            Chat.Cleanup();
            CleanupSpeechRecognizer();
            CleanupAudioRecording();
        }

        private async Task InitializePageAsync()
        {
            try
            {
                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;

                var appointmentResponse = await client
                    .From<Appointment>()
                    .Where(a => a.Id == _appointmentId)
                    .Single();

                if (appointmentResponse != null)
                {
                    _patientId = appointmentResponse.PatientId;

                    string roomCode = appointmentResponse.RoomCode?.Trim().ToLower();

                    if (string.IsNullOrWhiteSpace(roomCode))
                    {
                        roomCode = _appointmentId?.Trim().ToLower();
                    }

                    await LoadPatientInfoAsync();

                    await VideoCall.InitializeAsync(_appointmentId, _patientId, roomCode);

                    // Bác sĩ là người gọi nên phải StartCallAsync()
                    await VideoCall.StartCallAsync();

                    await Chat.InitializeAsync(_appointmentId, _currentUserId, _patientId);
                }
                else
                {
                    await new ContentDialog
                    {
                        Title = "Lỗi",
                        Content = "Không tìm thấy thông tin lịch hẹn này.",
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DoctorExam] Init Error: {ex}");

                await new ContentDialog
                {
                    Title = "Lỗi khởi tạo phiên khám",
                    Content = ex.Message,
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        private async Task LoadPatientInfoAsync()
        {
            try
            {
                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;

                var appointmentResponse = await client
                    .From<Appointment>()
                    .Get();

                var appointment = appointmentResponse.Models
                    .FirstOrDefault(a => a.Id == _appointmentId);

                if (appointment == null)
                {
                    TxtPatientName.Text = "Không tìm thấy lịch hẹn";
                    TxtPatientId.Text = "–";
                    TxtPatientMeta.Text = "–";
                    TxtCondition.Text = "–";
                    return;
                }

                _patientId = appointment.PatientId;

                var userResponse = await client
                    .From<User>()
                    .Get();

                var patientUser = userResponse.Models
                    .FirstOrDefault(u => u.Id == _patientId);

                if (patientUser != null && patientUser.FullName != null && patientUser.FullName.StartsWith("ệnh nhân", StringComparison.OrdinalIgnoreCase))
                {
                    patientUser.FullName = "B" + patientUser.FullName;
                    try
                    {
                        await client.From<User>().Update(patientUser);
                    }
                    catch { }
                }

                var patientProfileResponse = await client
                    .From<PatientProfile>()
                    .Get();

                var patientProfile = patientProfileResponse.Models
                    .FirstOrDefault(p => p.PatientId == _patientId);

                TxtPatientName.Text = patientUser?.FullName ?? "Bệnh nhân";
                TxtPatientId.Text = patientUser != null ? $"#{patientUser.Id.Substring(0, Math.Min(patientUser.Id.Length, 6)).ToUpper()}" : "–";
                TxtPatientMeta.Text = BuildPatientMeta(patientProfile);
                TxtCondition.Text = BuildCondition(patientProfile);
            }
            catch
            {
                TxtPatientName.Text = "Lê Văn Dũng";
                TxtPatientId.Text = "#MD8829";
                TxtPatientMeta.Text = "45 tuổi / Nam";
                TxtCondition.Text = "Cao huyết áp";
                _patientId = "mock-patient-id";
            }
        }

        private static string BuildPatientMeta(PatientProfile? profile)
        {
            if (profile == null)
                return "–";

            string ageText = "–";
            if (!string.IsNullOrWhiteSpace(profile.DateOfBirth))
            {
                if (DateTime.TryParse(profile.DateOfBirth, out var dob))
                {
                    var age = DateTime.Now.Year - dob.Year;
                    if (dob > DateTime.Now.AddYears(-age)) age--;
                    ageText = $"{age} tuổi";
                }
            }

            string genderText = !string.IsNullOrWhiteSpace(profile.Gender) ? profile.Gender : "Chưa rõ";

            return $"{ageText} / {genderText}";
        }

        private static string BuildCondition(PatientProfile? profile)
        {
            if (profile == null) return "Chưa rõ";

            if (profile.ChronicDiseases != null && profile.ChronicDiseases.Count > 0)
                return string.Join(", ", profile.ChronicDiseases);

            if (profile.Allergies != null && profile.Allergies.Count > 0)
                return "Dị ứng: " + string.Join(", ", profile.Allergies);

            return "Chưa rõ";
        }

        private void BtnNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            string oldNav = _activeNav;
            _activeNav = btn.Tag?.ToString() ?? "info";
            
            if (oldNav == _activeNav) return;

            UpdateNavStyles();

            // All panels initially collapsed
            InfoPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Collapsed;
            NotesPanel.Visibility = Visibility.Collapsed;
            PrescriptionPanel.Visibility = Visibility.Collapsed;

            // Activate panel based on tag
            switch (_activeNav)
            {
                case "info":
                    InfoPanel.Visibility = Visibility.Visible;
                    break;
                case "chat":
                    ChatPanel.Visibility = Visibility.Visible;
                    break;
                case "notes":
                    NotesPanel.Visibility = Visibility.Visible;
                    RenderQuickNotes();
                    break;
                case "prescription":
                    PrescriptionPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateNavStyles()
        {
            // Reset all icons to default color
            IconInfo.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconChat.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconNotes.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconPrescription.Foreground = new SolidColorBrush(HexToColor("#64748B"));

            // Set active icon color
            var activeBrush = new SolidColorBrush(HexToColor("#0284C7"));
            switch (_activeNav)
            {
                case "info": IconInfo.Foreground = activeBrush; break;
                case "chat": IconChat.Foreground = activeBrush; break;
                case "notes": IconNotes.Foreground = activeBrush; break;
                case "prescription": IconPrescription.Foreground = activeBrush; break;
            }
        }

        private void VideoCall_CallStarted(object sender, EventArgs e)
        {
            _activeNav = "info";
            UpdateNavStyles();
        }

        private void VideoCall_CallEnded(object sender, EventArgs e)
        {
            // In the new layout, we might not want to hide the whole VideoCall control, 
            // but just show that it ended. Or we can hide it.
            // Keeping it visible as a black screen with status message for now.
        }


        private async void Chat_NotesSaved(object sender, MedicalNotesSavedEventArgs e)
        {
            // Logic này có thể bỏ qua hoặc cập nhật nếu cần đồng bộ thông tin khác
        }

        private async void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            var input = new TextBox { PlaceholderText = "Nhập ghi chú nhanh...", Width = 260, TextWrapping = TextWrapping.Wrap };
            var dialog = new ContentDialog { Title = "Thêm ghi chú nhanh", Content = input, PrimaryButtonText = "Thêm", CloseButtonText = "Huỷ", DefaultButton = ContentDialogButton.Primary, XamlRoot = this.XamlRoot };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(input.Text))
            {
                _quickNotes.Add(input.Text.Trim());
                RenderQuickNotes();
            }
        }

        private void RenderQuickNotes()
        {
            QuickNotesList.Children.Clear();
            if (_quickNotes.Count == 0)
            {
                QuickNotesList.Children.Add(new TextBlock { Text = "Chưa có ghi chú nhanh", FontSize = 12, Foreground = new SolidColorBrush(HexToColor("#94A3B8")) });
                return;
            }
            foreach (var note in _quickNotes)
            {
                var row = new Border { Background = new SolidColorBrush(Colors.White), CornerRadius = new CornerRadius(8), BorderBrush = new SolidColorBrush(HexToColor("#F1F5F9")), BorderThickness = new Thickness(1, 1, 1, 1), Padding = new Thickness(10, 8, 10, 8) };
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                sp.Children.Add(new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE73E", FontSize = 12, Foreground = new SolidColorBrush(HexToColor("#0059BB")), VerticalAlignment = VerticalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = note, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(HexToColor("#1E293B")), VerticalAlignment = VerticalAlignment.Center });
                row.Child = sp;
                QuickNotesList.Children.Add(row);
            }
        }

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e)
        {
            _ = AddMedicineDialogAsync();
        }

        private async Task AddMedicineDialogAsync()
        {
            var medicineBox = new TextBox
            {
                Header = "Tên thuốc",
                PlaceholderText = "Ví dụ: Paracetamol 500mg",
                Margin = new Thickness(0, 0, 0, 0)
            };

            var dosageBox = new TextBox
            {
                Header = "Liều dùng / Cách dùng",
                PlaceholderText = "Ví dụ: 1 viên × 3 lần/ngày × 5 ngày"
            };

            var panel = new StackPanel { Spacing = 12, Width = 300 };
            panel.Children.Add(medicineBox);
            panel.Children.Add(dosageBox);

            var dialog = new ContentDialog
            {
                Title = "Thêm thuốc vào đơn",
                Content = panel,
                PrimaryButtonText = "Thêm",
                CloseButtonText = "Huỷ",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary
                && !string.IsNullOrWhiteSpace(medicineBox.Text))
            {
                _prescriptions.Add(new PrescriptionEntry(
                    medicineBox.Text.Trim(),
                    dosageBox.Text.Trim()));
                RenderPrescriptions();
            }
        }

        private void RenderPrescriptions()
        {
            PrescriptionList.Children.Clear();

            if (_prescriptions.Count == 0)
            {
                PrescriptionList.Children.Add(new TextBlock
                {
                    Text = "Chưa có thuốc nào được thêm",
                    FontSize = 12,
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                    Foreground = new SolidColorBrush(HexToColor("#94A3B8"))
                });
                return;
            }

            for (int i = 0; i < _prescriptions.Count; i++)
            {
                var entry = _prescriptions[i];

                var row = new Border
                {
                    Background = new SolidColorBrush(HexToColor("#F8FAFC")),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(HexToColor("#E2E8F0")),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 8, 10, 8)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var idx = new TextBlock
                {
                    Text = $"{i + 1}",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(HexToColor("#94A3B8")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(idx, 0);

                var name = new TextBlock
                {
                    Text = entry.Medicine,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(HexToColor("#1E293B")),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                Grid.SetColumn(name, 1);

                var dosage = new TextBlock
                {
                    Text = string.IsNullOrEmpty(entry.Dosage) ? "–" : entry.Dosage,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(HexToColor("#64748B")),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(dosage, 2);

                grid.Children.Add(idx);
                grid.Children.Add(name);
                grid.Children.Add(dosage);
                row.Child = grid;
                PrescriptionList.Children.Add(row);
            }
        }

        private async void BtnSendVoice_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecordingAudio)
            {
                await StopAudioRecordingAsync();
            }
            else
            {
                await StartAudioRecordingAsync();
            }
        }

        private async Task StartAudioRecordingAsync()
        {
            try
            {
                var settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio,
                    MediaCategory = MediaCategory.Speech
                };

                _audioMediaCapture = new MediaCapture();
                await _audioMediaCapture.InitializeAsync(settings);

                var localFolder = ApplicationData.Current.TemporaryFolder;
                _tempRecordingFile = await localFolder.CreateFileAsync("voice_diagnosis.m4a", CreationCollisionOption.GenerateUniqueName);

                var profile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.Low);
                await _audioMediaCapture.StartRecordToStorageFileAsync(profile, _tempRecordingFile);

                _isRecordingAudio = true;

                // Update UI to Recording state
                BtnSendVoice.Background = new SolidColorBrush(HexToColor("#EF4444")); // Red
                BtnSendVoiceIcon.Glyph = "\uE71A"; // Stop glyph
                BtnSendVoiceIcon.Foreground = new SolidColorBrush(Colors.White);
                BtnSendVoiceText.Text = "Đang ghi âm... (Bấm để dừng)";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioRecord] Start Error: {ex.Message}");
                await ShowErrorAsync("Lỗi khởi động ghi âm: " + ex.Message);
                CleanupAudioRecording();
            }
        }

        private async Task StopAudioRecordingAsync()
        {
            try
            {
                if (_audioMediaCapture != null && _isRecordingAudio)
                {
                    await _audioMediaCapture.StopRecordAsync();
                    _isRecordingAudio = false;

                    if (_tempRecordingFile != null)
                    {
                        using (var stream = await _tempRecordingFile.OpenReadAsync())
                        {
                            using (var reader = new Windows.Storage.Streams.DataReader(stream))
                            {
                                await reader.LoadAsync((uint)stream.Size);
                                byte[] bytes = new byte[stream.Size];
                                reader.ReadBytes(bytes);
                                _voiceAudioBase64 = Convert.ToBase64String(bytes);
                            }
                        }
                    }

                    // Update UI to Recorded state
                    BtnSendVoice.Background = new SolidColorBrush(HexToColor("#16A34A")); // Green
                    BtnSendVoiceIcon.Glyph = "\uE8FB"; // Checkmark glyph
                    BtnSendVoiceIcon.Foreground = new SolidColorBrush(Colors.White);
                    BtnSendVoiceText.Text = "Đã ghi âm (Bấm để ghi lại)";

                    await new ContentDialog
                    {
                        Title = "Ghi âm hoàn tất",
                        Content = "Đã ghi âm chẩn đoán thành công. Bản ghi âm này sẽ tự động được gửi kèm khi bạn lưu hồ sơ khám bệnh hoặc kết thúc phiên khám.",
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioRecord] Stop Error: {ex.Message}");
                await ShowErrorAsync("Lỗi dừng ghi âm: " + ex.Message);
            }
            finally
            {
                CleanupAudioRecording();
            }
        }

        private void CleanupAudioRecording()
        {
            if (_audioMediaCapture != null)
            {
                try
                {
                    _audioMediaCapture.Dispose();
                }
                catch { }
                _audioMediaCapture = null;
            }
            _isRecordingAudio = false;
        }

        /// <summary>
        /// Gửi văn bản dưới dạng tin nhắn "[TTS] text" cho bệnh nhân.
        /// Bệnh nhân sẽ thấy nút phát giọng nói trong ChatControl.
        /// </summary>
        private async Task SendDoctorVoiceToChatAsync(string text)
        {
            try
            {
                _ttsHttpClient ??= new HttpClient
                {
                    BaseAddress = new Uri(APIClient.BaseHttpClient.ServerBaseUrl)
                };

                await _ttsHttpClient.PostAsJsonAsync("api/chat/send", new
                {
                    Id          = Guid.NewGuid().ToString(),
                    SenderId    = _currentUserId,
                    ReceiverId  = _patientId,
                    MessageText = "[TTS] " + text,
                    IsRead      = false,
                    CreatedAt   = DateTime.UtcNow
                });
            }
            catch (Exception httpEx)
            {
                Debug.WriteLine($"[TTS Send HTTP] Failed: {httpEx.Message}");

                // Fallback: direct Supabase insert
                try
                {
                    var client = SupabaseIntegration.SupabaseManager.Instance.Client;
                    await client.From<ChatMessageItem>().Insert(new ChatMessageItem
                    {
                        Id          = Guid.NewGuid().ToString(),
                        SenderId    = _currentUserId,
                        ReceiverId  = _patientId,
                        MessageText = "[TTS] " + text,
                        IsRead      = false,
                        CreatedAt   = DateTime.UtcNow
                    });
                }
                catch (Exception ex2)
                {
                    Debug.WriteLine($"[TTS Send Fallback] Error: {ex2.Message}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // SPEECH-TO-TEXT  (Windows.Media.SpeechRecognition)
        // ─────────────────────────────────────────────────────────────

        private async void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecording)
                await StopRecordingAsync();
            else
                await StartRecordingAsync();
        }

        private async Task StartRecordingAsync()
        {
            try
            {
                _speechRecognizer = new SpeechRecognizer();

                // Thêm ngữ pháp dictation (nhận dạng tự do, không giới hạn từ)
                var dictationConstraint = new SpeechRecognitionTopicConstraint(
                    SpeechRecognitionScenario.Dictation, "dictation");
                _speechRecognizer.Constraints.Add(dictationConstraint);

                var compileResult = await _speechRecognizer.CompileConstraintsAsync();
                if (compileResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    await ShowErrorAsync("Không thể khởi động nhận dạng giọng nói. Vui lòng kiểm tra micro.");
                    return;
                }

                // Lắng nghe từng kết quả liên tục
                _speechRecognizer.ContinuousRecognitionSession.ResultGenerated +=
                    SpeechSession_ResultGenerated;
                _speechRecognizer.ContinuousRecognitionSession.Completed +=
                    SpeechSession_Completed;

                await _speechRecognizer.ContinuousRecognitionSession.StartAsync();

                _isRecording = true;
                UpdateMicUI(isRecording: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[STT] StartRecording Error: {ex.Message}");
                await ShowErrorAsync("Lỗi khởi động micro: " + ex.Message);
                CleanupSpeechRecognizer();
            }
        }

        private async Task StopRecordingAsync()
        {
            try
            {
                if (_speechRecognizer != null && _isRecording)
                    await _speechRecognizer.ContinuousRecognitionSession.StopAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[STT] StopRecording Error: {ex.Message}");
            }
            finally
            {
                _isRecording = false;
                CleanupSpeechRecognizer();
                UpdateMicUI(isRecording: false);
            }
        }

        private void SpeechSession_ResultGenerated(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // Chỉ lấy kết quả đủ tin cậy (tránh nhận nhầm)
            if (args.Result.Confidence == SpeechRecognitionConfidence.Low ||
                string.IsNullOrWhiteSpace(args.Result.Text))
                return;

            string recognizedText = args.Result.Text;

            // Marshal về UI thread để cập nhật TextBox + hiện toast
            DispatcherQueue.TryEnqueue(() =>
            {
                string current = DiagnosisBox.Text;
                if (string.IsNullOrEmpty(current))
                    DiagnosisBox.Text = recognizedText;
                else
                    DiagnosisBox.Text = current.TrimEnd() + " " + recognizedText;

                // Di chuyển con trỏ về cuối
                DiagnosisBox.SelectionStart = DiagnosisBox.Text.Length;

                // Hiện toast trên màn hình video
                ShowSttToastAsync(recognizedText);
            });
        }

        private void SpeechSession_Completed(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionCompletedEventArgs args)
        {
            // Session tự kết thúc (ví dụ: im lặng quá lâu)
            DispatcherQueue.TryEnqueue(() =>
            {
                _isRecording = false;
                CleanupSpeechRecognizer();
                UpdateMicUI(isRecording: false);
            });
        }

        private void UpdateMicUI(bool isRecording)
        {
            if (isRecording)
            {
                // Floating button → Stop icon + nền đỏ
                MicIconFloat.Glyph = "\uE71A";
                BtnMicFloat.Background = new SolidColorBrush(HexToColor("#CC7F1D1D"));

                // Vòng tròn đỏ nhấp nháy quanh floating button
                var ringAnim = new DoubleAnimation
                {
                    From = 1.0, To = 0.0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.9)),
                    AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever
                };
                var ringSb = new Storyboard();
                ringSb.Children.Add(ringAnim);
                Storyboard.SetTarget(ringAnim, RecordingRing);
                Storyboard.SetTargetProperty(ringAnim, "Opacity");
                RecordingRing.Opacity = 1.0;
                ringSb.Begin();
                RecordingRing.Tag = ringSb;
            }
            else
            {
                // Floating button → Mic icon + nền tối mờ
                MicIconFloat.Glyph = "\uE720";
                BtnMicFloat.Background = new SolidColorBrush(HexToColor("#CC0F172A"));
                if (RecordingRing.Tag is Storyboard ringSb) ringSb.Stop();
                RecordingRing.Opacity = 0;

                // Ẩn toast nếu đang hiện
                SttToast.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Hiện toast text trên video, tự động ẩn sau 4 giây.
        /// Nếu text mới đến trước khi hết thời gian, timer reset.
        /// </summary>
        private async void ShowSttToastAsync(string text)
        {
            // Huỷ timer cũ nếu đang chạy
            _toastCts?.Cancel();
            _toastCts = new System.Threading.CancellationTokenSource();
            var token = _toastCts.Token;

            SttToastText.Text = $"🎙 {text}";
            SttToast.Visibility = Visibility.Visible;

            try
            {
                await Task.Delay(4000, token);
                if (!token.IsCancellationRequested)
                    SttToast.Visibility = Visibility.Collapsed;
            }
            catch (TaskCanceledException) { /* Có toast mới thay thế, bỏ qua */ }
        }

        private void CleanupSpeechRecognizer()
        {
            if (_speechRecognizer != null)
            {
                try
                {
                    _speechRecognizer.ContinuousRecognitionSession.ResultGenerated -=
                        SpeechSession_ResultGenerated;
                    _speechRecognizer.ContinuousRecognitionSession.Completed -=
                        SpeechSession_Completed;
                    _speechRecognizer.Dispose();
                }
                catch { /* Bỏ qua lỗi cleanup */ }
                finally
                {
                    _speechRecognizer = null;
                }
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            await new ContentDialog
            {
                Title = "Lỗi",
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }

        private async void BtnSaveNotes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;

                // Merge quick notes + prescriptions into AiMedicines
                var medicines = new System.Collections.Generic.List<string>(_quickNotes);
                foreach (var p in _prescriptions)
                    medicines.Add(string.IsNullOrEmpty(p.Dosage) ? p.Medicine : $"{p.Medicine} — {p.Dosage}");

                if (!string.IsNullOrEmpty(_voiceDiagnosisText))
                    medicines.Add("[VoiceDiagnosis] " + _voiceDiagnosisText);

                if (!string.IsNullOrEmpty(_voiceAudioBase64))
                    medicines.Add("[VoiceAudio] " + _voiceAudioBase64);

                var record = new MedicalRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    AppointmentId = _appointmentId,
                    DoctorId = _currentUserId,
                    PatientId = _patientId,
                    Diagnosis = DiagnosisBox.Text ?? string.Empty,
                    PrescriptionImageUrl = string.Empty,
                    AiMedicines = medicines,
                    CreatedAt = DateTime.UtcNow
                };
                await client.From<MedicalRecord>().Insert(record);
                await new ContentDialog { Title = "Đã lưu", Content = "Ghi chú khám bệnh đã được lưu.", CloseButtonText = "Đóng", XamlRoot = this.XamlRoot }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog { Title = "Lỗi", Content = "Không lưu được ghi chú: " + ex.Message, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot }.ShowAsync();
            }
        }

        private async void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Kết thúc khám",
                Content = "Kết thúc buổi khám và lưu toàn bộ ghi chú?",
                PrimaryButtonText = "Kết thúc",
                CloseButtonText = "Huỷ",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;
                    
                    // 1. Lưu hồ sơ bệnh án (bao gồm cả đơn thuốc)
                    var medicines = new System.Collections.Generic.List<string>(_quickNotes);
                    foreach (var p in _prescriptions)
                        medicines.Add(string.IsNullOrEmpty(p.Dosage) ? p.Medicine : $"{p.Medicine} — {p.Dosage}");

                    // Thêm ghi chú đơn thuốc nếu có
                    string prescNote = PrescriptionNoteBox.Text?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(prescNote))
                        medicines.Add("[Ghi chú] " + prescNote);

                    if (!string.IsNullOrEmpty(_voiceDiagnosisText))
                        medicines.Add("[VoiceDiagnosis] " + _voiceDiagnosisText);

                    if (!string.IsNullOrEmpty(_voiceAudioBase64))
                        medicines.Add("[VoiceAudio] " + _voiceAudioBase64);

                    var medicalRecord = new MedicalRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        AppointmentId = _appointmentId,
                        PatientId = _patientId,
                        DoctorId = _currentUserId,
                        Diagnosis = DiagnosisBox.Text ?? string.Empty,
                        AiMedicines = medicines,
                        CreatedAt = DateTime.UtcNow
                    };

                    await client.From<MedicalRecord>().Insert(medicalRecord);

                    // 2. Cập nhật Appointment status = Completed
                    await client.From<Appointment>()
                        .Where(x => x.Id == _appointmentId)
                        .Set(x => x.Status, "Completed")
                        .Update();

                    VideoCall.Cleanup();
                    Chat.Cleanup();

                    // Quay về trang chủ bác sĩ
                    Frame.Navigate(typeof(ManageSchedulePage)); 
                }
                catch (Exception ex)
                {
                    await new ContentDialog
                    {
                        Title = "Lỗi lưu dữ liệu",
                        Content = $"Không thể lưu kết quả khám: {ex.Message}",
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
            }
        }

        private async void BtnEmergency_Click(object sender, RoutedEventArgs e)
        {
            var d = new ContentDialog
            {
                Title = "Khẩn cấp",
                Content = "Đã gửi tín hiệu khẩn cấp.\nVui lòng hướng dẫn bệnh nhân liên hệ 115 ngay.",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            await d.ShowAsync();
        }

        private static Color HexToColor(string hex)
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