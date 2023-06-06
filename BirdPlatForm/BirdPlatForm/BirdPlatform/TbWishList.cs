﻿using System;
using System.Collections.Generic;

namespace BirdPlatForm.BirdPlatform;

public partial class TbWishList
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public virtual TbProduct Product { get; set; } = null!;

    public virtual TbUser User { get; set; } = null!;
}
