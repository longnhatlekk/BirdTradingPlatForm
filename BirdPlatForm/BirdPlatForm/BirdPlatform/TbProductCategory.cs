﻿using System;
using System.Collections.Generic;

namespace BirdPlatForm.BirdPlatform;

public partial class TbProductCategory
{
    public string CateId { get; set; } = null!;

    public string? CateName { get; set; }

    public virtual ICollection<TbProduct> TbProducts { get; set; } = new List<TbProduct>();
}
