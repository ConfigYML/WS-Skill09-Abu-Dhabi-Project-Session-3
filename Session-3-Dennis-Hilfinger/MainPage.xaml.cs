using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Session_3_Dennis_Hilfinger.Models;
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
            CabinPicker.Items.Add("Business");
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
            try
            {
                using (var db = new AirlineContext())
                {
                    string departure = DeparturePicker.SelectedItem.ToString();
                    string destination = DestinationPicker.SelectedItem.ToString();
                    string cabin = CabinPicker.SelectedItem.ToString();
                    DateOnly outboundDate = DateOnly.MinValue;
                    DateOnly returnDate = DateOnly.MinValue;
                    HasReturn = Return.IsChecked;


                    if (String.IsNullOrEmpty(departure) || String.IsNullOrEmpty(destination))
                    {
                        await DisplayAlert("Info", "Please select a departure airport and a destination airport.", "Ok");
                        return;
                    }

                    if (departure == destination && !String.IsNullOrEmpty(departure))
                    {
                        await DisplayAlert("Info", "Departure and destination airport can not be the same. Please change your selection.", "Ok");
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
                    else
                    {
                        await DisplayAlert("Info", "Please enter the required date(s) for the flight.", "Ok");
                        return;
                    }

                    if (HasReturn)
                    {
                        ReturnFlightHeader.IsVisible = true;
                        ReturnFlightGrid.IsVisible = true;
                    }
                    else
                    {
                        ReturnFlightHeader.IsVisible = false;
                        ReturnFlightGrid.IsVisible = false;
                    }

                    var departureAirport = await db.Airports.FirstOrDefaultAsync(a => a.Iatacode == departure);
                    var arrivalAirport = await db.Airports.FirstOrDefaultAsync(a => a.Iatacode == destination);

                    OutboundFlightList.Clear();
                    ReturnFlightList.Clear();


                    // Outbound flights
                    var outboundFlights = db.Schedules
                        .Include(s => s.Route).ThenInclude(r => r.DepartureAirport)
                        .Include(s => s.Route).ThenInclude(r => r.ArrivalAirport)
                        .Where(f => f.Confirmed == true);

                    if (OutboundCheckBox.IsChecked)
                    {
                        var priorDate = outboundDate.AddDays(-3);
                        var laterDate = outboundDate.AddDays(3);
                        outboundFlights = outboundFlights.Where(f => f.Date >= priorDate && f.Date <= laterDate);
                    }
                    else
                    {
                        outboundFlights = outboundFlights.Where(f => f.Date == outboundDate);
                    }


                    var outboundDeparture = outboundFlights.Where(f => f.Route.DepartureAirportId == departureAirport.Id);
                    var outboundArrival = outboundFlights.Where(f => f.Route.ArrivalAirportId == arrivalAirport.Id);
                    var matches = await outboundDeparture
                                            .Select(d => d.Route.ArrivalAirport.Id)
                                            .Intersect(outboundArrival.Select(a => a.Route.DepartureAirport.Id))
                                            .ToListAsync();

                    List<Schedule[]> outboundIndirectFlights = new List<Schedule[]>();

                    foreach (var match in matches)
                    {
                        var firstFlight = await outboundFlights.FirstOrDefaultAsync(f => f.Route.ArrivalAirportId == match
                                                                                    && f.Route.DepartureAirportId == departureAirport.Id);
                        TimeOnly earliestLeaveTime = firstFlight.Time.AddMinutes(firstFlight.Route.FlightTime);
                        var secondFlight = await outboundFlights
                                                    .Where(f => f.Route.DepartureAirportId == match 
                                                            && f.Time > earliestLeaveTime
                                                            && f.Route.ArrivalAirportId == arrivalAirport.Id)
                                                    .FirstOrDefaultAsync();
                        if (secondFlight != null)
                        {
                            outboundIndirectFlights.Add([firstFlight, secondFlight]);
                        }
                    }
                    

                    var outboundDirectFlights = outboundFlights.Where(f => f.Route.DepartureAirportId == departureAirport.Id
                                                            && f.Route.ArrivalAirportId == arrivalAirport.Id);

                    OutboundFlightList.Clear();
                    foreach (var flight in outboundDirectFlights)
                    {
                        if (flight.Route == null)
                        {
                            continue;
                        }
                        OutboundFlightList.Add(new FlightDTO
                        {
                            Id_First = flight.Id,
                            FlightDate = flight.Date,
                            FlightTime = flight.Time,
                            DepartureAirport = flight.Route.DepartureAirport.Iatacode,
                            DestinationAirport = flight.Route.ArrivalAirport.Iatacode,
                            FlightNumbers = flight.FlightNumber,
                            BasePrice = decimal.ToInt32(flight.EconomyPrice),
                            Cabin = cabin
                        });
                    }

                    foreach (var flight in outboundIndirectFlights)
                    {
                        if (flight.Length != 2)
                        {
                            continue;
                        }
                        if (flight[0].Route == null || flight[1].Route == null)
                        {
                            continue;
                        }
                        OutboundFlightList.Add(new FlightDTO
                        {
                            Id_First = flight[0].Id,
                            Id_Second = flight[1].Id,
                            FlightDate = flight[0].Date,
                            FlightTime = flight[0].Time,
                            DepartureAirport = flight[0].Route.DepartureAirport.Iatacode,
                            DestinationAirport = flight[1].Route.ArrivalAirport.Iatacode,
                            FlightNumbers = $"{flight[0].FlightNumber} - {flight[1].FlightNumber}",
                            BasePrice = decimal.ToInt32(flight[0].EconomyPrice + flight[1].EconomyPrice),
                            Cabin = cabin
                        });
                    }

                    OutboundFlightGrid.ItemsSource = OutboundFlightList;

                    // Return flights
                    if (HasReturn)
                    {

                        var returnFlights = db.Schedules
                            .Include(s => s.Route).ThenInclude(r => r.DepartureAirport)
                            .Include(s => s.Route).ThenInclude(r => r.ArrivalAirport)
                            .Where(f => f.Confirmed == true);

                        if (ReturnCheckBox.IsChecked)
                        {
                            var priorDate = returnDate.AddDays(-3);
                            var laterDate = returnDate.AddDays(3);
                            returnFlights = returnFlights.Where(f => f.Date >= priorDate && f.Date <= laterDate);
                        }
                        else
                        {
                            returnFlights = returnFlights.Where(f => f.Date == returnDate);
                        }

                        var returnDeparture = returnFlights.Where(f => f.Route.DepartureAirportId == arrivalAirport.Id);
                        var returnArrival = returnFlights.Where(f => f.Route.ArrivalAirportId == departureAirport.Id);
                        var returnMatches = await returnDeparture
                                                .Select(d => d.Route.ArrivalAirport.Id)
                                                .Intersect(returnArrival.Select(a => a.Route.DepartureAirport.Id))
                                                .ToListAsync();

                        List<Schedule[]> returnIndirectFlights = new List<Schedule[]>();

                        foreach (var match in returnMatches)
                        {
                            var firstFlight = await returnFlights.FirstOrDefaultAsync(f => f.Route.ArrivalAirportId == match
                                                                                        && f.Route.DepartureAirportId == arrivalAirport.Id);
                            TimeOnly earliestLeaveTime = firstFlight.Time.AddMinutes(firstFlight.Route.FlightTime);
                            var secondFlight = await returnFlights
                                                        .Where(f => f.Route.DepartureAirportId == match
                                                                && f.Time > earliestLeaveTime
                                                                && f.Route.ArrivalAirportId == departureAirport.Id)
                                                        .FirstOrDefaultAsync();
                            if (secondFlight != null)
                            {
                                returnIndirectFlights.Add([firstFlight, secondFlight]);
                            }
                        }

                        var returnDirectFlights = returnFlights.Where(f => f.Route.DepartureAirportId == arrivalAirport.Id
                                                                && f.Route.ArrivalAirportId == departureAirport.Id);

                        foreach (var flight in returnDirectFlights)
                        {
                            if (flight.Route == null)
                            {
                                continue;
                            }
                            ReturnFlightList.Add(new FlightDTO
                            {
                                Id_First = flight.Id,
                                FlightDate = flight.Date,
                                FlightTime = flight.Time,
                                DepartureAirport = flight.Route.DepartureAirport.Iatacode,
                                DestinationAirport = flight.Route.ArrivalAirport.Iatacode,
                                FlightNumbers = flight.FlightNumber,
                                BasePrice = decimal.ToInt32(flight.EconomyPrice),
                                Cabin = cabin
                            });
                        }

                        foreach (var flight in returnIndirectFlights)
                        {
                            if (flight.Length != 2)
                            {
                                continue;
                            }
                            if (flight[0].Route == null || flight[1].Route == null)
                            {
                                continue;
                            }
                            ReturnFlightList.Add(new FlightDTO
                            {
                                Id_First = flight[0].Id,
                                Id_Second = flight[1].Id,
                                FlightDate = flight[0].Date,
                                FlightTime = flight[0].Time,
                                DepartureAirport = flight[0].Route.DepartureAirport.Iatacode,
                                DestinationAirport = flight[1].Route.ArrivalAirport.Iatacode,
                                FlightNumbers = $"{flight[0].FlightNumber} - {flight[1].FlightNumber}",
                                BasePrice = decimal.ToInt32(flight[0].EconomyPrice + flight[1].EconomyPrice),
                                Cabin = cabin
                            });
                        }

                        ReturnFlightGrid.ItemsSource = ReturnFlightList;
                    }

                }
            } catch
            {
                await DisplayAlert("Error", "An error occurred while loading the data. Please check your input and try again.", "Ok");
                return;
            }
        }

        private async void BookingTypeChanged(object sender, EventArgs e)
        {
            if (Return.IsChecked)
            {
                ReturnDateInput.IsEnabled = true;
            } else
            {
                ReturnDateInput.IsEnabled = false;
            }
        }

        private async void OutboundChecked(object sender, EventArgs e)
        {
            LoadData(null, null);
        }
        private async void ReturnChecked(object sender, EventArgs e)
        {
            LoadData(null, null);
        }

        private async void BookFlight(object sender, EventArgs e)
        {
            var outboundFlight = OutboundFlightGrid.SelectedItem as FlightDTO;
            var returnFlight = ReturnFlightGrid.SelectedItem as FlightDTO;
            if (outboundFlight == null)
            {
                await DisplayAlert("Info", "Please select an outbound flight to book.", "Ok");
                return;
            }
            if (HasReturn && returnFlight == null)
            {
                await DisplayAlert("Info", "Please select a return flight to book.", "Ok");
                return;
            }
            if (!int.TryParse(PassengerAmount.Text, out int passengerAmount))
            {
                await DisplayAlert("Info", "Passenger amount must be a valid number.", "Ok");
                return;
            }

            if (passengerAmount <= 0)
            {
                await DisplayAlert("Info", "Passenger amount must be at least 1.", "Ok");
                return;
            }

            // Check for available seats
            try
            {
                using (var db = new AirlineContext())
                {
                    var outboundSchedule = await db.Schedules.FirstOrDefaultAsync(s => s.Id == outboundFlight.Id_First); 
                    Schedule outboundSchedule_Second = null;
                    if (outboundFlight.Id_Second != null)
                    {
                        outboundSchedule_Second = await db.Schedules.FirstOrDefaultAsync(s => s.Id == outboundFlight.Id_Second);
                    }
                    var outboundAircraft = await db.Aircrafts.FirstOrDefaultAsync(a => a.Id == outboundSchedule.AircraftId);
                    var outboundCabinType = await db.CabinTypes.FirstOrDefaultAsync(c => c.Name == outboundFlight.Cabin);

                    switch (outboundFlight.Cabin)
                    {
                        case "Economy":
                            var economySeatsBooked = await db.Tickets.Where(t => t.ScheduleId == outboundSchedule.Id 
                                                                            && t.CabinTypeId == outboundCabinType.Id).CountAsync();
                            var freeEconomySeats = outboundAircraft.EconomySeats - economySeatsBooked;
                            if (freeEconomySeats < passengerAmount)
                            {
                                await DisplayAlert("Info", "Not enough economy seats available on the outbound flight.", "Ok");
                                return;
                            }

                            if (outboundSchedule_Second != null)
                            {
                                economySeatsBooked = await db.Tickets.Where(t => t.ScheduleId == outboundSchedule_Second.Id
                                                                            && t.CabinTypeId == outboundCabinType.Id).CountAsync();
                                freeEconomySeats = outboundAircraft.EconomySeats - economySeatsBooked;
                                if (freeEconomySeats < passengerAmount)
                                {
                                    await DisplayAlert("Info", "Not enough economy seats available on the outbound flight.", "Ok");
                                    return;
                                }
                            }
                            break;

                        case "Business":
                            var businessSeatsBooked = await db.Tickets.Where(t => t.ScheduleId == outboundSchedule.Id 
                                                                            && t.CabinTypeId == outboundCabinType.Id).CountAsync();
                            var freeBusinessSeats = outboundAircraft.EconomySeats - businessSeatsBooked;
                            if (freeBusinessSeats < passengerAmount)
                            {
                                await DisplayAlert("Info", "Not enough business seats available on the outbound flight.", "Ok");
                                return;
                            }

                            if (outboundSchedule_Second != null)
                            {
                                businessSeatsBooked = await db.Tickets.Where(t => t.ScheduleId == outboundSchedule_Second.Id
                                                                            && t.CabinTypeId == outboundCabinType.Id).CountAsync();
                                freeBusinessSeats = outboundAircraft.EconomySeats - businessSeatsBooked;
                                if (freeBusinessSeats < passengerAmount)
                                {
                                    await DisplayAlert("Info", "Not enough business seats available on the outbound flight.", "Ok");
                                    return;
                                }
                            }
                            break;

                        case "First Class":
                            var firstClassSeatsBooked = await db.Tickets.Where(t => t.ScheduleId == outboundSchedule.Id 
                                                                            && t.CabinTypeId == outboundCabinType.Id).CountAsync();
                            var freeFirstClassSeats = outboundAircraft.EconomySeats - firstClassSeatsBooked;
                            if (freeFirstClassSeats < passengerAmount)
                            {
                                await DisplayAlert("Info", "Not enough first class seats available on the outbound flight.", "Ok");
                                return;
                            }

                            if (outboundSchedule_Second != null)
                            {
                                firstClassSeatsBooked = await db.Tickets.Where(t => t.ScheduleId == outboundSchedule_Second.Id
                                                                            && t.CabinTypeId == outboundCabinType.Id).CountAsync();
                                freeFirstClassSeats = outboundAircraft.EconomySeats - firstClassSeatsBooked;
                                if (freeFirstClassSeats < passengerAmount)
                                {
                                    await DisplayAlert("Info", "Not enough first class seats available on the outbound flight.", "Ok");
                                    return;
                                }
                            }
                            break;
                    }

                    if (returnFlight != null)
                    {
                        var returnSchedule = await db.Schedules.FirstOrDefaultAsync(s => s.Id == returnFlight.Id_First);
                        Schedule returnSchedule_Second = null;
                        if (returnFlight.Id_Second != null)
                        {
                            returnSchedule_Second = await db.Schedules.FirstOrDefaultAsync(s => s.Id == returnFlight.Id_Second);
                        }
                        var returnAircraft = await db.Aircrafts.FirstOrDefaultAsync(a => a.Id == returnSchedule.AircraftId);
                        var returnCabinType = await db.CabinTypes.FirstOrDefaultAsync(c => c.Name == returnFlight.Cabin);

                        switch (returnFlight.Cabin)
                        {
                            case "Economy":
                                var economySeatsBooked = await db.Tickets.Where(t => t.ScheduleId == returnSchedule.Id
                                                                                && t.CabinTypeId == returnCabinType.Id).CountAsync();
                                var freeEconomySeats = returnAircraft.EconomySeats - economySeatsBooked;
                                if (freeEconomySeats < passengerAmount)
                                {
                                    await DisplayAlert("Info", "Not enough economy seats available on the return flight.", "Ok");
                                    return;
                                }

                                if (returnSchedule_Second != null)
                                {
                                    economySeatsBooked = await db.Tickets.Where(t => t.ScheduleId == returnSchedule_Second.Id
                                                                                && t.CabinTypeId == returnCabinType.Id).CountAsync();
                                    freeEconomySeats = returnAircraft.EconomySeats - economySeatsBooked;
                                    if (freeEconomySeats < passengerAmount)
                                    {
                                        await DisplayAlert("Info", "Not enough economy seats available on the return flight.", "Ok");
                                        return;
                                    }
                                }

                                break;

                            case "Business":
                                var businessSeatsBooked = await db.Tickets.Where(t => t.ScheduleId == returnSchedule.Id
                                                                                && t.CabinTypeId == returnCabinType.Id).CountAsync();
                                var freeBusinessSeats = returnAircraft.EconomySeats - businessSeatsBooked;
                                if (freeBusinessSeats < passengerAmount)
                                {
                                    await DisplayAlert("Info", "Not enough business seats available on the return flight.", "Ok");
                                    return;
                                }

                                if (returnSchedule_Second != null)
                                {
                                    businessSeatsBooked = await db.Tickets.Where(t => t.ScheduleId == returnSchedule_Second.Id
                                                                                && t.CabinTypeId == returnCabinType.Id).CountAsync();
                                    freeBusinessSeats = returnAircraft.EconomySeats - businessSeatsBooked;
                                    if (freeBusinessSeats < passengerAmount)
                                    {
                                        await DisplayAlert("Info", "Not enough business seats available on the return flight.", "Ok");
                                        return;
                                    }
                                }
                                break;

                            case "First Class":
                                var firstClassSeatsBooked = await db.Tickets.Where(t => t.ScheduleId == returnSchedule.Id
                                                                                && t.CabinTypeId == returnCabinType.Id).CountAsync();
                                var freeFirstClassSeats = returnAircraft.EconomySeats - firstClassSeatsBooked;
                                if (freeFirstClassSeats < passengerAmount)
                                {
                                    await DisplayAlert("Info", "Not enough first class seats available on the return flight.", "Ok");
                                    return;
                                }

                                if (returnSchedule_Second != null)
                                {
                                    firstClassSeatsBooked = await db.Tickets.Where(t => t.ScheduleId == returnSchedule_Second.Id
                                                                                && t.CabinTypeId == returnCabinType.Id).CountAsync();
                                    freeFirstClassSeats = returnAircraft.EconomySeats - firstClassSeatsBooked;
                                    if (freeFirstClassSeats < passengerAmount)
                                    {
                                        await DisplayAlert("Info", "Not enough first class seats available on the return flight.", "Ok");
                                    }
                                }
                                break;
                        }
                    }
                    
                }
            } catch
            {
                await DisplayAlert("Error", "An error occurred while processing your booking. Please try again.", "Ok");
                return;
            }
            

            ShellNavigationQueryParameters parameters = new ShellNavigationQueryParameters()
            {
                {"outboundFlight", outboundFlight},
                {"returnFlight", returnFlight},
                {"passengerAmount", passengerAmount}
            };
            await Shell.Current.GoToAsync("BookingConfirmationPage", parameters);
        }

        private async void Exit(object sender, EventArgs e)
        {
            Application.Current.Quit();
        }

    }
    
}
