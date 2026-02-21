namespace Session_3_Dennis_Hilfinger
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(BookingConfirmationPage), typeof(BookingConfirmationPage));
            Routing.RegisterRoute(nameof(BillingConfirmationPage), typeof(BillingConfirmationPage));
        }
    }
}
