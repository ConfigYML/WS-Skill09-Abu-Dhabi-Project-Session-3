using Session_3_Dennis_Hilfinger.Models;

namespace Session_3_Dennis_Hilfinger;

public partial class BillingConfirmationPage : ContentPage, IQueryAttributable
{
    private FlightDTO outboundFlight;
    private FlightDTO returnFlight;
    private int passengerCount;
    public BillingConfirmationPage()
	{
		InitializeComponent();
	}

	private async void IssueTickets(object sender, EventArgs e)
	{
        await DisplayAlert("Info", "Feature not implemented yet. Please check back later.", "OK");
    }
    private async void Cancel(object sender, EventArgs e)
    {
        ShellNavigationQueryParameters parameters = new ShellNavigationQueryParameters()
        {
            {"outboundFlight", outboundFlight},
            {"returnFlight", returnFlight},
            {"passengerAmount", passengerCount}
        };
        await Shell.Current.GoToAsync("..", parameters);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        outboundFlight = query["outboundFlight"] as FlightDTO;
        returnFlight = query["returnFlight"] as FlightDTO;
        passengerCount = (int)query["passengerAmount"];
    }
}