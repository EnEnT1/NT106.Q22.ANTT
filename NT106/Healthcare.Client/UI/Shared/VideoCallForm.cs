using System;
using System.Drawing;
using System.Windows.Forms;

namespace Healthcare.Client.UI.Controls.Shared
{
    public partial class VideoCallForm : Form
    {
        private bool _isMicOn = true;
        private bool _isCamOn = true;

        public VideoCallForm(string currentUserId, string targetUserId)
        {
            InitializeComponent();
            this.Text = $"Cuộc gọi bảo mật với: {targetUserId}";

            // Thiết lập ép UI để Camera nhỏ nổi lên trên Camera nền
            picLocalCamera.Parent = picRemoteCamera;
            lblStatus.Parent = picRemoteCamera;

            // Căn chỉnh lại tọa độ khi Parent thay đổi
            picLocalCamera.Location = new Point(picRemoteCamera.Width - picLocalCamera.Width - 30,
                                                picRemoteCamera.Height - picLocalCamera.Height - 30);
        }

        private void BtnMic_Click(object sender, EventArgs e)
        {
            _isMicOn = !_isMicOn;
            btnMic.Text = _isMicOn ? "🎙️ Tắt Mic" : "🎙️ Bật Mic";
            btnMic.BackColor = _isMicOn ? Color.White : Color.LightGray;
        }

        private void BtnCam_Click(object sender, EventArgs e)
        {
            _isCamOn = !_isCamOn;
            btnCam.Text = _isCamOn ? "📷 Tắt Cam" : "📷 Bật Cam";
            btnCam.BackColor = _isCamOn ? Color.White : Color.LightGray;
        }

        private void BtnEndCall_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // TODO: Dừng Camera và ngắt kết nối mạng tại đây
            base.OnFormClosing(e);
        }
    }
}