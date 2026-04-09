using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Healthcare.Client.SupabaseIntegration;

namespace Healthcare.Client.UI.Components;

public sealed partial class VideoCallWindow : Page
{
    private string _receiverId;
    private bool _isCaller;
    private Supabase.Client _supabase;

    public VideoCallWindow()
    {
        this.InitializeComponent();
        _supabase = SupabaseManager.Instance.Client;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Nhận tham số từ trang ManageSchedulePage gửi sang
        if (e.Parameter is VideoCallParams p)
        {
            _receiverId = p.ReceiverId;
            _isCaller = p.IsCaller;
            TxtStatus.Text = _isCaller ? "Đang gọi..." : "Cuộc gọi đến...";
        }
    }

    private void BtnEndCall_Click(object sender, RoutedEventArgs e)
    {
        if (this.Frame.CanGoBack) this.Frame.GoBack();
    }

    private void BtnMic_Click(object sender, RoutedEventArgs e) { }
    private void BtnCamera_Click(object sender, RoutedEventArgs e) { }
}

// Class phụ trợ để truyền dữ liệu giữa các trang
public class VideoCallParams
{
    public string ReceiverId { get; set; }
    public bool IsCaller { get; set; }
}