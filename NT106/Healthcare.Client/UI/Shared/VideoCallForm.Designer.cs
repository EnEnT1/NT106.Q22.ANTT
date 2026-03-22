using System.Drawing;
using System.Windows.Forms;

namespace Healthcare.Client.UI.Controls.Shared
{
    partial class VideoCallForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.picRemoteCamera = new System.Windows.Forms.PictureBox();
            this.picLocalCamera = new System.Windows.Forms.PictureBox();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.btnMic = new System.Windows.Forms.Button();
            this.btnCam = new System.Windows.Forms.Button();
            this.btnEndCall = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.picRemoteCamera)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLocalCamera)).BeginInit();
            this.pnlControls.SuspendLayout();
            this.SuspendLayout();

            // 
            // picLocalCamera (Camera nhỏ của mình)
            // 
            this.picLocalCamera.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.picLocalCamera.BackColor = System.Drawing.Color.Black;
            this.picLocalCamera.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picLocalCamera.Location = new System.Drawing.Point(784, 340); // Đã căn lại cho màn 1024
            this.picLocalCamera.Name = "picLocalCamera";
            this.picLocalCamera.Size = new System.Drawing.Size(220, 150);
            this.picLocalCamera.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLocalCamera.TabIndex = 1;
            this.picLocalCamera.TabStop = false;
            // 
            // lblStatus 
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.MediumSpringGreen;
            this.lblStatus.Location = new System.Drawing.Point(20, 20);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(437, 28);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Đang thiết lập kênh truyền WebRTC bảo mật...";
            // 
            // pnlControls 
            // 
            this.pnlControls.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.pnlControls.Controls.Add(this.btnMic);
            this.pnlControls.Controls.Add(this.btnCam);
            this.pnlControls.Controls.Add(this.btnEndCall);
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlControls.Location = new System.Drawing.Point(0, 510);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(1024, 90); // Rộng 1024
            this.pnlControls.TabIndex = 2;
            // 
            // btnMic (Tự động căn giữa màn 1024)
            // 
            this.btnMic.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnMic.BackColor = System.Drawing.Color.White;
            this.btnMic.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMic.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMic.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnMic.Location = new System.Drawing.Point(312, 20);
            this.btnMic.Name = "btnMic";
            this.btnMic.Size = new System.Drawing.Size(120, 50);
            this.btnMic.TabIndex = 0;
            this.btnMic.Text = "🎙️ Tắt Mic";
            this.btnMic.UseVisualStyleBackColor = false;
            this.btnMic.Click += new System.EventHandler(this.BtnMic_Click);
            // 
            // btnCam
            // 
            this.btnCam.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCam.BackColor = System.Drawing.Color.White;
            this.btnCam.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCam.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCam.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnCam.Location = new System.Drawing.Point(452, 20);
            this.btnCam.Name = "btnCam";
            this.btnCam.Size = new System.Drawing.Size(120, 50);
            this.btnCam.TabIndex = 1;
            this.btnCam.Text = "📷 Tắt Cam";
            this.btnCam.UseVisualStyleBackColor = false;
            this.btnCam.Click += new System.EventHandler(this.BtnCam_Click);
            // 
            // btnEndCall
            // 
            this.btnEndCall.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnEndCall.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.btnEndCall.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEndCall.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEndCall.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnEndCall.ForeColor = System.Drawing.Color.White;
            this.btnEndCall.Location = new System.Drawing.Point(592, 20);
            this.btnEndCall.Name = "btnEndCall";
            this.btnEndCall.Size = new System.Drawing.Size(120, 50);
            this.btnEndCall.TabIndex = 2;
            this.btnEndCall.Text = "📞 Kết thúc";
            this.btnEndCall.UseVisualStyleBackColor = false;
            this.btnEndCall.Click += new System.EventHandler(this.BtnEndCall_Click);
            // 
            // picRemoteCamera (Full nền màn hình)
            // 
            this.picRemoteCamera.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(35)))), ((int)(((byte)(35)))));
            this.picRemoteCamera.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picRemoteCamera.Location = new System.Drawing.Point(0, 0);
            this.picRemoteCamera.Name = "picRemoteCamera";
            this.picRemoteCamera.Size = new System.Drawing.Size(1024, 510);
            this.picRemoteCamera.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picRemoteCamera.TabIndex = 0;
            this.picRemoteCamera.TabStop = false;
            // 
            // VideoCallForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(15)))));
            this.ClientSize = new System.Drawing.Size(1024, 600); 
            this.Controls.Add(this.picLocalCamera);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.picRemoteCamera);
            this.Controls.Add(this.pnlControls);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "VideoCallForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Phòng Khám Trực Tuyến - Video Call";

            ((System.ComponentModel.ISupportInitialize)(this.picRemoteCamera)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLocalCamera)).EndInit();
            this.pnlControls.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.PictureBox picRemoteCamera, picLocalCamera;
        private System.Windows.Forms.Panel pnlControls;
        private System.Windows.Forms.Button btnMic, btnCam, btnEndCall;
        private System.Windows.Forms.Label lblStatus;
    }
}