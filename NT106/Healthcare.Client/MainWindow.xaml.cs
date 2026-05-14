using Healthcare.Client.UI.Shell;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Healthcare.Client
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;

            if (MicaController.IsSupported())
            {
                SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
            }

            // Ra lệnh cho khung chứa load giao diện trang Đăng nhập
            rootFrame.Navigate(typeof(UI.Auth.LoginPage));
        }
    }
}