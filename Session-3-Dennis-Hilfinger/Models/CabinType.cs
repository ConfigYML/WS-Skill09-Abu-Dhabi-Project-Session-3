using System;
using System.Collections.Generic;

namespace Session_3_Dennis_Hilfinger;

public partial class CabinType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
