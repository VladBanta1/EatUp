using EatUp.Data;
using EatUp.Hubs;
using EatUp.Models;
using EatUp.Services;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Security.Claims;
using System.Text.Json;

namespace EatUp.Controllers;

[Authorize(Policy = "CustomerOnly")]
public class CartController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHubContext<OrderHub> _hub;
    private readonly IEmailService _email;

    public CartController(ApplicationDbContext db, IConfiguration config, IHubContext<OrderHub> hub, IEmailService email)
    {
        _db = db;
        _config = config;
        _hub = hub;
        _email = email;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = GetCart();
        decimal minOrder = 0;
        double mapLat = 44.4268, mapLng = 26.1025;
        if (cart.RestaurantId > 0)
        {
            var restaurant = await _db.Restaurants.FindAsync(cart.RestaurantId);
            minOrder = restaurant?.MinOrderAmount ?? 0;
            if (restaurant?.Address?.Contains("Craiova") == true)
            { mapLat = 44.3302; mapLng = 23.7949; }
        }
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        var vm = new CartViewModel
        {
            Cart = cart,
            MinOrderAmount = minOrder,
            StripePublishableKey = _config["Stripe:PublishableKey"] ?? string.Empty,
            DefaultAddress = user?.Address ?? string.Empty,
            DefaultPhone = user?.Phone ?? string.Empty,
            MapCenterLat = mapLat,
            MapCenterLng = mapLng
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest req)
    {
        var menuItem = await _db.MenuItems.FindAsync(req.MenuItemId);
        if (menuItem == null || !menuItem.IsApproved || !menuItem.IsAvailable)
            return BadRequest(new { error = "Produsul nu este disponibil." });

        var restaurant = await _db.Restaurants.FindAsync(req.RestaurantId);
        if (restaurant == null)
            return BadRequest(new { error = "Restaurantul nu a fost găsit." });

        Cart cart = GetCart();

        if (cart.Items.Count > 0 && cart.RestaurantId != req.RestaurantId)
            return Json(new { differentRestaurant = true, currentRestaurantName = cart.RestaurantName });

        if (cart.Items.Count == 0)
        {
            cart.RestaurantId = restaurant.Id;
            cart.RestaurantName = restaurant.Name;
            cart.DeliveryFee = restaurant.DeliveryFee;
        }

        var existing = cart.Items.FirstOrDefault(i => i.MenuItemId == req.MenuItemId);
        if (existing != null)
            existing.Quantity += req.Quantity;
        else
            cart.Items.Add(new CartItem
            {
                MenuItemId = menuItem.Id,
                Name = menuItem.Name,
                Price = menuItem.Price,
                Quantity = req.Quantity
            });

        SaveCart(cart);
        HttpContext.Session.SetInt32("CartCount", cart.TotalItems);

        return Json(new
        {
            differentRestaurant = false,
            cartCount = cart.TotalItems,
            subtotal = cart.Subtotal
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove("Cart");
        HttpContext.Session.SetInt32("CartCount", 0);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove([FromBody] RemoveFromCartRequest req)
    {
        var cart = GetCart();
        cart.Items.RemoveAll(i => i.MenuItemId == req.MenuItemId);
        if (cart.Items.Count == 0)
        {
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.SetInt32("CartCount", 0);
        }
        else
        {
            SaveCart(cart);
            HttpContext.Session.SetInt32("CartCount", cart.TotalItems);
        }
        return Json(new { cartCount = cart.TotalItems, subtotal = cart.Subtotal });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateQuantity([FromBody] UpdateQuantityRequest req)
    {
        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(i => i.MenuItemId == req.MenuItemId);
        if (item == null) return BadRequest();

        if (req.Quantity <= 0)
            cart.Items.Remove(item);
        else
            item.Quantity = req.Quantity;

        if (cart.Items.Count == 0)
        {
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.SetInt32("CartCount", 0);
        }
        else
        {
            SaveCart(cart);
            HttpContext.Session.SetInt32("CartCount", cart.TotalItems);
        }
        return Json(new { cartCount = cart.TotalItems, subtotal = cart.Subtotal });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoRequest req)
    {
        var cart = GetCart();
        if (cart.Items.Count == 0)
            return Json(new { ok = false, error = "Coșul este gol." });

        var code = await _db.PromoCodes
            .FirstOrDefaultAsync(p => p.Code == req.Code.Trim().ToUpper() && p.IsActive);

        if (code == null)
            return Json(new { ok = false, error = "Cod promoțional invalid sau inactiv." });

        if (code.ExpiresAt.HasValue && code.ExpiresAt < DateTime.UtcNow)
            return Json(new { ok = false, error = "Codul promoțional a expirat." });

        if (code.MaxUses > 0 && code.UsedCount >= code.MaxUses)
            return Json(new { ok = false, error = "Codul promoțional a atins limita de utilizări." });

        if (cart.Subtotal < code.MinOrderAmount)
            return Json(new { ok = false, error = $"Comanda minimă pentru acest cod este {code.MinOrderAmount:0} RON." });

        decimal discount = code.DiscountType == DiscountType.Percentage
            ? Math.Round(cart.Subtotal * code.DiscountValue / 100, 2)
            : Math.Min(code.DiscountValue, cart.Subtotal);

        string description = code.Description
            ?? (code.DiscountType == DiscountType.Percentage
                ? $"{code.DiscountValue}% reducere"
                : $"{code.DiscountValue} RON reducere");

        return Json(new { ok = true, discount, promoCodeId = code.Id, description });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest req)
    {
        if (req.Amount <= 0)
            return BadRequest(new { error = "Sumă invalidă." });

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(req.Amount * 100),
            Currency = "ron",
            PaymentMethodTypes = new List<string> { "card" },
        };
        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);

        return Json(new { clientSecret = intent.ClientSecret });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req)
    {
        var cart = GetCart();
        if (cart.Items.Count == 0)
            return BadRequest(new { error = "Coșul este gol." });

        if (string.IsNullOrWhiteSpace(req.DeliveryAddress))
            return BadRequest(new { error = "Adresa de livrare este obligatorie." });

        var restaurantForOrder = await _db.Restaurants.FindAsync(cart.RestaurantId);
        if (restaurantForOrder != null && cart.Subtotal < restaurantForOrder.MinOrderAmount)
            return BadRequest(new { error = $"Comanda minimă la acest restaurant este {restaurantForOrder.MinOrderAmount:0} RON." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (req.PaymentMethod == "Card")
        {
            if (string.IsNullOrEmpty(req.PaymentIntentId))
                return BadRequest(new { error = "Plata cu cardul nu a fost finalizată." });

            var piService = new PaymentIntentService();
            var pi = await piService.GetAsync(req.PaymentIntentId);
            if (pi.Status != "succeeded")
                return BadRequest(new { error = "Plata nu a fost confirmată de Stripe." });
        }

        decimal discount = 0;
        Models.PromoCode? promo = null;
        if (req.PromoCodeId.HasValue)
        {
            promo = await _db.PromoCodes.FindAsync(req.PromoCodeId.Value);
            if (promo != null && promo.IsActive)
            {
                discount = promo.DiscountType == DiscountType.Percentage
                    ? Math.Round(cart.Subtotal * promo.DiscountValue / 100, 2)
                    : Math.Min(promo.DiscountValue, cart.Subtotal);
            }
        }

        var total = cart.Subtotal + cart.DeliveryFee - discount;

        int restaurantOrderNumber = await _db.Orders.CountAsync(o => o.RestaurantId == cart.RestaurantId) + 1;

        var order = new Order
        {
            CustomerId = userId,
            RestaurantId = cart.RestaurantId,
            RestaurantOrderNumber = restaurantOrderNumber,
            Status = OrderStatus.Pending,
            ItemsJson = JsonSerializer.Serialize(cart.Items),
            Subtotal = cart.Subtotal,
            DeliveryFee = cart.DeliveryFee,
            Discount = discount,
            Total = total,
            PaymentMethod = req.PaymentMethod == "Card" ? Models.PaymentMethod.Card : Models.PaymentMethod.Cash,
            PaymentStatus = req.PaymentMethod == "Card" ? PaymentStatus.Paid : PaymentStatus.Pending,
            StripePaymentIntentId = req.PaymentIntentId,
            DeliveryAddress = req.DeliveryAddress,
            DeliveryBlock = string.IsNullOrWhiteSpace(req.DeliveryBlock) ? null : req.DeliveryBlock.Trim(),
            DeliveryStaircase = string.IsNullOrWhiteSpace(req.DeliveryStaircase) ? null : req.DeliveryStaircase.Trim(),
            DeliveryApartment = string.IsNullOrWhiteSpace(req.DeliveryApartment) ? null : req.DeliveryApartment.Trim(),
            DeliveryLat = req.DeliveryLat,
            DeliveryLng = req.DeliveryLng,
            PromoCodeId = promo?.Id,
            PhoneNumber = req.PhoneNumber,
            DeliveryComment = req.DeliveryComment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var item in cart.Items)
        {
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                MenuItemId = item.MenuItemId,
                NameSnapshot = item.Name,
                PriceSnapshot = item.Price,
                Quantity = item.Quantity,
            });
        }

        if (promo != null)
            promo.UsedCount++;

        await _db.SaveChangesAsync();

        HttpContext.Session.Remove("Cart");
        HttpContext.Session.SetInt32("CartCount", 0);

        var customer = await _db.Users.FindAsync(userId);
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        await _hub.Clients.Group($"restaurant-{cart.RestaurantId}")
            .SendAsync("order_incoming", new
            {
                id = order.Id,
                restaurantOrderNumber = order.RestaurantOrderNumber,
                customerName = customer?.Name ?? "Client",
                items = cart.Items.Select(i => new
                {
                    name = i.Name,
                    qty = i.Quantity,
                    price = i.Price.ToString("0.00", ic)
                }).ToList(),
                total = order.Total.ToString("0.00", ic),
                paymentMethod = req.PaymentMethod,
                deliveryAddress = req.DeliveryAddress,
                deliveryBlock = req.DeliveryBlock,
                deliveryStaircase = req.DeliveryStaircase,
                deliveryApartment = req.DeliveryApartment,
                phoneNumber = req.PhoneNumber,
                deliveryComment = req.DeliveryComment,
                createdAt = order.CreatedAt.ToLocalTime().ToString("HH:mm")
            });

        var restaurant = await _db.Restaurants.FindAsync(cart.RestaurantId);
        var itemsSummary = string.Join("<br>", cart.Items
            .Select(i => $"{i.Name} × {i.Quantity} &mdash; {(i.Price * i.Quantity):0.00} RON"));
        _email.SendOrderPlaced(customer!.Email, customer.Name, order.Id,
            cart.RestaurantName, itemsSummary, order.Total,
            restaurant?.EstimatedDeliveryTime ?? 30);

        return Json(new { orderId = order.Id });
    }

    private Cart GetCart()
    {
        var json = HttpContext.Session.GetString("Cart");
        return string.IsNullOrEmpty(json)
            ? new Cart()
            : JsonSerializer.Deserialize<Cart>(json) ?? new Cart();
    }

    private void SaveCart(Cart cart)
        => HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
}

public record AddToCartRequest(int MenuItemId, int RestaurantId, int Quantity = 1);
public record RemoveFromCartRequest(int MenuItemId);
public record UpdateQuantityRequest(int MenuItemId, int Quantity);
public record ApplyPromoRequest(string Code, decimal Subtotal);
public record CreatePaymentIntentRequest(decimal Amount);
public record PlaceOrderRequest(
    string DeliveryAddress,
    string PaymentMethod,
    double? DeliveryLat,
    double? DeliveryLng,
    string? PaymentIntentId,
    int? PromoCodeId,
    string? PhoneNumber,
    string? DeliveryComment,
    string? DeliveryBlock,
    string? DeliveryStaircase,
    string? DeliveryApartment);
