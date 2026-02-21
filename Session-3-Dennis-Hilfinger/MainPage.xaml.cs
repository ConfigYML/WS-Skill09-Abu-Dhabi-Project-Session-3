using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Collections.ObjectModel;
using Windows.Devices.AllJoyn;

namespace Session_3_Dennis_Hilfinger
{
    public partial class MainPage : ContentPage
    {
        private bool HasReturn = false;
        public ObservableCollection<FlightDTO> OutboundFlightList = new ObservableCollection<FlightDTO>();
        public ObservableCollection<FlightDTO> ReturnFlightList = new ObservableCollection<FlightDTO>();
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            FillFilterData();
        }
        
        private async void FillFilterData()
        {
            CabinPicker.Items.Clear();
            CabinPicker.Items.Add("Economy");
            CabinPicker.Items.Add("Business Class");
            CabinPicker.Items.Add("First Class");
            CabinPicker.SelectedIndex = 0;

            DeparturePicker.Items.Clear();
            DeparturePicker.Items.Add("");
            DeparturePicker.SelectedIndex = 0;

            DestinationPicker.Items.Clear();
            DestinationPicker.Items.Add("");
            DestinationPicker.SelectedIndex = 0;

            using (var db = new AirlineContext())
            {
                var airports = await db.Airports.ToListAsync();
                foreach (var airport in airports)
                {
                    DeparturePicker.Items.Add(airport.Iatacode);
                    DestinationPicker.Items.Add(airport.Iatacode);
                }
            }
        }


        private async void LoadData(object sender, EventArgs e)
        {
            using (var db = new AirlineContext())
            {
                string departure = DeparturePicker.SelectedItem.ToString();
                string destination = DestinationPicker.SelectedItem.ToString();
                string cabin = CabinPicker.SelectedItem.ToString();
                DateOnly outboundDate = DateOnly.MinValue;
                DateOnly returnDate = DateOnly.MinValue;
                HasReturn = Return.IsChecked;

                if (departure == destination && !String.IsNullOrEmpty(departure))
                {
                    await DisplayAlert("Info", "Departure and destination airport can not be the same. Please change your selection.", "Ok");
                    return;
                }

                if (!String.IsNullOrEmpty(departure) && String.IsNullOrEmpty(destination))
                {
                    await DisplayAlert("Info", "Please select a departure airport and a destination airport.", "Ok");
                    return;
                }

                if (!String.IsNullOrEmpty(OutboundDateInput.Text) || (HasReturn && !String.IsNullOrEmpty(ReturnDateInput.Text)))
                {
                    if (!DateOnly.TryParse(OutboundDateInput.Text, out DateOnly outbound))
                    {
                        await DisplayAlert("Info", "Outbound date was not in the correct fomat.", "Ok");
                        return;
                    }
                    if (HasReturn)
                    {
                        if (!DateOnly.TryParse(ReturnDateInput.Text, out DateOnly returnD))
                        {
                            await DisplayAlert("Info", "Return date was not in the correct fomat.", "Ok");
                            return;
                        }
                        if (outbound > returnD)
                        {
                            await DisplayAlert("Info", "Return date can not be before outbound date.", "Ok");
                            return;
                        }
                        returnDate = returnD;
                    }
                    outboundDate = outbound;
                }

                if(HasReturn)
                {
                    ReturnFlightHeader.IsVisible = true;
                    ReturnFlightGrid.IsVisible = true;
                } else
                {
                    ReturnFlightHeader.IsVisible = false;
                    ReturnFlightGrid.IsVisible = false;
                }

                var departureAirport = await db.Airports.FirstOrDefaultAsync(a => a.Iatacode == departure);
                var arrivalAirport = await db.Airports.FirstOrDefaultAsync(a => a.Iatacode == destination);


                OutboundFlightList.Clear();
                ReturnFlightList.Clear();

                var outboundFlights = db.Schedules
                    .Where(f => f.Confirmed == true
                    && f.Route.DepartureAirportId == departureAirport.Id 
                    && f.Route.ArrivalAirportId == arrivalAirport.Id);

                if (OutboundCheckBox.IsChecked)
                {
                    var priorDate = outboundDate.AddDays(-3);
                    var laterDate = outboundDate.AddDays(3);
                    outboundFlights = outboundFlights.Where(f => f.Date >= priorDate && f.Date <= laterDate);
                } else
                {
                    outboundFlights = outboundFlights.Where(f => f.Date == outboundDate);
                }

                /*foreach (var flight in filtered)
                {
                    new FlightDTO
                    {
                        Id = flight.Id,
                        FlightDate = flight.Date,
                        FlightTime = flight.Time,
                        DepartureAirport = flight.Route.DepartureAirport.Iatacode,
                        DestinationAirport = flight.Route.ArrivalAirport.Iatacode,
                        FlightNumber = int.Parse(flight.FlightNumber),
                        Aircraft = flight.AircraftId,
                        BasePrice = decimal.ToInt32(flight.EconomyPrice)
                    };
                }*/

            }
        }
        /*
        private async void CancelFlight(object sender, EventArgs e)
        {
            if (FlightGrid.SelectedItem != null)
            {
                FlightDTO flight = FlightGrid.SelectedItem as FlightDTO; 
                using (var db = new AirlineContext())
                {
                    var fl = db.Schedules.FirstOrDefault(s => s.Id == flight.Id);
                    fl.Confirmed = !fl.Confirmed;
                    db.Update(fl);
                    await db.SaveChangesAsync();
                    LoadData(null, null);
                }
            }
        }*/

        private async void OutboundChecked(object sender, EventArgs e)
        {
            //LoadData(null, null);
        }
        private async void ReturnChecked(object sender, EventArgs e)
        {
            //LoadData(null, null);
        }

        private async void BookFlight(object sender, EventArgs e)
        {
            
        }

        private async void Exit(object sender, EventArgs e)
        {
            Application.Current.Quit();
        }

        public class FlightDTO
        {
            public int Id { get; set; }
            public DateOnly FlightDate { get; set; }
            public TimeOnly FlightTime { get; set; }
            public DateTime FlightDt => new DateTime(FlightDate.Year, FlightDate.Month, FlightDate.Day, FlightTime.Hour, FlightTime.Minute, FlightTime.Second);
            public string DepartureAirport { get; set; }
            public string DestinationAirport { get; set; }
            public int FlightNumber { get; set; }
            public string Cabin { get; set; }
            public int Price { get
                {
                    if (Cabin == "Economy")
                    {
                        return BasePrice;
                    }
                    else if (Cabin == "Business Class")
                    {
                        return BusinessPrice;
                    }
                    else if (Cabin == "First Class")
                    {
                        return FirstClassPrice;
                    }
                    else
                    {
                        return BasePrice;
                    }
                }
            }
            public int BasePrice { get; set; }
            public int BusinessPrice => (int)(BasePrice * 1.35);
            public int FirstClassPrice => (int)(BusinessPrice * 1.3);
            public int StopCount { get; set; }
        }

    }
    
}
