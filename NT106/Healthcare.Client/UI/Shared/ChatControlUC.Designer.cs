using System.Drawing;
using System.Windows.Forms;

namespace Healthcare.Client.UI.Controls.Shared
{
    partial class ChatControlUC
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlTop = new Panel();
            lblChatTitle = new Label();
            btnVideoCall = new Button();
            pnlBottom = new Panel();
            txtInput = new TextBox();
            btnSend = new Button();
            lstMessages = new ListBox();
            pnlTop.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTop
            // 
            pnlTop.BackColor = Color.White;
            pnlTop.Controls.Add(lblChatTitle);
            pnlTop.Controls.Add(btnVideoCall);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Location = new Point(0, 0);
            pnlTop.Name = "pnlTop";
            pnlTop.Padding = new Padding(20, 15, 20, 15);
            pnlTop.Size = new Size(1024, 80);
            pnlTop.TabIndex = 0;
            // 
            // lblChatTitle
            // 
            lblChatTitle.Dock = DockStyle.Left;
            lblChatTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblChatTitle.ForeColor = Color.FromArgb(41, 128, 185);
            lblChatTitle.Location = new Point(20, 15);
            lblChatTitle.Name = "lblChatTitle";
            lblChatTitle.Size = new Size(500, 50);
            lblChatTitle.TabIndex = 1;
            lblChatTitle.Text = "Tư vấn Y tế Trực tuyến";
            lblChatTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnVideoCall
            // 
            btnVideoCall.BackColor = Color.FromArgb(0, 120, 215);
            btnVideoCall.Cursor = Cursors.Hand;
            btnVideoCall.Dock = DockStyle.Right;
            btnVideoCall.FlatAppearance.BorderSize = 0;
            btnVideoCall.FlatStyle = FlatStyle.Flat;
            btnVideoCall.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnVideoCall.ForeColor = Color.White;
            btnVideoCall.Location = new Point(804, 15);
            btnVideoCall.Name = "btnVideoCall";
            btnVideoCall.Size = new Size(200, 50);
            btnVideoCall.TabIndex = 0;
            btnVideoCall.Text = "🎥 GỌI VIDEO";
            btnVideoCall.UseVisualStyleBackColor = false;
            btnVideoCall.Click += BtnVideoCall_Click;
            // 
            // pnlBottom
            // 
            pnlBottom.BackColor = Color.White;
            pnlBottom.Controls.Add(txtInput);
            pnlBottom.Controls.Add(btnSend);
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Location = new Point(0, 500);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Padding = new Padding(20, 15, 20, 15);
            pnlBottom.Size = new Size(1024, 100);
            pnlBottom.TabIndex = 1;
            // 
            // txtInput
            // 
            txtInput.BorderStyle = BorderStyle.FixedSingle;
            txtInput.Dock = DockStyle.Fill;
            txtInput.Font = new Font("Segoe UI", 12F);
            txtInput.Location = new Point(20, 15);
            txtInput.Margin = new Padding(0, 0, 15, 0);
            txtInput.Multiline = true;
            txtInput.Name = "txtInput";
            txtInput.PlaceholderText = " Nhập nội dung tin nhắn của bạn vào đây...";
            txtInput.Size = new Size(834, 70);
            txtInput.TabIndex = 0;
            txtInput.KeyDown += TxtInput_KeyDown;
            // 
            // btnSend
            // 
            btnSend.BackColor = Color.FromArgb(40, 167, 69);
            btnSend.Cursor = Cursors.Hand;
            btnSend.Dock = DockStyle.Right;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnSend.ForeColor = Color.White;
            btnSend.Location = new Point(854, 15);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(150, 70);
            btnSend.TabIndex = 1;
            btnSend.Text = "GỬI";
            btnSend.UseVisualStyleBackColor = false;
            btnSend.Click += BtnSend_Click;
            // 
            // lstMessages
            // 
            lstMessages.BorderStyle = BorderStyle.None;
            lstMessages.Dock = DockStyle.Fill;
            lstMessages.Font = new Font("Segoe UI", 12F);
            lstMessages.FormattingEnabled = true;
            lstMessages.IntegralHeight = false;
            lstMessages.Location = new Point(0, 80);
            lstMessages.Name = "lstMessages";
            lstMessages.Size = new Size(1024, 420);
            lstMessages.TabIndex = 2;
            // 
            // ChatControlUC
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 247, 250);
            Controls.Add(lstMessages);
            Controls.Add(pnlTop);
            Controls.Add(pnlBottom);
            Name = "ChatControlUC";
            Size = new Size(1024, 600);
            pnlTop.ResumeLayout(false);
            pnlBottom.ResumeLayout(false);
            pnlBottom.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlTop, pnlBottom;
        private System.Windows.Forms.Button btnVideoCall, btnSend;
        private System.Windows.Forms.TextBox txtInput;
        private System.Windows.Forms.ListBox lstMessages;
        private System.Windows.Forms.Label lblChatTitle;
    }
}