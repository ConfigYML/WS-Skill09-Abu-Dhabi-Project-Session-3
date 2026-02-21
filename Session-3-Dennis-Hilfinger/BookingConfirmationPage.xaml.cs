
using Microsoft.EntityFrameworkCore;
using Session_3_Dennis_Hilfinger.Models;
using System.Collections.ObjectModel;

namespace Session_3_Dennis_Hilfinger;

public partial class BookingConfirmationPage : ContentPage, IQueryAttributable
{
    private FlightDTO outboundFlight;
    private FlightDTO returnFlight;
    private int passengerCount;
    private ObservableCollection<Passenger> passengers = new ObservableCollection<Passenger>();
    public BookingConfirmationPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        outboundFlight = query["outboundFlight"] as FlightDTO;
        returnFlight = query["returnFlight"] as FlightDTO;
        passengerCount = (int)query["passengerAmount"];
        FillLabels();
    }

    private async void FillLabels()
    {
        OutboundFromLabel.Text = outboundFlight.DepartureAirport;
        OutboundToLabel.Text = outboundFlight.DestinationAirport;
        OutboundCabinTypeLabel.Text = outboundFlight.Cabin;
        OutboundDateLabel.Text = outboundFlight.FlightDate.ToString("dd.MM.yyyy");
        OutboundFlightNumberLabel.Text = outboundFlight.FlightNumbers;


        ReturnFromLabel.Text = returnFlight.DepartureAirport;
        ReturnToLabel.Text = returnFlight.DestinationAirport;
        ReturnCabinTypeLabel.Text = returnFlight.Cabin;
        ReturnDateLabel.Text = returnFlight.FlightDate.ToString("dd.MM.yyyy");
        ReturnFlightNumberLabel.Text = returnFlight.FlightNumbers;

        using(var db = new AirlineContext())
        {
            var countryList = await db.Countries.ToListAsync();
            foreach(var country in countryList)
            {
                CountryPicker.Items.Add(country.Name);
            }
        }
    }

    private async void AddPassenger(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "Feature not implemented yet. Please check back later.", "OK");
    }
    private async void RemovePassenger(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "Feature not implemented yet. Please check back later.", "OK");
    }

    private async void Cancel(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void ConfirmBooking(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "Feature not implemented yet. Preview of next page available.", "OK");
        ShellNavigationQueryParameters parameters = new ShellNavigationQueryParameters()
        {
            {"outboundFlight", outboundFlight},
            {"returnFlight", returnFlight},
            {"passengerAmount", passengerCount} 
        };
        await Shell.Current.GoToAsync("BillingConfirmationPage", parameters);
    }

    public class Passenger
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string PassportNumber { get; set; }
        public int CountryId { get; set; }
        public Country PassportCountry { get; set; }
        public string Phone { get; set; }
    }
}