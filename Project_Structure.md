```

NT106.Solution
│
├── Healthcare.Shared (Class Library)
│   ├── Constants                  (Chứa các hằng số dùng chung)
│   ├── Cryptography                   
│   │   ├── AESManager.cs          (WinForms tự mã hóa bệnh án thành chuỗi bí mật trước khi đẩy lên Firebase)
│   │   └── RSAManager.cs          (Mã hóa khóa riêng tư)
│   ├── Data Transfer Objects      (DTOs: Chứa các class gói dữ liệu gửi giữa WinForms và Server API)
│   ├── Enums                      (Định nghĩa trạng thái: Đã thanh toán, Đang chờ...)
│   └── Models                     (Các class này sẽ được ánh xạ trực tiếp thành Document trên Firestore)
│       ├── Appointment.cs             
│       ├── HealthIndicator.cs
│       ├── PatientRecord.cs
│       └── User.cs
│
├── Healthcare.Server (ASP.NET Core Web API - Microservice Server)
│   ├── Controllers                    
│   │   ├── PaymentController.cs   (Nhận yêu cầu tạo link thanh toán từ WinForms)
│   │   └── AiController.cs        (Nhận ảnh đơn thuốc từ WinForms để đưa AI phân tích)
│   ├── FirebaseIntegration        
│   │   └── FirebaseAdminHelper.cs (Kiểm tra xem User gọi API có token hợp lệ từ Firebase hay không)
│   ├── Services                       
│   │   ├── PaymentService.cs      (Chứa logic mã hóa chữ ký điện tử, kết nối VNPay/MoMo/Stripe)
│   │   ├── AiPrescriptionService.cs(Gọi API của OpenAI/Google Vision trích xuất văn bản từ ảnh)
│   │   └── ScheduledWorker.cs     (Tiến trình chạy ngầm 24/7: Quét Firebase để hẹn giờ nhắc uống thuốc)
│   ├── appsettings.json           (CHỈ CHỨA: API Key VNPay, API Key ChatGPT, và link file firebase-adminsdk.json)
│   └── Program.cs                 (Cấu hình Server cực nhẹ, gỡ bỏ toàn bộ Entity Framework/SQL)
│
├── Healthcare.Client (Windows Forms - Thick Client)
│   ├── APIClient                  (Dùng HttpClient để gọi các API đặc thù của Server C#)
│   │   ├── AiClient.cs            (Gửi ảnh lên AiController)
│   │   ├── BaseHttpClient.cs      (Tự động đính kèm Firebase ID Token vào Header bảo mật)
│   │   └── PaymentClient.cs       (Gọi PaymentController lấy link thanh toán)
│   │
│   ├── FirebaseIntegration        (TRÁI TIM MỚI: WinForms giao tiếp trực tiếp với đám mây)
│   │   ├── FirebaseAuthService.cs (Xử lý Đăng nhập, Đăng ký, Gửi OTP SMS/Email qua Firebase)
│   │   ├── FirebaseManager.cs     (Khởi tạo cấu hình Firebase API Key lúc mở app)
│   │   ├── FirebaseRealtimeService.cs (Lắng nghe tin nhắn Chat và tín hiệu WebRTC)
│   │   ├── FirebaseStorageService.cs  (Tải lên/Tải xuống file X-Quang, hình ảnh đơn thuốc)
│   │   └── FirestoreService.cs    (Thực hiện Thêm/Sửa/Xóa dữ liệu bệnh án trực tiếp siêu tốc)
│   │
│   ├── Assets
│   │   ├── Icons
│   │   └── Styles
│   ├── Helpers
│   │   └── SessionStorage.cs      (Lưu thông tin User và Token hiện tại)
│   ├── Media
│   │   └── WebRtcPeerConnection.cs(Thuật toán truyền Video P2P, dùng Firebase Realtime làm trạm môi giới)
│   ├── Monitoring
│   │   ├── NetworkDiagnostics.cs  (Đo Ping mạng của Client)
│   │   └── TrafficAnalyzer.cs     
│   ├── UI                         
│   │   ├── Auth
│   │   │   ├── LoginForm.cs
│   │   │   └── OTPVerificationForm.cs
│   │   ├── Controls
│   │   │   ├── Admin
│   │   │   │   └── AdminDashboardUC.cs
│   │   │   ├── Doctor
│   │   │   │   ├── DoctorDashboardUC.cs
│   │   │   │   └── MedicalRecordUC.cs
│   │   │   ├── Patient
│   │   │   │   ├── DoctorSearchUC.cs
│   │   │   │   ├── HealthTrackingUC.cs
│   │   │   │   └── PatientDashboardUC.cs
│   │   │   └── Shared
│   │   │       ├── ChatControlUC.cs
│   │   │       └── VideoCallForm.cs
│   │   └── Main
│   │       └── MainDashboard.cs
│   └── Program.cs 
│
└── README.md
```