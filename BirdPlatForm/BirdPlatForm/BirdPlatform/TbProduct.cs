using System;
using System.Collections.Generic;

namespace BirdPlatForm.BirdPlatform;

public partial class TbProduct
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public bool? Status { get; set; }

    public decimal Price { get; set; }

    public string? Decription { get; set; }

    public string? Detail { get; set; }

    public int? QuantitySold { get; set; }

    public string CateId { get; set; } = null!;

    public int? ShopId { get; set; }

    public int? Rate { get; set; }

    public string? Video { get; set; }

    public string? Image { get; set; }

    public string? Image1 { get; set; }

    public string? Image2 { get; set; }

    public string? Image3 { get; set; }

    public string? Image4 { get; set; }

    public int? DiscountId { get; set; }

    public virtual TbProductCategory Cate { get; set; } = null!;

    public virtual TbShop? Shop { get; set; }

    public virtual ICollection<TbCart> TbCarts { get; set; } = new List<TbCart>();

    public virtual TbDiscount? TbDiscount { get; set; }

    public virtual ICollection<TbFeedback> TbFeedbacks { get; set; } = new List<TbFeedback>();

    public virtual ICollection<TbOrderDetail> TbOrderDetails { get; set; } = new List<TbOrderDetail>();

    public virtual ICollection<TbPost> TbPosts { get; set; } = new List<TbPost>();

    public virtual ICollection<TbWishList> TbWishLists { get; set; } = new List<TbWishList>();
}
