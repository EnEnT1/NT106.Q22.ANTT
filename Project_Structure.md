NT106.Solution
│
├── Healthcare.Shared (Class Library)
│   ├── Constants
│   ├── Cryptography                   
│   │   ├── AESManager.cs              (Mã hóa đối xứng file/hồ sơ bệnh án từ Client)
│   │   └── RSAManager.cs              (Mã hóa bất đối xứng khóa AES)
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
│   │   ├── AuthController.cs          (Nhận request đăng nhập từ Client, gọi Firebase xác thực)
│   │   └── PatientController.cs
│   ├── Cryptography                 
│   │   └── HashHelper.cs              (Có thể bỏ đi nếu Firebase Auth đã tự động băm mật khẩu)
│   ├── Data
│   │   └── FirestoreRepository.cs    (Cloud Firestore)
│   ├── Hubs
│   │   └── ChatHub.cs                 (giao tiếp Firebase Realtime)
│   ├── Network
│   │   ├── Auth
│   │   │   └── FirebaseAuthProvider.cs (Firebase lo toàn bộ việc gửi OTP Email)
│   │   └── FileTransfer
│   │       └── FirebaseStorageService.cs  (Thay thế lưu ổ cứng cục bộ bằng Firebase Storage)
│   ├── Services
│   │   ├── CryptoService.cs          
│   │   ├── NotificationService.cs     (Dùng Firebase Cloud Messaging (FCM))
│   │   └── PaymentService.cs
│   ├── appsettings.json               (Chứa đường dẫn tới file 'firebase-adminsdk.json' thay vì Connection String)
│   └── Program.cs                     (Cấu hình DI, JWT, và khởi tạo FirebaseApp)
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
│   │   └── TokenStorage.cs            (Lưu JWT Token do Firebase/Server cấp)
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