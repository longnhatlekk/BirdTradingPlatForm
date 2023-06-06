using System;
using System.Collections.Generic;

namespace BirdPlatForm.BirdPlatform;

public partial class TbDiscount
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? StartDate { get; set; }

    public bool? Status { get; set; }

    public int? ProductId { get; set; }

    public virtual TbProduct IdNavigation { get; set; } = null!;
}
