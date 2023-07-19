﻿using BirdPlatFormEcommerce.NEntity;
using BirdPlatFormEcommerce.Helper.Mail;
using BirdPlatFormEcommerce.Order.Requests;
using BirdPlatFormEcommerce.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using BirdPlatFormEcommerce.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;

namespace BirdPlatFormEcommerce.Order
{
    public class OrderService : IOrderService
    {
        private readonly SwpDataBaseContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IMailService _mailService;

        public OrderService(SwpDataBaseContext context, IVnPayService vnPayService, IMailService mailService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _mailService = mailService;
        }
        public async Task<List<TbOrder>> CompleteOrder(List<int> order)
        {
            // Update product quantity
            var orders = await GetOrders(order);

            foreach (var orderDetail in orders)
            {
                var orderDetails = orderDetail.TbOrderDetails;
                foreach (var item in orderDetails)
                {
                    var product = _context.TbProducts.FirstOrDefault(prod => prod.ProductId == item.ProductId);
                    if (product == null)
                    {
                        throw new Exception("Product not found");
                    }
                    if (product.Quantity < item.Quantity)
                    {
                        throw new Exception($"Product {product.ProductId} is out of stock");
                    }

                    /*product.Quantity -= item.Quantity;
                    product.QuantitySold += item.Quantity;*/

                    _context.TbProducts.Update(product);

                }
                orderDetail.ToConfirm = 2;
                orderDetail.Status = true;
                _context.TbOrders.Update(orderDetail);
            }

            string listProductHtml = "";
            decimal total = 0;
            var processedOrderIds = new List<int>();
            foreach (var order1 in orders)
            {
                if (!processedOrderIds.Contains(order1.OrderId))
                {
                    total = (decimal)(total + order1.TotalPrice);
                    processedOrderIds.Add(order1.OrderId);
                    foreach (TbOrderDetail item in order1.TbOrderDetails)
                    {
                        listProductHtml += $"<li>{item.Product?.Name} - <del>{item.ProductPrice:n0}</del> $ {item.DiscountPrice:n0} $ - x{item.Quantity}</li><br>";

                    }
                }
                if (processedOrderIds.Count == orders.Count)
                {
                    var toEmail = order1.User?.Email ?? string.Empty;
                    var fullName = order1.Address?.NameRg ?? string.Empty;
                    var toPhone = order1.Address?.Phone ?? string.Empty;
                    var address = order1.Address?.Address ?? string.Empty;
                    var addressDetail = order1.Address?.AddressDetail ?? string.Empty;
                    var emailBody = $@"<div><h3>THÔNG TIN ĐƠN HÀNG CỦA BẠN </h3> 
                        <div>
                            <h3>Thông tin nhận hàng</h3> 
                              <span>Tên người nhận: </span> <strong>{fullName}</strong><br>
                            <span>Số Điện thoại: </span> <strong>{toPhone:n0}</strong><br>
                            <span>Địa Chỉ Nhận hàng: </span> <strong>{addressDetail}, {address}</strong>
                        </div>
                        <ul>{listProductHtml} </ul>
                        <div>
                            <span>Tổng tiền: </span> <strong>{total:n0} VND</strong>
                        </div>
                           
                        <p>Xin trân trọng cảm ơn</p>
                    </div>";
                    var mailRequest = new MailRequest()
                    {
                        ToEmail = toEmail,
                        Subject = "[BIRD TRADING PLATFORM] THANH TOÁN THÀNH CÔNG",
                        Body = emailBody
                    };


                    await _mailService.SendEmailAsync(mailRequest);
                    _context.SaveChanges();

                }

            }





            return orders;



        }
        public async Task<List<TbOrder>> CreateOrder(int userId, CreateOrderModel orderModel)
        {
            // validate user
            var user = await _context.TbUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var orders = new List<TbOrder>(); // Lưu trữ danh sách các đơn hàng đã tạo

            // Tạo đơn hàng cho từng sản phẩm
            foreach (var requestItem in orderModel.Items)
            {
                var quantity = requestItem.Quantity;
                var productId = requestItem.ProductId;

                var product = await _context.TbProducts.FirstOrDefaultAsync(p => p.ProductId == productId);
                

                if (product == null)
                {
                    throw new Exception($"Product {productId} not found");
                }

                if (product.Quantity < quantity)
                {
                    throw new Exception($"Product {productId} is out of stock");
                }
                product.Quantity -= quantity;
                _context.TbProducts.Update(product);
                await _context.SaveChangesAsync();

                // Kiểm tra xem đã có đơn hàng cho shop tương ứng chưa
                var existingOrder = orders.FirstOrDefault(o => o.ShopId == product.ShopId);

                if (existingOrder != null)
                {
                    // Nếu đã có đơn hàng cho shop này, thêm sản phẩm vào đơn hàng đã có
                    var orderItem = CreateOrderItem(product, quantity);
                    existingOrder.TbOrderDetails.Add(orderItem);
                    existingOrder.TotalPrice += orderItem.Total ?? 0;
                }
                else
                {
                    // Nếu chưa có đơn hàng cho shop này, tạo đơn hàng mới
                    var orderItem = CreateOrderItem(product, quantity);
                    var order = CreateNewOrder(userId, orderModel, orderItem, product.ShopId);
                    orders.Add(order);
                }
            }


            // Lưu các đơn hàng vào cơ sở dữ liệu
            foreach (var order in orders)
            {
                await _context.TbOrders.AddAsync(order);
            }

            await _context.SaveChangesAsync();

            return orders;
        }
        private TbOrderDetail CreateOrderItem(TbProduct product, int quantity)
        {
            var orderItem = new TbOrderDetail
            {
                ProductId = product.ProductId,
                Quantity = quantity,
                ProductPrice = product.Price,
                DiscountPrice = (1 - product.DiscountPercent * (decimal)0.01) * product.Price,
                ToConfirm = 1,

            };

            orderItem.Total = orderItem.DiscountPrice * quantity;

            return orderItem;
        }
        private TbOrder CreateNewOrder(int userId, CreateOrderModel orderModel, TbOrderDetail orderItem, int? shopId)
        {
            var order = new TbOrder()
            {
                UserId = userId,
                Status = false,
                OrderDate = DateTime.Now,
                Note = orderModel.Note,
                AddressId = (int)orderModel.AddressID,
                TbOrderDetails = new List<TbOrderDetail> { orderItem },
                TotalPrice = orderItem.Total ?? 0,
                ShopId = (int)shopId
            };

            return order;
        }
        public async Task<TbOrder?> GetOrder(int orderId)
        {
            return await _context.TbOrders
                .Include(order => order.TbOrderDetails)
                .ThenInclude(orderItem => orderItem.Product)
                .Include(order => order.Payment)
                .Include(order => order.User)
                .FirstOrDefaultAsync(order => order.OrderId == orderId);
        }
        public async Task<List<TbOrder>> GetOrders(List<int> orderIds)
        {
            var orders = await _context.TbOrders
                .Include(order => order.TbOrderDetails)
                 .ThenInclude(orderItem => orderItem.Product)
                .Include(order => order.Payment)
                .Include(order => order.User)
                 .Include(order => order.Address)
                .Where(order => orderIds.Contains(order.OrderId))
                .ToListAsync();

            // Kiểm tra xem có bất kỳ đơn hàng nào là NULL không
            if (orders.Any(order => order == null))
            {
                throw new Exception("One or more orders not found");
            }

            return orders;
        }
        public async Task<string?> PayOrders(List<int> orderIds, PaymentMethod method)
        {
            var orders = await GetOrders(orderIds);
            if (orders == null || orders.Any(o => o.Status == true))
            {
                throw new Exception("Order(s) not found or invalid status");
            }
            decimal total = 0;
            foreach (var order in orders)
            {
                total = (decimal)(total + order.TotalPrice);
            }


            var payment = new TbPayment()
            {
                UserId = orders.First().UserId, // Lấy UserId từ Order đầu tiên (giả sử cùng User)
                PaymentMethod = method.ToString(),
                PaymentDate = DateTime.Now,
                Amount = total // Sử dụng tổng TotalPrice làm Amount
            };
            var paymentId = _context.TbPayments.Update(payment);
            await _context.SaveChangesAsync();
            // Cập nhật PaymentId vào cơ sở dữ liệu

            foreach (var order in orders)
            {

                order.PaymentId = payment.PaymentId; // Gán PaymentId cho mỗi Order
                await _context.SaveChangesAsync();
            }


            // Cập nhật PaymentId vào cơ sở dữ liệu

            var paymentUrl = PaymentMethod.VnPay.Equals(method)
                ? _vnPayService.CreatePaymentUrl(payment)
                : null;

            return paymentUrl;
        }
        public async Task<List<TbOrder>> GetOrderByPaymentId(int paymentId)
        {
            return await _context.TbOrders
              .Where(order => order.PaymentId == paymentId)
              .ToListAsync();
        }

       

        public async Task<TbOrder> UpdateOrder(TbOrder order)
        {
            _context.TbOrders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }
        public async Task<List<TbOrder>> GetConfirmedOrdersByUser(int userId, int toConfirm)
        {

            return await _context.TbOrders
         .Include(order => order.TbOrderDetails)
             .ThenInclude(orderItem => orderItem.Product)
             .ThenInclude(product => product.Shop) // Nạp thông tin bảng Shop vào Product
         .Include(order => order.Payment)
         .Include(order => order.User)
         .Include(addr =>  addr.Address)
         .Where(order => order.UserId == userId && order.TbOrderDetails.Any(item => item.ToConfirm == toConfirm))
         .ToListAsync();
        }

        public Task<List<TbOrderDetail>> Orderservice(int shopid)
        {
            var orders = _context.TbOrderDetails.                         
                Where(o => o.ToConfirm == 2)
                .Select(od => new
                {
                    TbOrderDetail = od,
                    ShopId = _context.TbProducts
                    .Where(p => p.ProductId == od.ProductId)
                    .Select(p => p.ShopId).FirstOrDefault()
                }) 
                .Where(shop => shop.ShopId == shopid)
                .Select(shop => shop.TbOrderDetail).ToListAsync();
            return orders;
        }

        public async Task<List<TbOrder>> GetConfirmedOrdersByShop(int userId, int shopId)
        {
            return await _context.TbOrders
                .Include(order => order.TbOrderDetails)
                    .ThenInclude(orderItem => orderItem.Product)
                    .ThenInclude(product => product.Shop)
                .Include(order => order.Payment)
                .Include(order => order.User)
                .Include(addr => addr.Address)
                .Where(order => order.User.UserId == userId && order.TbOrderDetails.Any(item => item.ToConfirm == 2 && item.Product.Shop.ShopId == shopId))
                .ToListAsync();
        }

    }
}
