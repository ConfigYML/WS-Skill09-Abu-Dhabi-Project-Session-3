
using Microsoft.EntityFrameworkCore;
using Session_3_Dennis_Hilfinger.Models;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.VoiceCommands;

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
        PassengerGrid.ItemsSource = passengers;
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


        if (returnFlight != null)
        {
            ReturnFromLabel.Text = returnFlight.DepartureAirport;
            ReturnToLabel.Text = returnFlight.DestinationAirport;
            ReturnCabinTypeLabel.Text = returnFlight.Cabin;
            ReturnDateLabel.Text = returnFlight.FlightDate.ToString("dd.MM.yyyy");
            ReturnFlightNumberLabel.Text = returnFlight.FlightNumbers;
        } else
        {
            ReturnDetailsLabel.IsVisible = false;
            ReturnDetailsGrid.IsVisible = false;
        }


            using (var db = new AirlineContext())
            {
                var countryList = await db.Countries.ToListAsync();
                foreach (var country in countryList)
                {
                    CountryPicker.Items.Add(country.Name);
                }
            }
    }

    private async void AddPassenger(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(FirstnameInput.Text) || String.IsNullOrEmpty(LastnameInput.Text) 
            || String.IsNullOrEmpty(BirthdateInput.Text) || String.IsNullOrEmpty(PassportNumberInput.Text) 
            || CountryPicker.SelectedIndex < 0 || String.IsNullOrEmpty(PhoneInput.Text))
        {
            await DisplayAlert("Info", "Please fill in all fields.", "OK");
            return;
        }

        if (!DateOnly.TryParse(BirthdateInput.Text, out DateOnly birthdate))
        {
            await DisplayAlert("Info", "Please enter a valid date for the birthdate.", "OK");
            return;
        } 

        if (!int.TryParse(PassportNumberInput.Text, out int passportNumber))
        {
            await DisplayAlert("Info", "Please enter a valid passport number.", "OK");
            return;
        }

        string phoneNumber = PhoneInput.Text;

        if (phoneNumber.StartsWith("+"))
        {
            phoneNumber = phoneNumber.Split("+")[1];
        } 
        if (!long.TryParse(phoneNumber.Replace(" ", String.Empty), out long number))
        {
            await DisplayAlert("Info", "Please enter a valid phone number.", "OK");
            return;
        }

        phoneNumber = phoneNumber.Replace(" ", "-");

        using (var db = new AirlineContext())
        {
            var country = await db.Countries.FirstOrDefaultAsync(c => c.Name == CountryPicker.SelectedItem.ToString());
            passengers.Add(new Passenger()
            {
                FirstName = FirstnameInput.Text,
                LastName = LastnameInput.Text,
                DateOfBirth = birthdate,
                PassportNumber = passportNumber.ToString(),
                CountryId = country.Id,
                PassportCountry = country,
                Phone = phoneNumber
            });
            
            FirstnameInput.Text = String.Empty;
            LastnameInput.Text = String.Empty;
            BirthdateInput.Text = String.Empty;
            PassportNumberInput.Text = String.Empty;
            PhoneInput.Text = String.Empty;
        }

    }
    private async void RemovePassenger(object sender, EventArgs e)
    {
        var passenger = PassengerGrid.SelectedItem as Passenger;
        if (passenger != null)
        {
            passengers.Remove(passengers.FirstOrDefault(p => p.PassportNumber == passenger.PassportNumber));
        }
    }

    private async void PassengerSelectionChanged(object sender, EventArgs e)
    {
        var passenger = PassengerGrid.SelectedItem as Passenger;
        if (passenger != null)
        {
            RemovePassengerBtn.IsEnabled = true;
        } else
        {
            RemovePassengerBtn.IsEnabled = false;
        }
    }

    private async void Cancel(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void ConfirmBooking(object sender, EventArgs e)
    {
        if (passengers.Count != passengerCount)
        {
            await DisplayAlert("Info", $"Passenger amount in list must match the passenger amount selected on the previous page. {passengerCount} passenger(s) are required.", "Ok");
            return;
        }
        ShellNavigationQueryParameters parameters = new ShellNavigationQueryParameters()
        {
            {"outboundFlight", outboundFlight},
            {"returnFlight", returnFlight},
            {"passengerAmount", passengerCount},
            {"passengers", passengers.ToList()}
        };
        await Shell.Current.GoToAsync("BillingConfirmationPage", parameters);
    }
}