```

NT106.Solution 
│
├── Healthcare.Server (Project 1: ASP.NET Core Web API)
│   │
│   ├── Controllers                      (Nơi nhận các HTTP Request từ WinForms hoặc VNPay)
│   │   ├── AiController.cs              (Nhận ảnh đơn thuốc, trả về kết quả AI)
│   │   └── PaymentController.cs         (Nhận Webhook từ VNPay/MoMo khi thanh toán xong)
│   │
│   ├── Models                     (Nơi chứa các thư viện)
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
│   ├── Services                         
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
└── Healthcare.Client 
    │
    ├── APIClient                        (Giao tiếp nhánh 1: Chọc về Server C# nội bộ)
    │   ├── AiClient.cs                  (Gói ảnh gửi lên Server phân tích)
    │   ├── PaymentClient.cs             (Gửi request lấy URL thanh toán VNPay)
    │   └── BaseHttpClient.cs            (Cấu hình HttpClient chung, tự động nhét Token)
    │
    ├── SupabaseIntegration              (Giao tiếp nhánh 2: Chọc thẳng lên Cloud Database)
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
    ├── Monitoring                       (Giám sát mạng - Yếu tố ghi điểm môn Mạng máy tính)
    │   ├── NetworkDiagnostics.cs        (Đo Ping, phát hiện rớt mạng)
    │   └── TrafficAnalyzer.cs           (Đo băng thông upload/download của Video Call)
    │
    ├── UI                               (Giao diện WinForms)
    │   ├── Auth    (Giao diện Đăng nhập / Đăng ký)
    │   │                    
    │   ├── Controls.Shared              (Các mảnh giao diện nhỏ có thể dùng lại)
    │   │   ├── ChatControlUC.cs         (UserControl khung chat tin nhắn)
    │   │   └── VideoCallForm.cs         (Cửa sổ nổi bật lên khi có cuộc gọi)
    │   └── Main
    │       └── MainDashboard.cs         (Giao diện chính chứa Menu trái và các khung hiển thị)
    │
    ├── Assets                           (Thư mục chứa tài nguyên tĩnh)
    │
    ├── Program.cs                       (Khởi động app, chờ SupabaseManager nối mạng)
    └── Healthcare.Client.csproj
```