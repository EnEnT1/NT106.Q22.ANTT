using Healthcare.Client.UI.Shell;
using Microsoft.UI.Xaml;

namespace Healthcare.Client
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;

            // Ra lệnh cho khung chứa load giao diện trang Đăng nhập
            rootFrame.Navigate(typeof(UI.Auth.LoginPage));
        }
    }
}