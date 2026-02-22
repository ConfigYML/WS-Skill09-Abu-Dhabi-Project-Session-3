using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Session_3_Dennis_Hilfinger.Models
{
    public class FlightDTO
    {
        public int Id_First { get; set; }
        public int Id_Second { get; set; }
        public DateOnly FlightDate { get; set; }
        public TimeOnly FlightTime { get; set; }
        public DateTime FlightDt => new DateTime(FlightDate.Year, FlightDate.Month, FlightDate.Day, FlightTime.Hour, FlightTime.Minute, FlightTime.Second);
        public string DepartureAirport { get; set; }
        public string DestinationAirport { get; set; }
        public string FlightNumbers { get; set; }
        public string Cabin { get; set; }
        public int CabinPrice
        {
            get
            {
                if (Cabin == "Economy")
                {
                    return BasePrice;
                }
                else if (Cabin == "Business")
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
        public int StopCount { get; set; } = 0;
    }
}
