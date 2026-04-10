using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class ExaminationPage : Page
    {
        private string _appointmentId;

        public ExaminationPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string appointmentId)
            {
                _appointmentId = appointmentId;
                LoadAppointmentData(_appointmentId);
            }
        }

        private void LoadAppointmentData(string appointmentId)
        {
            // test tạm
            System.Diagnostics.Debug.WriteLine("AppointmentId nhận được: " + appointmentId);

            // TODO:
            // gọi SupabaseDbService lấy dữ liệu ca khám theo appointmentId
        }
    }
}
