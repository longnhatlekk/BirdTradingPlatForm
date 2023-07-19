﻿using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.NEntity;

public partial class TbOrderDetail
{
    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public int? Discount { get; set; }

    public int Id { get; set; }

    public decimal? ProductPrice { get; set; }

    public decimal? DiscountPrice { get; set; }

    public decimal? Total { get; set; }

    public DateTime? DateOrder { get; set; }

    public int? ToConfirm { get; set; }
    public bool? ToFeedback { get; set; }

    public bool? RecievedStatus { get; set; }

    public virtual TbOrder Order { get; set; } = null!;

    public virtual TbProduct Product { get; set; } = null!;
}
