using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class ChatbotAIControl : UserControl
    {
        private readonly HttpClient _httpClient = new HttpClient
        {
            // Sửa port này theo Healthcare.Server/Properties/launchSettings.json nếu khác
            BaseAddress = new Uri("https://localhost:5001/")
        };

        public ChatbotAIControl()
        {
            this.InitializeComponent();

            AppendBotMessage("Xin chào! Tôi là ChatGPT hỗ trợ tư vấn sức khỏe cơ bản. Bạn cần hỏi gì?");
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void ChatInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            string message = ChatInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
                return;

            ChatInput.Text = string.Empty;
            BtnSend.IsEnabled = false;

            AppendUserMessage(message);

            try
            {
                AppendBotMessage("Đang suy nghĩ...");

                string reply = await CallChatGptApiAsync(message);

                RemoveLastMessage();
                AppendBotMessage(reply);
            }
            catch (Exception ex)
            {
                RemoveLastMessage();
                AppendBotMessage("Không kết nối được ChatGPT: " + ex.Message);
            }
            finally
            {
                BtnSend.IsEnabled = true;
            }
        }

        private async Task<string> CallChatGptApiAsync(string message)
        {
            var requestBody = new
            {
                message = message
            };

            string json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            using var response = await _httpClient.PostAsync("api/ai/chat", content);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return "Server AI lỗi: " + responseJson;
            }

            var result = JsonSerializer.Deserialize<ChatGptResponse>(
                responseJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (result == null || string.IsNullOrWhiteSpace(result.Reply))
            {
                return "ChatGPT chưa có câu trả lời phù hợp.";
            }

            return result.Reply;
        }

        private void AppendUserMessage(string text)
        {
            MessageList.Children.Add(BuildBubble(
                senderName: "Bạn",
                message: text,
                isUser: true
            ));

            ScrollToBottom();
        }

        private void AppendBotMessage(string text)
        {
            MessageList.Children.Add(BuildBubble(
                senderName: "ChatGPT",
                message: text,
                isUser: false
            ));

            ScrollToBottom();
        }

        private void RemoveLastMessage()
        {
            if (MessageList.Children.Count > 0)
            {
                MessageList.Children.RemoveAt(MessageList.Children.Count - 1);
            }
        }

        private UIElement BuildBubble(string senderName, string message, bool isUser)
        {
            var wrapper = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 320,
                Margin = new Thickness(0, 4, 0, 4)
            };

            wrapper.Children.Add(new TextBlock
            {
                Text = senderName,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(HexToColor("#64748B")),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
            });

            wrapper.Children.Add(new Border
            {
                Background = new SolidColorBrush(
                    isUser ? HexToColor("#0059BB") : HexToColor("#E7E8E9")
                ),
                CornerRadius = new CornerRadius(
                    isUser ? 12 : 2,
                    isUser ? 2 : 12,
                    12,
                    12
                ),
                Padding = new Thickness(12, 8, 12, 8),
                Child = new TextBlock
                {
                    Text = message,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(
                        isUser ? Colors.White : HexToColor("#1E293B")
                    )
                }
            });

            return wrapper;
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.UpdateLayout();
            ChatScrollViewer.ChangeView(null, ChatScrollViewer.ScrollableHeight, null);
        }

        private static Color HexToColor(string hex)
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

    public class ChatGptResponse
    {
        public bool Success { get; set; }

        public string Reply { get; set; } = string.Empty;
    }
}