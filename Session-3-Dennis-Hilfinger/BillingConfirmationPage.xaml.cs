using Microsoft.EntityFrameworkCore;
using Session_3_Dennis_Hilfinger.Models;

namespace Session_3_Dennis_Hilfinger;

public partial class BillingConfirmationPage : ContentPage, IQueryAttributable
{
    private FlightDTO outboundFlight;
    private FlightDTO returnFlight;
    private int passengerCount;
    private List<Passenger> passengers = new List<Passenger>();
    public BillingConfirmationPage()
	{
		InitializeComponent();
	}

    private async void CalculateTotalAmount()
    {
        double totalAmount = 0;
        if (outboundFlight != null)
        {
            totalAmount += outboundFlight.CabinPrice;
        }
        if (returnFlight != null)
        {
            totalAmount += returnFlight.CabinPrice;
        }
        totalAmount *= passengerCount;
        TotalAmountLabel.Text = $"$ {totalAmount:C}";
    }


    private async void IssueTickets(object sender, EventArgs e)
	{
        if (!CreditCardRadioButton.IsChecked && !CashRadioButton.IsChecked && !VoucherRadioButton.IsChecked)
        {
            await DisplayAlert("Info", "Please select a payment method.", "Ok");
            return;
        }
        using (var db = new AirlineContext())
        {
            var bookingReference = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            while (db.Tickets.Any(t => t.BookingReference == bookingReference))
            {
                bookingReference = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            }

            var users = db.Users.Include(u => u.Role).Where(u => u.Role.Title == "User");
            User targetUser;

            if (users.Any(u => u.LastName == passengers[0].LastName
            && u.FirstName == passengers[0].FirstName
            && u.Birthdate == passengers[0].DateOfBirth))
            {
                targetUser = users.First(u => u.LastName == passengers[0].LastName
                                        && u.FirstName == passengers[0].FirstName
                                        && u.Birthdate == passengers[0].DateOfBirth);
            } else
            {
                int highestId = db.Users.Max(u => u.Id);
                targetUser = new User()
                {
                    Id = highestId + 1,
                    FirstName = passengers[0].FirstName,
                    LastName = passengers[0].LastName,
                    Birthdate = passengers[0].DateOfBirth,
                    Email = string.Empty,
                    Password = string.Empty,
                    RoleId = db.Roles.First(r => r.Title == "User").Id
                };
                db.Users.Add(targetUser);
                await db.SaveChangesAsync();
            }

            var outboundCabinType = db.CabinTypes.FirstOrDefault(ct => ct.Name == outboundFlight.Cabin);

            List<Schedule> outboundSchedules = new List<Schedule>();
            if (outboundFlight.Id_Second != null)
            {
                outboundSchedules = await db.Schedules
                                            .Where(s => s.Id == outboundFlight.Id_First || s.Id == outboundFlight.Id_Second)
                                            .ToListAsync();
            }
            else
            {
                outboundSchedules = await db.Schedules
                                            .Where(s => s.Id == outboundFlight.Id_First)
                                            .ToListAsync();
            }

            foreach(var sched in outboundSchedules)
            {
                foreach (var pass in passengers)
                {
                    db.Tickets.Add(new Ticket()
                    {
                        Firstname = pass.FirstName,
                        Lastname = pass.LastName,
                        PassportNumber = pass.PassportNumber,
                        PassportCountryId = pass.CountryId,
                        Phone = pass.Phone,
                        BookingReference = bookingReference,
                        Schedule = sched,
                        CabinTypeId = outboundCabinType.Id,
                        UserId = targetUser.Id,
                        Confirmed = true
                    });
                }
            }
                
            
            if (returnFlight != null)
            {
                var returnCabinType = db.CabinTypes.FirstOrDefault(ct => ct.Name == returnFlight.Cabin);

                List<Schedule> returnSchedules = new List<Schedule>();
                if (returnFlight.Id_Second != null)
                {
                    returnSchedules = await db.Schedules
                                                .Where(s => s.Id == returnFlight.Id_First || s.Id == returnFlight.Id_Second)
                                                .ToListAsync();
                }
                else
                {
                    returnSchedules = await db.Schedules
                                                .Where(s => s.Id == returnFlight.Id_First)
                                                .ToListAsync();
                }
                foreach (var sched in returnSchedules)
                {
                    foreach (var pass in passengers)
                    {
                        db.Tickets.Add(new Ticket()
                        {
                            Firstname = pass.FirstName,
                            Lastname = pass.LastName,
                            PassportNumber = pass.PassportNumber,
                            PassportCountryId = pass.CountryId,
                            Phone = pass.Phone,
                            BookingReference = bookingReference,
                            Schedule = sched,
                            CabinTypeId = returnCabinType.Id,
                            UserId = targetUser.Id,
                            Confirmed = true
                        });
                    }
                }
                
            }
            await db.SaveChangesAsync();
            await DisplayAlert("Success", $"Tickets issued successfully! Your booking reference is {bookingReference}.", "OK");
        }
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
        passengers = query["passengers"] as List<Passenger>;
        CalculateTotalAmount();
    }
}