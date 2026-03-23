using Healthcare.Client.UI.Shared;
using System;
using System.Windows.Forms;

namespace Healthcare.Client.UI.Controls.Shared
{
    public partial class ChatControlUC : UserControl
    {
        private string _currentUserId;
        private string _targetUserId;

        public ChatControlUC()
        {
            InitializeComponent();
        }

        public void StartChatSession(string currentUserId, string targetUserId)
        {
            _currentUserId = currentUserId;
            _targetUserId = targetUserId;
            lblChatTitle.Text = $"Đang trò chuyện với: {targetUserId}";
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            string msg = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            lstMessages.Items.Add($"[{DateTime.Now:HH:mm}] Tôi: {msg}");
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
            txtInput.Clear();
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                BtnSend_Click(this, EventArgs.Empty);
            }
        }

        private void BtnVideoCall_Click(object sender, EventArgs e)
        {
            VideoCallForm videoForm = new VideoCallForm(_currentUserId, _targetUserId ?? "Khách");
            videoForm.Show();
        }
    }
}