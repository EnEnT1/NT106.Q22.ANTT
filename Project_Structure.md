```

NNT106.Solution // Thư mục gốc Solution
│
├── Healthcare.Server // Project 1: ASP.NET Core Web API
│   │
│   ├── Controllers                      // Nơi nhận các HTTP Request từ WinUI 3 hoặc VNPay
│   │   ├── AiController.cs              // Nhận ảnh đơn thuốc, trả về kết quả AI
│   │   └── PaymentController.cs         // Nhận Webhook từ VNPay/MoMo khi thanh toán xong
│   │
│   ├── Models                         
│   │   ├── Identity
│   │   │   ├── User.cs                  // Bảng users
│   │   │   ├── PatientProfile.cs        // Bảng patient_profiles
│   │   │   └── DoctorProfile.cs         // Bảng doctor_profiles
│   │   ├── Core
│   │   │   ├── Appointment.cs           // Bảng appointments
│   │   │   ├── MedicalRecord.cs         // Bảng medical_records
│   │   │   ├── Transaction.cs           // Bảng transactions
│   │   │   ├── HealthMetric.cs          // Bảng health_metrics
│   │   │   ├── LabResult.cs             // Bảng lab_results
│   │   │   ├── MasterMedicine.cs        // Bảng master_medicines
│   │   │   └── MedicationReminder.cs    // Bảng medication_reminders
│   │   ├── Communication
│   │   │   ├── ChatMessage.cs           // Bảng chat_messages
│   │   │   ├── WebrtcSignal.cs          // Bảng webrtc_signals
│   │   │   ├── Notification.cs          // Bảng notifications
│   │   │   └── Review.cs                // Bảng reviews
│   │   └── Security
│   │       └── AuditLog.cs              // Bảng audit_logs
│   │
│   ├── Services                         // Logic nghiệp vụ ẩn phía sau Server
│   │   ├── AiPrescriptionService.cs     // Code gọi API trí tuệ nhân tạo OCR
│   │   ├── PaymentService.cs            // Kiểm tra chữ ký bảo mật giao dịch
│   │   └── ScheduledWorker.cs           // Tiến trình chạy ngầm quét giờ uống thuốc
│   │
│   ├── SupabaseIntegration              
│   │   └── SupabaseAdminHelper.cs       // Xác thực Token JWT xem ai đang gọi API
│   │
│   ├── Properties
│   │   └── launchSettings.json          // Cấu hình cổng mạng Port chạy Server
│   ├── appsettings.json                 // Chứa VNPay Key, AI Key, Supabase Service Role
│   ├── Program.cs                       // File khởi chạy Server, cài đặt Dependency Injection
│   └── Healthcare.Server.csproj         
│
└── Healthcare.Client // Project 2: WinUI 3 / Windows App SDK
    │
    ├── APIClient                        // Giao tiếp nhánh 1: Chọc về Server nội bộ
    │   ├── AiClient.cs                  // Gói ảnh gửi lên Server phân tích
    │   ├── PaymentClient.cs             // Gửi request lấy URL thanh toán VNPay
    │   └── BaseHttpClient.cs            // Cấu hình HttpClient chung, tự động nhét Token
    │
    ├── SupabaseIntegration              // Giao tiếp nhánh 2: Chọc thẳng lên Cloud Database
    │   ├── SupabaseManager.cs           // Singleton khởi tạo kết nối mạng
    │   ├── SupabaseAuthService.cs       // Hàm Sign In, Sign Up
    │   ├── SupabaseDbService.cs         // Hàm CRUD cơ bản: Add, Get, Update, Delete
    │   ├── SupabaseStorageService.cs    // Hàm Upload file ảnh avatar, kết quả X-Quang
    │   └── SupabaseRealtimeService.cs   // WebSockets lắng nghe tin nhắn Chat & Tín hiệu gọi Video
    │
    ├── Models                          
    │   ├── Identity
    │   │   ├── User.cs                  // Bảng users
    │   │   ├── PatientProfile.cs        // Bảng patient_profiles
    │   │   └── DoctorProfile.cs         // Bảng doctor_profiles
    │   ├── Core
    │   │   ├── Appointment.cs           // Bảng appointments
    │   │   ├── MedicalRecord.cs         // Bảng medical_records
    │   │   ├── Transaction.cs           // Bảng transactions
    │   │   ├── HealthMetric.cs          // Bảng health_metrics
    │   │   ├── LabResult.cs             // Bảng lab_results
    │   │   ├── MasterMedicine.cs        // Bảng master_medicines
    │   │   └── MedicationReminder.cs    // Bảng medication_reminders
    │   ├── Communication
    │   │   ├── ChatMessage.cs           // Bảng chat_messages
    │   │   ├── WebrtcSignal.cs          // Bảng webrtc_signals
    │   │   ├── Notification.cs          // Bảng notifications
    │   │   └── Review.cs                // Bảng reviews
    │   └── Security
    │       └── AuditLog.cs              // Bảng audit_logs
    │
    ├── Cryptography                     // Bảo mật cục bộ máy Client
    │   ├── AESManager.cs                // Mã hóa tin nhắn trước khi đẩy lên Supabase
    │   └── RSAManager.cs                // Tạo khóa Public/Private Key
    │
    ├── Helpers                          // Các class tiện ích dùng chung
    │   └── SessionStorage.cs            // Lưu thông tin User đang đăng nhập hiện tại
    │
    ├── Media                            // Xử lý âm thanh, hình ảnh cục bộ
    │   └── WebRtcPeerConnection.cs      // Thuật toán bắt Camera & Micro cho Video Call P2P
    │
    ├── UI                               // Giao diện WinUI 3 XAML
    │   ├── App.xaml / .cs               // Điểm bắt đầu app, khởi tạo SupabaseManager
    │   ├── MainWindow.xaml / .cs        // Cửa sổ gốc chứa NavigationView và Frame điều hướng
    │   │
    │   ├── Views                        // Các trang giao diện (Pages)
    │   │   ├── Auth                     
    │   │   │   ├── LoginPage.xaml / .cs          // Trang Đăng nhập / Đăng ký
    │   │   │   └── ForgotPasswordPage.xaml / .cs // Trang khôi phục mật khẩu
    │   │   ├── Patient                  // Giao diện cho Bệnh nhân
    │   │   │   ├── PatientHomePage.xaml / .cs    // Trang chủ Bệnh nhân
    │   │   │   ├── BookAppointmentPage.xaml / .cs// Đặt lịch khám
    │   │   │   ├── PaymentCheckoutPage.xaml / .cs// Thanh toán viện phí
    │   │   │   ├── MyRecordsPage.xaml / .cs      // Hồ sơ bệnh án
    │   │   │   ├── HealthMetricsPage.xaml / .cs  // Chỉ số sức khỏe
    │   │   │   └── LabResultsPage.xaml / .cs     // Kết quả xét nghiệm
    │   │   └── Doctor                   // Giao diện cho Bác sĩ
    │   │       ├── DoctorHomePage.xaml / .cs     // Trang chủ Bác sĩ (Hàng chờ)
    │   │       ├── ManageSchedulePage.xaml / .cs // Quản lý lịch trực
    │   │       ├── ExaminationPage.xaml / .cs    // Khám bệnh trực tuyến
    │   │       ├── PatientHistoryPage.xaml / .cs // Tiền sử bệnh nhân
    │   │       └── RevenuePage.xaml / .cs        // Thống kê doanh thu
    │   │
    │   └── Components                   // Các UserControl XAML tái sử dụng
    │       ├── ChatControl.xaml / .cs            // Khung chat nhắn tin
    │       ├── NotificationPanel.xaml / .cs      // Bảng thông báo
    │       ├── ProfileControl.xaml / .cs         // Cập nhật thông tin cá nhân
    │       └── VideoCallWindow.xaml / .cs        // Cửa sổ Popup cho Video Call
    │
    ├── Assets                           // Thư mục chứa tài nguyên tĩnh
    │
    └── Healthcare.Client.csproj         // File cấu hình Project Client
```