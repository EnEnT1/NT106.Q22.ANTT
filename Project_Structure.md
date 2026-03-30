```

NT106.Solution (Thư mục gốc Solution)
│
├── Healthcare.Server (Project 1: ASP.NET Core Web API)
│   │
│   ├── Controllers                      (Nơi nhận các HTTP Request từ WinForms hoặc VNPay)
│   │   ├── AiController.cs              (Nhận ảnh đơn thuốc, trả về kết quả AI)
│   │   └── PaymentController.cs         (Nhận Webhook từ VNPay/MoMo khi thanh toán xong)
│   │
│   ├── Models                        (Class ánh xạ từ database)
│   │   ├── Identity
│   │   │   ├── User.cs
│   │   │   ├── PatientProfile.cs
│   │   │   └── DoctorProfile.cs
│   │   ├── Core
│   │   │   ├── Appointment.cs
│   │   │   ├── MedicalRecord.cs
│   │   │   ├── Transaction.cs
│   │   │   ├── HealthMetric.cs
│   │   │   ├── LabResult.cs
│   │   │   ├── MasterMedicine.cs
│   │   │   └── MedicationReminder.cs
│   │   ├── Communication
│   │   │   ├── ChatMessage.cs
│   │   │   ├── WebrtcSignal.cs
│   │   │   ├── Notification.cs
│   │   │   └── Review.cs
│   │   └── Security
│   │       └── AuditLog.cs
│   │
│   ├── Services                         (Logic nghiệp vụ ẩn phía sau Server)
│   │   ├── AiPrescriptionService.cs     (Code gọi API trí tuệ nhân tạo OCR)
│   │   ├── PaymentService.cs            (Kiểm tra chữ ký bảo mật giao dịch)
│   │   └── ScheduledWorker.cs           (Tiến trình chạy ngầm quét giờ uống thuốc để báo thức)
│   │
│   ├── SupabaseIntegration              
│   │   └── SupabaseAdminHelper.cs       (Xác thực Token JWT xem ai đang gọi API)
│   │
│   ├── Properties
│   │   └── launchSettings.json          (Cấu hình cổng mạng Port chạy Server, vd: localhost:5000)
│   ├── appsettings.json                 (Chứa Secret Keys: VNPay Key, AI Key, Supabase Service Role)
│   ├── Program.cs                       (File khởi chạy Server, cài đặt Dependency Injection)
│   └── Healthcare.Server.csproj         
│
└── Healthcare.Client (Project 2: Win UI 3)
    │
    ├── APIClient                        (Nối Server C# nội bộ)
    │   ├── AiClient.cs                  (Gói ảnh gửi lên Server phân tích)
    │   ├── PaymentClient.cs             (Gửi request lấy URL thanh toán VNPay)
    │   └── BaseHttpClient.cs            (Cấu hình HttpClient chung, tự động nhét Token)
    │
    ├── SupabaseIntegration              (Nối thẳng lên Cloud Database)
    │   ├── SupabaseManager.cs           (Singleton khởi tạo kết nối mạng)
    │   ├── SupabaseAuthService.cs       (Hàm Sign In, Sign Up)
    │   ├── SupabaseDbService.cs         (Hàm CRUD cơ bản: Add, Get, Update, Delete)
    │   ├── SupabaseStorageService.cs    (Hàm Upload file ảnh avatar, kết quả X-Quang)
    │   └── SupabaseRealtimeService.cs   (WebSockets lắng nghe tin nhắn Chat & Tín hiệu gọi Video)
    │
    ├── Models                           
    │   ├── Identity                     (User.cs, PatientProfile.cs, DoctorProfile.cs)
    │   ├── Core                         (Appointment.cs, MedicalRecord.cs, ...)
    │   ├── Communication                (ChatMessage.cs, WebrtcSignal.cs, ...)
    │   └── Security                     (AuditLog.cs)
    │
    ├── Cryptography                     (Bảo mật cục bộ máy Client)
    │   ├── AESManager.cs                (Mã hóa tin nhắn trước khi đẩy lên Supabase)
    │   └── RSAManager.cs                (Tạo khóa Public/Private Key)
    │
    ├── Helpers                          (Các class tiện ích dùng chung)
    │   └── SessionStorage.cs            (Lưu thông tin User đang đăng nhập hiện tại)
    │
    ├── Media                            (Xử lý âm thanh, hình ảnh cục bộ)
    │   └── WebRtcPeerConnection.cs      (Thuật toán bắt Camera & Micro cho Video Call P2P)
    │
    ├── UI                               (Thư mục Giao diện WinForms)
    │   ├── Auth                             (Phân hệ Đăng nhập/Xác thực)
    │   │   ├── LoginForm.cs                     (Màn hình Login / Register gốc)
    │   │   └── ForgotPasswordForm.cs        (Màn hình nhập Email gửi link reset pass)
    │   │
    │   ├── Controls.Patient                 (Phân hệ dành riêng cho BỆNH NHÂN)
    │   │   ├── PatientHomeUC.cs             (Trang chủ: Nhắc nhở uống thuốc, Lịch khám sắp tới)
    │   │   ├── BookAppointmentUC.cs         (Tìm kiếm Bác sĩ, Chọn giờ rảnh & Đặt lịch)
    │   │   ├── PaymentCheckoutUC.cs         (Quét mã QR VNPay/MoMo để thanh toán trước khi khám)
    │   │   ├── MyRecordsUC.cs               (Xem lại Bệnh án cũ, xem đơn thuốc AI đọc)
    │   │   ├── HealthMetricsUC.cs           (Giao diện tự nhập số đo: Huyết áp, Nhịp tim, Đường huyết)
    │   │   └── LabResultsUC.cs              (Kho lưu trữ ảnh X-Quang, file PDF xét nghiệm)
    │   │
    │   ├── Controls.Doctor                  (Phân hệ dành riêng cho BÁC SĨ)
    │   │   ├── DoctorHomeUC.cs              (Trang chủ: Danh sách hàng chờ bệnh nhân hôm nay)
    │   │   ├── ManageScheduleUC.cs          (Đăng ký khung giờ rảnh để bệnh nhân biết mà đặt lịch)
    │   │   ├── ExaminationUC.cs             (Màn hình Khám Bệnh: Vừa gọi Video, vừa gõ Bệnh án bên cạnh)
    │   │   ├── PatientHistoryUC.cs          (Xem tiền sử dị ứng, bệnh nền của bệnh nhân trước khi kê đơn)
    │   │   └── RevenueUC.cs                 (Biểu đồ thống kê tiền khám bệnh kiếm được trong tháng)
    │   │
    │   ├── Controls.Shared                  (Phân hệ dùng chung cho CẢ 2 ĐỐI TƯỢNG)
    │   │   │   ├── ChatControlUC.cs             (Khung chat giống Messenger, mã hóa tin nhắn)
    │   │   ├── NotificationPanelUC.cs       (Bảng thả xuống khi bấm vào icon Quả chuông)
    │   │   ├── ProfileUC.cs                 (Cập nhật thông tin cá nhân, đổi Avatar, đổi mật khẩu)
    │   │   └── VideoCallForm.cs             (Lưu ý: Cái này phải là FORM để có thể phóng to Toàn màn hình)
    │   │
    │   └── Main                             (Khung xương của ứng dụng)
    │       └── MainDashboard.cs             (Form chính chứa Sidebar Menu bên trái và Panel chứa UC bên phải)
    │
    ├── Assets                           (Thư mục chứa tài nguyên tĩnh)
    │
    ├── Program.cs                       (Khởi động app, chờ SupabaseManager nối mạng)
    └── Healthcare.Client.csproj
```