using AH.Data;
using AHInteriorsERP.Models;

namespace AHInteriorsERP.Data
{
    public class DbInitializer
    {
        public static void Initialize(AHInteriorsERPContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Check if DB has already been seeded
            if (context.Customers.Any())
            {
                return;
            }

            // ---------- Customers ----------
            var customers = new Customer[]
            {
                new Customer
                {
                    CustomerName = "John Smith",
                    Email = "john.smith@email.com",
                    Phone = "07123456789",
                    City = "London",
                    Postcode = "SW1A 1AA"
                },
                new Customer
                {
                    CustomerName = "Sarah Johnson",
                    Email = "sarah.j@email.com",
                    Phone = "07987654321",
                    City = "Manchester",
                    Postcode = "M1 1AE"
                }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            // ---------- Products ----------
            var products = new Product[]
            {
                new Product
                {
                    SKU = "CHAIR-001",
                    ProductName = "Oak Dining Chair",
                    Description = "Solid oak dining chair",
                    StockQuantity = 25,
                    BasePrice = 120.00m
                },
                new Product
                {
                    SKU = "TABLE-001",
                    ProductName = "Oak Dining Table",
                    Description = "Solid oak dining table",
                    StockQuantity = 10,
                    BasePrice = 850.00m
                },
                new Product
                {
                    SKU = "CHAIR-002",
                    ProductName = "Upholstered Dining Chair",
                    Description = "Fabric upholstered dining chair with oak legs",
                    StockQuantity = 30,
                    BasePrice = 150.00m
                },
                new Product
                {
                    SKU = "DESK-001",
                    ProductName = "Oak Writing Desk",
                    Description = "Solid oak writing desk with drawers",
                    StockQuantity = 8,
                    BasePrice = 695.00m
                },
                new Product
                {
                    SKU = "CABINET-001",
                    ProductName = "Oak Sideboard",
                    Description = "Solid oak sideboard with soft-close doors",
                    StockQuantity = 6,
                    BasePrice = 1_250.00m
                },
                new Product
                {
                    SKU = "SHELF-001",
                    ProductName = "Wall Mounted Shelf",
                    Description = "Oak wall-mounted display shelf",
                    StockQuantity = 40,
                    BasePrice = 95.00m
                },
                new Product
                {
                    SKU = "BED-001",
                    ProductName = "Oak Bed Frame",
                    Description = "King size solid oak bed frame",
                    StockQuantity = 5,
                    BasePrice = 1_495.00m
                },
                new Product
                {
                    SKU = "WARD-001",
                    ProductName = "Oak Wardrobe",
                    Description = "Double-door oak wardrobe with hanging rail",
                    StockQuantity = 4,
                    BasePrice = 1_895.00m
                },
                new Product
                {
                    SKU = "BENCH-001",
                    ProductName = "Oak Storage Bench",
                    Description = "Solid oak bench with integrated storage",
                    StockQuantity = 12,
                    BasePrice = 520.00m
                },
                new Product
                {
                    SKU = "STOOL-001",
                    ProductName = "Oak Bar Stool",
                    Description = "Solid oak bar stool with footrest",
                    StockQuantity = 20,
                    BasePrice = 180.00m
                },
                new Product
                {
                    SKU = "COFFEE-001",
                    ProductName = "Oak Coffee Table",
                    Description = "Solid oak coffee table with shelf",
                    StockQuantity = 15,
                    BasePrice = 650.00m
                },
                new Product
                {
                    SKU = "TVUNIT-001",
                    ProductName = "Oak TV Unit",
                    Description = "Solid oak TV unit with cable management",
                    StockQuantity = 7,
                    BasePrice = 995.00m
                }
            };


            context.Products.AddRange(products);
            context.SaveChanges();

            // ---------- Orders ----------
            var orders = new Order[]
            {
                new Order
                {
                    CustomerID = customers[0].CustomerID,
                    OrderDate = DateTime.UtcNow.AddDays(-3),
                    Status = "Completed"
                },
                new Order
                {
                    CustomerID = customers[1].CustomerID,
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    Status = "Pending"
                }
            };

            context.Orders.AddRange(orders);
            context.SaveChanges();

            // ---------- Order Items ----------
            var orderItems = new OrderItem[]
            {
                new OrderItem
                {
                    OrderID = orders[0].OrderID,
                    ProductID = products[0].ProductID,
                    Quantity = 4,
                    UnitPriceAtTime = 120.00m
                },
                new OrderItem
                {
                    OrderID = orders[0].OrderID,
                    ProductID = products[1].ProductID,
                    Quantity = 1,
                    UnitPriceAtTime = 850.00m
                },
                new OrderItem
                {
                    OrderID = orders[1].OrderID,
                    ProductID = products[0].ProductID,
                    Quantity = 2,
                    UnitPriceAtTime = 120.00m
                }
            };

            context.OrderItems.AddRange(orderItems);
            context.SaveChanges();

            // ---------- Invoice ----------
            var invoices = new Invoice[]
            {
                new Invoice
                {
                    OrderID = orders[0].OrderID,
                    InvoiceNumber = "INV-1001",
                    InvoiceDate = DateTime.UtcNow.AddDays(-2),
                    TotalAmount = 1330.00m,
                    PaymentStatus = "Paid"
                }
            };

            context.Invoices.AddRange(invoices);
            context.SaveChanges();
        }
    }
}

