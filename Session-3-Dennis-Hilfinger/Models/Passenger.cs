using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Session_3_Dennis_Hilfinger.Models
{
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
