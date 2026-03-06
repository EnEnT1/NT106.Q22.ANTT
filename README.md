

##**Quy định commit**



##**Cấu trúc thư mục** 
NT106.Solution
│
├── Healthcare.Shared(Class Library)
│   ├── Constants
│   ├── Data Transfer Objects
│   ├── Enums
│   └── Models
│       ├── Appointment.cs
│       ├── HealthIndicator.cs
│       ├── PatientRecord.cs
│       └── User.cs
│
├── Healthcare.Server (ASP.NET Core Web API)
│   ├── Controllers
│   │   ├── AppointmentController.cs
│   │   ├── AuthController.cs
│   │   └── PatientController.cs
│   ├── Cryptography
│   │   ├── AESManager.cs (Mã hóa đối xứng hồ sơ bệnh án)
│   │   ├── HashHelper.cs (Băm mật khẩu PBKDF2 + Salt)
│   │   └── RSAManager.cs (Mã hóa bất đối xứng)
│   ├── Data
│   │   ├── AppDbContext.cs (Tích hợp PostgreSQL qua Npgsql)
│   │   └── Migrations (Nơi chứa các file cập nhật CSDL)
│   ├── Hubs
│   │   └── ChatHub.cs (Quản lý kết nối SignalR Real-time)
│   ├── Network
│   │   ├── Email
│   │   │   └── MailProvider.cs (Cấu hình gửi OTP)
│   │   └── FileTransfer
│   │       └── FileService.cs
│   ├── Services
│   │   ├── CryptoService.cs
│   │   ├── EmailService.cs
│   │   └── PaymentService.cs
│   ├── appsettings.json (Chứa Connection String của PostgreSQL)
│   └── Program.cs (Cấu hình DI, JWT, Npgsql)
│
├── Healthcare.Client (Project: Windows Forms)
│   ├── APIClient
│   │   ├── AuthClient.cs
│   │   ├── BaseHttpClient.cs (Tự động đính kèm JWT Token)
│   │   └── HealthRecordClient.cs
│   ├── Assets
│   │   ├── Icons
│   │   └── Styles
│   ├── Helpers
│   │   └── TokenStorage.cs (Lưu trữ JWT trong phiên làm việc)
│   ├── Media
│   │   └── WebRtcPeerConnection.cs (Xử lý Video Call)
│   ├── Monitoring
│   │   ├── NetworkDiagnostics.cs
│   │   └── TrafficAnalyzer.cs
│   ├── RealTimeClient
│   │   └── ChatSignalRClient.cs (Lắng nghe tin nhắn từ Hub)
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
