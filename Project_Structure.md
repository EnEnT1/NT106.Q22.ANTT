##**Cấu trúc thư mục** 
NT106.Solution
│
├── Healthcare.Shared 
│   ├── Constants
│   ├── Cryptography                   
│   │   ├── AESManager.cs       (Mã hóa đối xứng file/hồ sơ bệnh án)
│   │   └── RSAManager.cs       (Mã hóa bất đối xứng khóa AES)
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
│   │   └── HashHelper.cs              (Băm mật khẩu PBKDF2 + Salt)
│   ├── Data
│   │   ├── AppDbContext.cs            
│   │   └── Migrations                 (Nơi chứa các file cập nhật CSDL)
│   ├── Hubs
│   │   └── ChatHub.cs                 (Quản lý kết nối Socket/SignalR real-time)
│   ├── Network
│   │   ├── Email
│   │   │   └── MailProvider.cs        (Cấu hình gửi OTP qua SMTP)
│   │   └── FileTransfer
│   │       └── FileService.cs         (Xử lý lưu trữ file X-Quang, hồ sơ mã hóa)
│   ├── Services
│   │   ├── CryptoService.cs           (Xử lý logic giải mã tại server)
│   │   ├── EmailService.cs
│   │   └── PaymentService.cs
│   ├── appsettings.json               (Chứa Connection String của PostgreSQL)
│   └── Program.cs                     (Cấu hình DI, JWT, Npgsql)
│
├── Healthcare.Client (Windows Forms)
│   ├── APIClient
│   │   ├── AuthClient.cs
│   │   ├── BaseHttpClient.cs          
│   │   ├── FileTransferClient.cs     
│   │   └── HealthRecordClient.cs
│   ├── Assets
│   │   ├── Icons
│   │   └── Styles
│   ├── Helpers
│   │   └── TokenStorage.cs           
│   ├── Media
│   │   └── WebRtcPeerConnection.cs   
│   ├── Monitoring
│   │   ├── NetworkDiagnostics.cs
│   │   └── TrafficAnalyzer.cs
│   ├── RealTimeClient
│   │   └── ChatSignalRClient.cs      
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