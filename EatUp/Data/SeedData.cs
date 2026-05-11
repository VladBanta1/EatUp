using EatUp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EatUp.Data;

public static class SeedData
{
    static string Img(string id, int w = 600) =>
        $"https://images.unsplash.com/photo-{id}?w={w}&auto=format&fit=crop";

    private static string OpenHours() => JsonSerializer.Serialize(new
    {
        Monday    = new { Open = "08:00", Close = "23:59" },
        Tuesday   = new { Open = "08:00", Close = "23:59" },
        Wednesday = new { Open = "08:00", Close = "23:59" },
        Thursday  = new { Open = "08:00", Close = "23:59" },
        Friday    = new { Open = "08:00", Close = "23:59" },
        Saturday  = new { Open = "08:00", Close = "23:59" },
        Sunday    = new { Open = "08:00", Close = "23:59" }
    });

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var hours = OpenHours();

        var stale = await db.Restaurants
            .Where(r => r.OpeningHoursJson != null && r.OpeningHoursJson.Contains("\"Open\":\"10:00\""))
            .ToListAsync();
        foreach (var r in stale) r.OpeningHoursJson = hours;
        if (stale.Any()) await db.SaveChangesAsync();

        if (await db.Restaurants.AnyAsync(r => r.TotalReviews > 10))
        {
            db.Reviews.RemoveRange(await db.Reviews.ToListAsync());
            await db.SaveChangesAsync();
            foreach (var r in await db.Restaurants.ToListAsync())
            { r.Rating = 0; r.TotalReviews = 0; }
            await db.SaveChangesAsync();
        }

        await EnsureAllRestaurantsAsync(db, hours);

        // ── Update restaurant/menu images to local file paths ──────────
        // must run after EnsureAllRestaurantsAsync, which overwrites Image/Logo/CoverImage with Unsplash URLs for existing records on every startup
        Console.WriteLine("[SeedData] Updating images to local paths...");
        await UpdateLocalImagePathsAsync(db);
        Console.WriteLine("[SeedData] Image update complete.");

        await TrySeedAllReviewsAsync(db);

        var zeroOrders = await db.Orders
            .Where(o => o.RestaurantOrderNumber == 0)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
        if (zeroOrders.Any())
        {
            var restIds = zeroOrders.Select(o => o.RestaurantId).Distinct().ToList();
            foreach (var restId in restIds)
            {
                var maxNum = await db.Orders
                    .Where(o => o.RestaurantId == restId && o.RestaurantOrderNumber > 0)
                    .Select(o => (int?)o.RestaurantOrderNumber)
                    .MaxAsync() ?? 0;
                int seq = maxNum;
                foreach (var ord in zeroOrders.Where(o => o.RestaurantId == restId).OrderBy(o => o.CreatedAt))
                    ord.RestaurantOrderNumber = ++seq;
            }
            await db.SaveChangesAsync();
        }

        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin)) return;

        db.Users.Add(new User
        {
            Name = "Administrator", Email = "admin@eatup.ro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
            Role = UserRole.Admin, Phone = "0700000000",
            CreatedAt = DateTime.UtcNow
        });

        db.Users.AddRange(
            new User { Name = "Andrei Ionescu",  Email = "andrei@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client@123"), Role = UserRole.Customer, Phone = "0711111111" },
            new User { Name = "Maria Popescu",   Email = "maria@example.com",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client@123"), Role = UserRole.Customer, Phone = "0722222222" },
            new User { Name = "Radu Constantin", Email = "radu@example.com",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client@123"), Role = UserRole.Customer, Phone = "0733333333" }
        );

        db.PromoCodes.AddRange(
            new PromoCode { Code = "WELCOME10", Description = "10% reducere pentru prima comandă",        DiscountType = DiscountType.Percentage, DiscountValue = 10, MinOrderAmount = 30,  MaxUses = 1000, UsedCount = 47,  ExpiresAt = DateTime.UtcNow.AddMonths(6), IsActive = true },
            new PromoCode { Code = "EATUP20",   Description = "20 RON reducere la comenzi peste 100 RON", DiscountType = DiscountType.Fixed,      DiscountValue = 20, MinOrderAmount = 100, MaxUses = 500,  UsedCount = 123, ExpiresAt = DateTime.UtcNow.AddMonths(3), IsActive = true },
            new PromoCode { Code = "SUMMER15",  Description = "15% reducere de vară",                     DiscountType = DiscountType.Percentage, DiscountValue = 15, MinOrderAmount = 50,  MaxUses = 2000, UsedCount = 389, ExpiresAt = DateTime.UtcNow.AddMonths(2), IsActive = true }
        );
        await db.SaveChangesAsync();

        await TrySeedAllReviewsAsync(db);
    }

    private static async Task EnsureAllRestaurantsAsync(ApplicationDbContext db, string hours)
    {
        await EnsureRestaurantAsync(db, hours,
            "lamama@eatup.ro", "La Mama Manager", "0745555555",
            "La Mama",
            "La Mama este cel mai iubit restaurant de bucătărie românească tradițională din București. Fiecare farfurie este gătită cu dragoste și ingrediente proaspete de la țară. Simte-te ca acasă, la masa bunicii.",
            "Strada Sfânta Vineri 8, București", 44.4268, 26.1025,
            "Românesc", "Românesc", "București", 10, 50, 40,
            Img("1569050467447-ce54b3bbc37d", 300),
            Img("1476718406336-4b801240bc7b", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Supe & Ciorbe", 0, new[] {
                    ("Ciorbă de Burtă",            "Burtă de vită, smântână, oțet, usturoi, ardei iute",                22m, Img("1569050467447-ce54b3bbc37d")),
                    ("Ciorbă de Perișoare",        "Perișoare de porc, legume, borș natural, leuștean",                 20m, Img("1569050467447-ce54b3bbc37d")),
                    ("Supă de Pui cu Fidea",       "Piept de pui, morcov, fidea, pătrunjel proaspăt",                  20m, Img("1569050467447-ce54b3bbc37d")),
                    ("Borș de Porc cu Tăiței",     "Ciolan de porc, tăiței de casă, rădăcinoase, borș acrit",          21m, Img("1569050467447-ce54b3bbc37d")),
                }),
                ("Feluri Principale", 1, new[] {
                    ("Sarmale cu Mămăligă",        "Sarmale în foi de varză murată, smântână și slănină afumată",      42m, Img("1476718406336-4b801240bc7b")),
                    ("Tochitura Moldovenească",    "Carne de porc, cârnați, ouă, mămăligă, brânză telemea",            46m, Img("1504544750208-6d2c0b0e37d1")),
                    ("Mușchi de Porc la Grătar",   "Mușchi de porc marinat cu usturoi și ierburi, garnitură legume",   48m, Img("1558030137-16cb3bc3c7ab")),
                    ("Friptură de Miel cu Usturoi","Pulpă de miel la cuptor cu mujdei, cartofi noi și rozmarin",       52m, Img("1558030137-16cb3bc3c7ab")),
                }),
                ("Salate & Garnituri", 2, new[] {
                    ("Mămăligă cu Brânză",         "Mămăligă cremă, brânză de burduf, smântână grasă",                20m, Img("1512621776951-a57141f2eefd")),
                    ("Salată de Sfeclă cu Hrean",  "Sfeclă roșie fiartă, hrean ras, ulei și oțet",                    18m, Img("1512621776951-a57141f2eefd")),
                    ("Murături Asortate",          "Castraveți, gogoșari, conopidă, morcov în saramură naturală",      16m, Img("1512621776951-a57141f2eefd")),
                }),
                ("Deserturi", 3, new[] {
                    ("Papanași cu Smântână",       "Papanași prăjiți, dulceață de vișine, smântână grasă",             24m, Img("1563729936-bc0771823ca3")),
                    ("Cozonac Tradițional",        "Felie generoasă de cozonac cu nucă și cacao, rețetă de casă",      16m, Img("1467003909585-2f8a72a4e6c7")),
                    ("Plăcintă cu Brânză",         "Plăcintă dobrogeană cu brânză sărată și ouă, coaptă la cuptor",   18m, Img("1476718406336-4b801240bc7b")),
                    ("Cremă de Zahăr Ars",         "Desert clasic românesc, cremă catifelată cu caramel",              20m, Img("1551024709-8f23befc4897")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "tokyo@eatup.ro", "Sushi Tokyo Manager", "0742222222",
            "Sushi Tokyo",
            "Sushi Tokyo aduce autenticitatea japoneză în inima cartierului Floreasca. Bucătarii noștri specializați pregătesc zilnic sushi din ingrediente proaspete importate din Japonia. O experiență culinară completă, de la ramen la sashimi premium.",
            "Strada Floreasca 15, București", 44.4634, 26.0946,
            "Sushi", "Sushi,Seafood", "București", 12, 60, 45,
            Img("1579584425555-c3ce17fd4351", 300),
            Img("1579584425555-c3ce17fd4351", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Rulouri & Nigiri", 0, new[] {
                    ("California Roll (8 buc)",    "Crab, avocado, castravete, icre tobiko, sos spicy mayo",           38m, Img("1579584425555-c3ce17fd4351")),
                    ("Salmon Nigiri (2 buc)",      "Orez sushi presat cu somon proaspăt Atlantic, wasabi",             22m, Img("1579584425555-c3ce17fd4351")),
                    ("Dragon Roll (8 buc)",        "Creveți tempura, avocado, somon, sos unagi glazat",                42m, Img("1579584425555-c3ce17fd4351")),
                    ("Tuna Maki (6 buc)",          "Ton roșu Atlantic, orez sushi, alge nori",                        30m, Img("1579584425555-c3ce17fd4351")),
                }),
                ("Ramen & Supe", 1, new[] {
                    ("Tonkotsu Ramen",             "Bulion de porc 18h, chashu, ou moale, nori, ceapă verde, bambus", 42m, Img("1569050467447-ce54b3bbc37d")),
                    ("Miso Ramen Vegetarian",      "Bulion miso, tofu satinat, ciuperci shiitake, bambus, edamame",   38m, Img("1569050467447-ce54b3bbc37d")),
                    ("Gyoza (6 buc)",              "Găluște prăjite cu porc și varză, sos ponzu cu ghimbir",          25m, Img("1569050467447-ce54b3bbc37d")),
                    ("Edamame cu Sare de Mare",    "Soia verde japoneză fiartă, sare de mare, servit cald",           18m, Img("1512621776951-a57141f2eefd")),
                }),
                ("Sashimi & Temaki", 2, new[] {
                    ("Salmon Sashimi (5 buc)",     "Somon proaspăt tăiat premium, wasabi, ghimbir murat roz",         45m, Img("1579584425555-c3ce17fd4351")),
                    ("Tuna Sashimi (5 buc)",       "Ton roșu selecție premium, sos soia, wasabi",                     48m, Img("1579584425555-c3ce17fd4351")),
                    ("Temaki Ton",                 "Con nori cu ton, avocado, castraveți, sos spicy",                 28m, Img("1579584425555-c3ce17fd4351")),
                    ("Temaki Creveți",             "Con nori cu creveți tempura, avocado, cream cheese",              30m, Img("1579584425555-c3ce17fd4351")),
                }),
                ("Deserturi Japoneze", 3, new[] {
                    ("Mochi Ice Cream Matcha",     "Mochi japonez umplut cu înghețată de ceai matcha premium",        22m, Img("1579584425555-c3ce17fd4351")),
                    ("Dorayaki cu Anko",           "Pâinică japoneză cu cremă de fasole azuki dulce",                 18m, Img("1561564823-be15b7a16d17")),
                    ("Cheesecake Japonez Pufos",   "Cheesecake sufleu cu brânză cream, ușor și aerat",               24m, Img("1533134242-0027ab0f73c2")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "napoli@eatup.ro", "Pizza Napoli Manager", "0741111111",
            "Pizza Napoli",
            "Pizza Napoli servește pizza autentică italiană coaptă în cuptor cu lemne adus din Napoli. Folosim ingrediente DOP și mozzarella fior di latte livrată săptămânal din Italia. Fiecare pizza este o călătorie culinară în inima Neapolului.",
            "Calea Victoriei 22, București", 44.4478, 26.0934,
            "Pizza", "Pizza", "București", 8, 40, 35,
            Img("1513104890138-7c749659a591", 300),
            Img("1513104890138-7c749659a591", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Pizza Clasică", 0, new[] {
                    ("Margherita",                 "Sos roșii San Marzano, mozzarella fior di latte, busuioc proaspăt",28m, Img("1513104890138-7c749659a591")),
                    ("Quattro Formaggi",           "Mozzarella, gorgonzola DOP, parmezan 24 luni, pecorino",          34m, Img("1513104890138-7c749659a591")),
                    ("Diavola",                    "Sos roșii, mozzarella, salam picant nduja, ardei iute calabres",  32m, Img("1513104890138-7c749659a591")),
                    ("Prosciutto e Funghi",        "Sos roșii, mozzarella, prosciutto cotto, ciuperci champignon",   35m, Img("1513104890138-7c749659a591")),
                }),
                ("Pizza Gourmet", 1, new[] {
                    ("Napoli cu Anșoa",            "Sos roșii, mozzarella, fileu de anșoa, capere, măsline Gaeta",   33m, Img("1513104890138-7c749659a591")),
                    ("Tartufo Nero",               "Cremă trufe negre, mozzarella, parmezan, rucola, trufe rase",    42m, Img("1513104890138-7c749659a591")),
                    ("Vegetariana",                "Sos roșii, mozzarella, ardei gras, zucchini, vinete, roșii cherry",30m, Img("1513104890138-7c749659a591")),
                    ("BBQ Chicken",                "Sos BBQ, pui la grătar, ceapă roșie, coriandru, mozzarella",    36m, Img("1513104890138-7c749659a591")),
                }),
                ("Paste & Risotto", 2, new[] {
                    ("Spaghetti Carbonara",        "Guanciale, gălbenuș, pecorino romano, piper negru proaspăt",     32m, Img("1513104890138-7c749659a591")),
                    ("Penne all'Arrabbiata",       "Sos roșii picant, usturoi, ardei iute, parmezan",               28m, Img("1513104890138-7c749659a591")),
                    ("Lasagna Bolognese",          "Carne de vită și porc, sos béchamel, parmezan gratinate",        34m, Img("1513104890138-7c749659a591")),
                }),
                ("Antipasti & Deserturi", 3, new[] {
                    ("Bruschetta cu Roșii",        "Pâine ciabatta, roșii cherry, busuioc, usturoi, ulei măsline",   18m, Img("1512621776951-a57141f2eefd")),
                    ("Tiramisu",                   "Desert clasic italian cu mascarpone, cafea și savoiardi",        20m, Img("1571877227200-a0d98ea607e9")),
                    ("Panna Cotta cu Fructe",      "Cremă de vanilie, coulis de căpșuni, fructe de pădure",          18m, Img("1551024709-8f23befc4897")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "dulceviata@eatup.ro", "Dulce Viență Manager", "0746666666",
            "Dulce Viență",
            "Dulce Viență este cafeneaua de specialitate din Pipera unde fiecare zi începe cu o cafea perfectă. Preparăm prăjituri artizanale și mic dejun sănătos cu ingrediente de sezon. Ambianță cozy, ideală pentru work-from-cafe sau întâlniri relaxate.",
            "Strada Pipera 10, București", 44.4876, 26.1101,
            "Cafenea", "Cafenea", "București", 7, 35, 25,
            Img("1509042239860-f550ce710b93", 300),
            Img("1509042239860-f550ce710b93", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Cafea & Ceai", 0, new[] {
                    ("Espresso Double",            "Cafea specialty single origin, extrasă la 9 bari, cremă persistentă",10m, Img("1509042239860-f550ce710b93")),
                    ("Cappuccino cu Lapte Spumat", "Espresso double, lapte integral spumat, latte art",                  14m, Img("1509042239860-f550ce710b93")),
                    ("Latte cu Vanilie",           "Espresso, lapte spumat, sirop de vanilie bourbon",                  16m, Img("1509042239860-f550ce710b93")),
                    ("Ceai de Fructe Roșii",       "Amestec premium de hibiscus, măcese, coacăze și mentă",             12m, Img("1509042239860-f550ce710b93")),
                }),
                ("Prăjituri & Torturi", 1, new[] {
                    ("Tort de Ciocolată Neagră",   "Pandișpan de cacao, ganache 70% ciocolată belgiană, fructe roșii", 24m, Img("1578985545062-70b2fd97b49")),
                    ("Cheesecake Fructe Pădure",   "Biscuit speculoos, cremă brânză, coulis de fructe de pădure",      22m, Img("1533134242-0027ab0f73c2")),
                    ("Eclair cu Cremă Vanilie",    "Eclair crocant, cremă diplomată cu vanilie Bourbon, glazură",      14m, Img("1535141192574-5d4897c12636")),
                    ("Macaron Asortate (4 buc)",   "Macaron franțuzești: ciocolată, zmeură, fistic, caramel sărat",    18m, Img("1509461399763-ae67a81ea3aa")),
                }),
                ("Mic Dejun", 2, new[] {
                    ("Croissant cu Unt și Gem",    "Croissant franțuzesc cu unt AOP, gem de caise artizanal",          14m, Img("1509042239860-f550ce710b93")),
                    ("Avocado Toast Sourdough",    "Pâine sourdough, avocado, ou pocat, semințe, fulgi chili",         28m, Img("1512621776951-a57141f2eefd")),
                    ("Pancakes cu Căpșuni",        "Pancakes americane, căpșuni proaspete, miere de albine, unt",      26m, Img("1509042239860-f550ce710b93")),
                    ("Granola cu Iaurt și Miere",  "Granola artizanală, iaurt grecesc, miere de salcâm, nuci",         20m, Img("1546069901-ba9599a7e63c")),
                }),
                ("Smoothie Bowls & Înghețate", 3, new[] {
                    ("Smoothie Bowl Tropical",     "Bază mango-ananas, granola, fructe proaspete, cocos ras",          28m, Img("1509042239860-f550ce710b93")),
                    ("Parfait cu Granola",         "Straturi de iaurt grec, granola crocantă, fructe de sezon",        22m, Img("1546069901-ba9599a7e63c")),
                    ("Înghețată Artizanală 3 Bile","Bile din lapte local: vanilie, ciocolată belgiană, zmeură",        20m, Img("1563805042-ef78c56ff5f4")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "tacoloco@eatup.ro", "Taco Loco Manager", "0747777777",
            "Taco Loco",
            "Taco Loco aduce autenticitatea mexicană din Ciudad de México direct în cartierul Militari. Folosim tortillas proaspete, carne marinată 24 de ore și salsa preparată zilnic din roșii coapte. Fiecare mușcătură este o fiesta de arome intense și autentice.",
            "Calea Giulești 44, București", 44.4289, 25.9876,
            "Mexican", "Mexican,Fast Food", "București", 9, 45, 30,
            Img("1568901346375-23c9450c58cd", 300),
            Img("1568901346375-23c9450c58cd", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Tacos", 0, new[] {
                    ("Tacos Pollo Picant (2 buc)", "Pui marinat în achiote, coriandru, ceapă albă, salsa verde",      28m, Img("1568901346375-23c9450c58cd")),
                    ("Tacos al Pastor (2 buc)",    "Carne de porc în chili guajillo, ananas, coriandru, ceapă",       30m, Img("1568901346375-23c9450c58cd")),
                    ("Tacos cu Vită (2 buc)",      "Carne de vită birria, mozzarella, sos consommé pentru dipping",   32m, Img("1568901346375-23c9450c58cd")),
                    ("Fish Tacos (2 buc)",         "Pește baja crocant, varză murată, sriracha mayo, lime",           30m, Img("1568901346375-23c9450c58cd")),
                }),
                ("Burritos & Quesadillas", 1, new[] {
                    ("Burrito Chicken Supreme",    "Pui, orez mexican, fasole neagră, guacamole, pico de gallo",      38m, Img("1568901346375-23c9450c58cd")),
                    ("Burrito cu Vită și Fasole",  "Carne de vită, fasole, orez, smântână, sos chipotle",             40m, Img("1568901346375-23c9450c58cd")),
                    ("Quesadilla cu Pui",          "Tortilla crocantă, pui, cheddar, jalapeño, sos roșu",            28m, Img("1568901346375-23c9450c58cd")),
                    ("Wrap Mexican Avocado",       "Avocado, legume colorate, fasole neagră, orez, sos tahini-lime",  32m, Img("1568901346375-23c9450c58cd")),
                }),
                ("Nachos & Aperitive", 2, new[] {
                    ("Nachos cu Brânză Jalapeño",  "Nachos crocante, nacho cheese, jalapeños, smântână, salsa",       24m, Img("1568901346375-23c9450c58cd")),
                    ("Nachos Supreme",             "Nachos, guacamole, pico de gallo, fasole, cheddar, jalapeños",   32m, Img("1568901346375-23c9450c58cd")),
                    ("Guacamole cu Tortilla Chips","Guacamole de casă cu avocado Hass, lime, coriandru, chips",       22m, Img("1512621776951-a57141f2eefd")),
                }),
                ("Deserturi", 3, new[] {
                    ("Churros cu Sos Ciocolată",   "Churros prăjite, zahăr cu scorțișoară, sos ciocolată mexicană",  18m, Img("1548583983-afbc1d8f4de7")),
                    ("Tres Leches Cake",           "Pandișpan îmbibat în trei tipuri de lapte, frișcă, scorțișoară", 22m, Img("1533134242-0027ab0f73c2")),
                    ("Empanadas cu Mere",          "Empanadas coapte cu mere, scorțișoară și zahăr brun",            16m, Img("1535141192574-5d4897c12636")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "marelezid@eatup.ro", "Marele Zid Manager", "0748888888",
            "Marele Zid",
            "Marele Zid este destinația pentru bucătăria chineză autentică din cartierul Titan. Cu un bucătar adus din Hong Kong, preparăm dim sum proaspăt, noodles wok-fried și specialități cantoneze unice. O experiență culinară care traversează Marele Zid direct pe masa ta.",
            "Bulevardul Basarabia 210, București", 44.4089, 26.1534,
            "Asian", "Asian", "București", 11, 55, 40,
            Img("1569050467447-ce54b3bbc37d", 300),
            Img("1569050467447-ce54b3bbc37d", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Dim Sum", 0, new[] {
                    ("Har Gao (4 buc)",            "Găluște cantoneze cu creveți în foaie translucidă aburită",       28m, Img("1569050467447-ce54b3bbc37d")),
                    ("Siu Mai cu Porc (4 buc)",    "Găluște aburite cu carne de porc și creveți, icre de somon",      26m, Img("1569050467447-ce54b3bbc37d")),
                    ("Char Siu Bao (3 buc)",       "Chiflă pufos cu porc BBQ lăcuit cantones, aburit",               22m, Img("1569050467447-ce54b3bbc37d")),
                    ("Cheung Fun cu Creveți",      "Rulouri de orez cu creveți, sos soia dulce și ulei de susan",     30m, Img("1569050467447-ce54b3bbc37d")),
                }),
                ("Noodles & Orez", 1, new[] {
                    ("Lo Mein Pui și Legume",      "Noodles de ou wok-fried cu pui, morcov, ceapă verde, sos oyster", 34m, Img("1569050467447-ce54b3bbc37d")),
                    ("Fried Rice Cantones",        "Orez jasmine prăjit în wok cu ou, ceapă verde, sos soia",        30m, Img("1569050467447-ce54b3bbc37d")),
                    ("Chow Mein cu Vită",          "Noodles crocante cu vită, bok choy, sos hoisin și sos stridii",  36m, Img("1569050467447-ce54b3bbc37d")),
                    ("Orez cu Ou și Ceapă Verde",  "Orez simplu wok-fried cu ou proaspăt și ceapă verde",            24m, Img("1569050467447-ce54b3bbc37d")),
                }),
                ("Specialități Wok", 2, new[] {
                    ("Pui în Sos Dulce-Picant",    "Piept de pui crocant, sos kung pao, ardei roșu, arahide",        38m, Img("1569050467447-ce54b3bbc37d")),
                    ("Tofu Mapo Picant",           "Tofu moale în sos Sichuan cu carne de porc, piper sichuan",      32m, Img("1569050467447-ce54b3bbc37d")),
                    ("Costițe BBQ Caramelizate",   "Costițe de porc lăcuite în sos hoisin-miere, grilate",           42m, Img("1615361200141-f45040f367be")),
                    ("Rată Lăcuită Beijing ½",     "Jumătate de rată lăcuită în stil Beijing, piele crocantă",       68m, Img("1569050467447-ce54b3bbc37d")),
                }),
                ("Supe", 3, new[] {
                    ("Supă Wonton cu Creveți",     "Wonton umplut cu creveți și porc în bulion clar, ceapă verde",   22m, Img("1569050467447-ce54b3bbc37d")),
                    ("Hot and Sour Soup",          "Supă acru-picantă cu tofu, ciuperci black fungus, bambus",       20m, Img("1569050467447-ce54b3bbc37d")),
                    ("Tom Yum cu Creveți",         "Supă thailandeză picantă cu creveți, ciuperci și lemongrass",   24m, Img("1569050467447-ce54b3bbc37d")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "carbon@eatup.ro", "Carbon & Flăcări Manager", "0749999999",
            "Carbon & Flăcări",
            "Carbon & Flăcări este templul cărnii la grătar din Drumul Taberei, gătită pe cărbune de fag pentru acel gust incomparabil. Folosim exclusiv carne premium de la ferme românești, selecționată zilnic. De la coaste glasate BBQ la antricot perfect, fiecare farfurie este o sărbătoare.",
            "Strada Drumul Taberei 80, București", 44.4123, 25.9987,
            "Grill", "Grill,Românesc", "București", 12, 60, 45,
            Img("1529193591184-b1d58069ecdd", 300),
            Img("1544025162-d76538fd2d82", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Coaste & Steaks", 0, new[] {
                    ("Coaste Porc BBQ 500g",       "Coaste glazate în sos BBQ de casă, fragede, fumate pe cărbune",  52m, Img("1615361200141-f45040f367be")),
                    ("Antricot de Vită 300g",      "Antricot Angus maturat 28 zile, cu sos de piper verde",          68m, Img("1558030137-16cb3bc3c7ab")),
                    ("T-Bone 400g",                "T-Bone de vită premium, cartofi copți și sos chimichurri",       75m, Img("1558030137-16cb3bc3c7ab")),
                    ("Piept Pui la Grătar",        "Piept de pui marinat în ierburi, cu sos de usturoi roz",         42m, Img("1558030137-16cb3bc3c7ab")),
                }),
                ("Mici & Specialități", 1, new[] {
                    ("Mici de Casă (5 buc)",       "Mici din carne de vită și porc cu condimente tradiționale",      30m, Img("1558030137-16cb3bc3c7ab")),
                    ("Cârnați de Casă la Grătar",  "Cârnați afumați din carne de porc cu cimbru și usturoi",         32m, Img("1558030137-16cb3bc3c7ab")),
                    ("Fleică Marinată cu Usturoi", "Fleică de porc marinată 12h în usturoi, ulei și ierburi aromate", 38m, Img("1558030137-16cb3bc3c7ab")),
                    ("Pulpă de Miel cu Rozmarin",  "Pulpă de miel la cărbune, marinată cu rozmarin și usturoi",      58m, Img("1558030137-16cb3bc3c7ab")),
                }),
                ("Garnituri", 2, new[] {
                    ("Cartofi Copți cu Smântână",  "Cartofi noi la cărbune, smântână, ceapă verde, brânză rasă",     18m, Img("1512621776951-a57141f2eefd")),
                    ("Salată Coleslaw",            "Varză, morcov, mayo de casă, muștar Dijon, semințe",             14m, Img("1512621776951-a57141f2eefd")),
                    ("Porumb la Grătar cu Unt",    "Porumb dulce la grătar direct pe cărbune, cu unt și sare",       12m, Img("1512621776951-a57141f2eefd")),
                    ("Fasole Bătută cu Ceapă",     "Fasole albă bătută cu usturoi, ulei, ceapă prăjită crocantă",    16m, Img("1512621776951-a57141f2eefd")),
                }),
                ("Sosuri & Băuturi", 3, new[] {
                    ("Sos BBQ de Casă",            "Sos BBQ smoky cu roșii, miere, bourbon și boia afumată",         8m,  Img("1512621776951-a57141f2eefd")),
                    ("Lemonadă de Casă cu Mentă",  "Lămâie proaspăt stoarsă, zahăr de trestie, mentă, apă carbogazoasă",14m, Img("1509042239860-f550ce710b93")),
                    ("Bere Artizanală 330ml",      "Bere blondă artizanală locală, nefiltrată, 5.2% alc.",           18m, Img("1509042239860-f550ce710b93")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "burgeria@eatup.ro", "Burgeria Manager", "0743333333",
            "Burgeria",
            "Burgeria este locul unde burgerii artizanali se ridică la rang de artă culinară în centrul Craiovei. Folosim exclusiv carne de vită Angus, pâine brioche coaptă zilnic și ingrediente locale de sezon. Fiecare burger spune o poveste de gust și pasiune.",
            "Calea Unirii 5, Craiova", 44.3302, 23.7949,
            "Fast Food", "Fast Food", "Craiova", 7, 35, 25,
            Img("1568901346375-23c9450c58cd", 300),
            Img("1568901346375-23c9450c58cd", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Burgeri Clasici", 0, new[] {
                    ("Classic Burger",             "Carne Angus 180g, cheddar, salată, roșii, ceapă, muștar clasic",  32m, Img("1568901346375-23c9450c58cd")),
                    ("Bacon Burger",               "Carne Angus, bacon crocant fumat, cheddar aged, sos BBQ",         38m, Img("1568901346375-23c9450c58cd")),
                    ("Veggie Burger",              "Patty de linte și quinoa, avocado, roșii cherry, sos harissa",    28m, Img("1568901346375-23c9450c58cd")),
                    ("Cheeseburger Dublu",         "2x carne Angus 100g, 2x cheddar, murături, sos special",         42m, Img("1568901346375-23c9450c58cd")),
                }),
                ("Burgeri Premium", 1, new[] {
                    ("Double Smash Burger",        "2x carne Angus smash 90g, caramelized onion, sos special house",  44m, Img("1568901346375-23c9450c58cd")),
                    ("Truffle Burger Wagyu",       "Carne Wagyu, mayo de trufe negre, rucola, parmezan 24 luni",      52m, Img("1568901346375-23c9450c58cd")),
                    ("Crispy Chicken Burger",      "Piept de pui crispy buttermilk, coleslaw, murături, sriracha mayo",35m, Img("1562802442796-b8a82e7e6a31")),
                }),
                ("Garnituri", 2, new[] {
                    ("Cartofi Prăjiți Clasici",    "Cartofi julienne crocanți cu sos aioli de casă",                  16m, Img("1568901346375-23c9450c58cd")),
                    ("Onion Rings în Crustă Bere", "Inele de ceapă dulce în crustă de bere artizanală",               18m, Img("1568901346375-23c9450c58cd")),
                    ("Coleslaw cu Muștar",         "Varză, morcov, mayo, muștar Dijon, semințe de fenicul",           12m, Img("1512621776951-a57141f2eefd")),
                    ("Sweet Potato Fries",         "Cartofi dulci prăjiți cu chimichurri și sare de Himalaya",        18m, Img("1568901346375-23c9450c58cd")),
                }),
                ("Deserturi & Shake-uri", 3, new[] {
                    ("Milkshake de Vanilie",       "Înghețată de vanilie artizanală, lapte integral, frișcă",         20m, Img("1509042239860-f550ce710b93")),
                    ("Milkshake de Ciocolată",     "Înghețată ciocolată belgiană, lapte, sirop de ciocolată",         20m, Img("1509042239860-f550ce710b93")),
                    ("Brownie cu Înghețată",       "Brownie cald cu ciocolată 70%, înghețată vanilie, caramel",       22m, Img("1564355808539-befd3c0d5777")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "salata@eatup.ro", "Salata Verde Manager", "0744444444",
            "Salata Verde",
            "Salata Verde este oaza de sănătate din Brazda lui Novac, dedicată celor care mănâncă conștient și delicios în același timp. Preparăm salate proaspete, boluri colorate și sucuri naturale din ingrediente organice locale. Mâncare bună pentru corp, minte și suflet.",
            "Strada Brazda lui Novac 12, Craiova", 44.3456, 23.8123,
            "Salate", "Salate,Vegan", "Craiova", 6, 30, 20,
            Img("1546069901-ba9599a7e63c", 300),
            Img("1512621776951-a57141f2eefd", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Salate", 0, new[] {
                    ("Caesar Salad",               "Romaine, croutons artizanali, parmezan ras, sos Caesar clasic",   28m, Img("1512621776951-a57141f2eefd")),
                    ("Greek Salad",                "Roșii cherry, castravete, feta PDO, măsline kalamata, oregano",   26m, Img("1512621776951-a57141f2eefd")),
                    ("Quinoa Bowl cu Avocado",     "Quinoa, avocado, năut crocant, spanac, vinegretă cu lămâie",      32m, Img("1546069901-ba9599a7e63c")),
                    ("Salată de Ton cu Ouă",       "Ton Atlantic, ouă de fermă, castraveți, roșii, porumb dulce",     30m, Img("1512621776951-a57141f2eefd")),
                }),
                ("Boluri Healthy", 1, new[] {
                    ("Poke Bowl cu Somon",         "Orez, somon marinat în soia, mango, edamame, avocado, ponzu",     42m, Img("1546069901-ba9599a7e63c")),
                    ("Buddha Bowl Vegan",          "Tofu baked, legume coapte, humus, tahini, semințe de sezon",      36m, Img("1546069901-ba9599a7e63c")),
                    ("Wrap cu Pui la Grill",       "Pui la grill, avocado, salată verde, roșii în tortilla integrală",30m, Img("1599487488170-d11ec9c172f0")),
                    ("Bowl Thai Unt Arahide",      "Orez, pui, salată, morcov, mango, sos satay cu unt arahide",      38m, Img("1546069901-ba9599a7e63c")),
                }),
                ("Sucuri & Smoothies", 2, new[] {
                    ("Green Detox cu Ghimbir",     "Spanac, măr verde, ghimbir proaspăt, lămâie, castraveți",         18m, Img("1512621776951-a57141f2eefd")),
                    ("Berry Smoothie",             "Căpșuni, afine, zmeură, iaurt natural, miere de salcâm",          20m, Img("1509042239860-f550ce710b93")),
                    ("Portocală Morcov Fresh",     "Portocale stoarse, morcov, ghimbir, turmeric, piper negru",       16m, Img("1509042239860-f550ce710b93")),
                    ("Smoothie Tropical",          "Mango, ananas, banană, lapte de cocos, semințe chia",             22m, Img("1509042239860-f550ce710b93")),
                }),
                ("Mic Dejun & Gustări", 3, new[] {
                    ("Overnight Oats cu Fructe",   "Fulgi ovăz, lapte migdale, fructe de pădure, semințe, miere",     22m, Img("1546069901-ba9599a7e63c")),
                    ("Granola Bowl",               "Granola artizanală, lapte de migdale, banană, afine, miere",       20m, Img("1546069901-ba9599a7e63c")),
                    ("Supă Cremă de Linte Roșie",  "Linte roșie, morcov, condimente indiene, iaurt, ulei chili",      24m, Img("1569050467447-ce54b3bbc37d")),
                    ("Hummus cu Legume Crude",     "Hummus de casă cu boia afumată, bastonașe legume colorate",        18m, Img("1546069901-ba9599a7e63c")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "shaorma@eatup.ro", "Shaorma Palace Manager", "0751111111",
            "Shaorma Palace",
            "Shaorma Palace este destinația supremă pentru shaorma și kebab autentic în cartierul Craiovița. Rețetele noastre turcești tradiționale, carnea marinată 24 de ore și sosurile secrete de casă fac din fiecare shaorma o experiență unică. Savurată de craioveni din 2015.",
            "Calea Severinului 33, Craiova", 44.3178, 23.7823,
            "Fast Food", "Fast Food", "Craiova", 5, 30, 20,
            Img("1599487488170-d11ec9c172f0", 300),
            Img("1568901346375-23c9450c58cd", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Shaorma", 0, new[] {
                    ("Shaorma Pui",                "Pui marinat 24h, salată, roșii, murături, sos alb de casă, lipie", 22m, Img("1568901346375-23c9450c58cd")),
                    ("Shaorma Vită",               "Vită marinată, cartofi, salată, roșii, sos alb și roșu, lipie",   24m, Img("1568901346375-23c9450c58cd")),
                    ("Shaorma Miel",               "Miel cu condimente orientale, varză, sos tahini, lipie caldă",     26m, Img("1568901346375-23c9450c58cd")),
                    ("Shaorma Mixtă",              "Carne de pui și vită, salată, roșii, ceapă, sosuri asortate",     26m, Img("1568901346375-23c9450c58cd")),
                }),
                ("Kebab", 1, new[] {
                    ("Kebab Pui",                  "Piept de pui condimentat, ceapă roșie, roșii, sos tzatziki",      24m, Img("1555939594-58d7cb561fd8")),
                    ("Kebab Vită",                 "Carne de vită maturată, ardei gras, ceapă, sos harissa",          26m, Img("1555939594-58d7cb561fd8")),
                    ("Dürüm Kebab",                "Carne la alegere, legume, sos garlic butter, înrolat strâns",     28m, Img("1568901346375-23c9450c58cd")),
                    ("Adana Kebab Picant",          "Carne de vită tocată cu ardei iute, condimente turcești, grătar", 28m, Img("1555939594-58d7cb561fd8")),
                }),
                ("Platouri & Specialități", 2, new[] {
                    ("Platou Shaorma pentru 2",    "2 shaorma la alegere, cartofi wedges, salată, băuturi răcoritoare",52m, Img("1599487488170-d11ec9c172f0")),
                    ("Falafel Plate",              "Falafel de năut crocant, humus, roșii, salată, pita caldă",       22m, Img("1546069901-ba9599a7e63c")),
                    ("Hummus cu Lipie Caldă",      "Hummus cremos cu boia afumată, ulei de măsline, lipie caldă",     18m, Img("1546069901-ba9599a7e63c")),
                }),
                ("Garnituri & Deserturi", 3, new[] {
                    ("Cartofi Wedges Condimentați","Wedges de cartofi cu ras el hanout, sos de iaurt cu usturoi",     14m, Img("1568901346375-23c9450c58cd")),
                    ("Salată Turcească cu Rodie",  "Roșii, castravete, ceapă roșie, rodie, măsline, pătrunjel",      16m, Img("1512621776951-a57141f2eefd")),
                    ("Baklava cu Fistic",          "Baklava turcească autentică cu straturi de fistic și miere",     18m, Img("1579954341869-28ec89c72e76")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "tajmahal@eatup.ro", "Taj Mahal Manager", "0752222222",
            "Taj Mahal",
            "Taj Mahal aduce aromele Indiei în Craiova, cu rețete autentice din Rajasthan și Punjab. Bucătarul nostru indian folosește condimente aduse direct din India pentru curry-uri, tikka masala și naan-uri desăvârșite. O experiență senzorială completă în fiecare comandă.",
            "Bulevardul 1 Mai 15, Craiova", 44.3389, 23.8234,
            "Indian", "Indian", "Craiova", 10, 50, 40,
            Img("1585937421096-f04b41d57e47", 300),
            Img("1455619452474-d73f7b5be5f3", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Curry", 0, new[] {
                    ("Butter Chicken",             "Piept de pui tandoor în sos de roșii, unt, smântână, garam masala",42m, Img("1585937421096-f04b41d57e47")),
                    ("Palak Paneer",               "Brânză indiană în sos de spanac cu usturoi, ghimbir, condimente", 36m, Img("1585937421096-f04b41d57e47")),
                    ("Tikka Masala de Pui",        "Pui marinat în iaurt, copt tandoor, în sos de roșii aromat",      44m, Img("1585937421096-f04b41d57e47")),
                    ("Dal Makhani",                "Linte neagră și fasole, gătite 12h cu unt, roșii, condimente",    34m, Img("1546069901-ba9599a7e63c")),
                }),
                ("Tandoor & Aperitive", 1, new[] {
                    ("Naan cu Unt (2 buc)",        "Pâine indiană coaptă în tandoor, unsă cu unt clarificat ghee",    14m, Img("1509042239860-f550ce710b93")),
                    ("Tandoori Chicken Half",      "Jumătate de pui marinat în iaurt și condimente, copt în tandoor",  48m, Img("1585937421096-f04b41d57e47")),
                    ("Seekh Kebab (4 buc)",        "Kebab de carne tocată cu ceapă, ardei verde și masala, grătar",   36m, Img("1555939594-58d7cb561fd8")),
                    ("Samosa cu Legume (2 buc)",   "Plăcintă triunghiulară crocantă cu cartofi, mazăre și condimente", 18m, Img("1606914501734-7c0be2e54bec")),
                }),
                ("Orez & Biryani", 2, new[] {
                    ("Chicken Biryani",            "Orez basmati cu pui marinat, condimente, șofran, mentă, ceapă",   46m, Img("1567188040759-fb8a883dc6d8")),
                    ("Jeera Rice",                 "Orez basmati cu semințe de chimion, unt ghee și coriandru",       18m, Img("1516684732757-565f9e88e2e6")),
                    ("Pilaf Vegetarian Basmati",   "Orez basmati cu legume, migdale prăjite, stafide, condimente",    22m, Img("1516684732757-565f9e88e2e6")),
                }),
                ("Deserturi & Băuturi", 3, new[] {
                    ("Gulab Jamun (3 buc)",        "Bile de lapte praf prăjite în sirop de zahăr cu cardamom",        16m, Img("1567620905732-2d1ec7ab7445")),
                    ("Mango Lassi",                "Iaurt indian, mango Alphonso, zahăr, cardamom, gheață",           18m, Img("1509042239860-f550ce710b93")),
                    ("Kheer cu Cardamom",          "Budincă de orez cu lapte integral, cardamom, șofran",             20m, Img("1551024709-8f23befc4897")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "pescarie@eatup.ro", "Pescărie La Dunăre Manager", "0753333333",
            "Pescărie La Dunăre",
            "Pescărie La Dunăre celebrează tradițiile culinare oltenești cu pește și fructe de mare proaspete aduse zilnic. Ciorbă de pește, doradă la grătar și platouri de fructe de mare după rețete vechi de generații. Marea la tine acasă, în Craiova!",
            "Strada Lăpuș 7, Craiova", 44.3234, 23.7756,
            "Seafood", "Seafood", "Craiova", 12, 60, 45,
            Img("1579584425555-c3ce17fd4351", 300),
            Img("1579584425555-c3ce17fd4351", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Pește la Grătar", 0, new[] {
                    ("Doradă cu Lămâie și Ierburi","Doradă 400g cu ierburi de Provence, lămâie, ulei de măsline",    48m, Img("1579584425555-c3ce17fd4351")),
                    ("Păstrăv cu Unt și Capere",   "Păstrăv de munte 350g, unt brun, capere, lămâie, pătrunjel",     42m, Img("1579584425555-c3ce17fd4351")),
                    ("Crap la Grătar cu Usturoi",  "Crap românesc 400g, mujdei de usturoi, rozmarin, cartofi noi",   38m, Img("1579584425555-c3ce17fd4351")),
                    ("Ton Grill cu Salată Mango",  "Ton blue fin 200g medium-rare, salată de mango, avocado, lime",  52m, Img("1579584425555-c3ce17fd4351")),
                }),
                ("Fructe de Mare", 1, new[] {
                    ("Creveți Garlic Butter 300g", "Creveți regali sotati în unt cu usturoi, lămâie, vin alb",       48m, Img("1579584425555-c3ce17fd4351")),
                    ("Calamari Crocante",          "Calamari în crustă de bere, prăjiți, cu sos aioli cu lămâie",    36m, Img("1579584425555-c3ce17fd4351")),
                    ("Midii Vin Alb și Usturoi",   "Midii deschise la abur cu vin alb, usturoi și pătrunjel",        44m, Img("1579584425555-c3ce17fd4351")),
                    ("Platou Seafood pentru 2",    "Doradă, creveți, calamari, midii, cartofi noi, sos tartare",     95m, Img("1579584425555-c3ce17fd4351")),
                }),
                ("Supe & Salate", 2, new[] {
                    ("Ciorbă de Pește Oltenească", "Crap, știucă, borș natural, legume de sezon, leuștean, smântână", 24m, Img("1569050467447-ce54b3bbc37d")),
                    ("Salată de Seafood Rucola",   "Creveți, calamar, rucola, roșii cherry, parmezan, lămâie",       32m, Img("1512621776951-a57141f2eefd")),
                    ("Bisque de Creveți",          "Supă cremă de creveți cu cognac, smântână și ierburi aromate",   28m, Img("1569050467447-ce54b3bbc37d")),
                }),
                ("Garnituri & Sosuri", 3, new[] {
                    ("Cartofi Noi cu Mărar",       "Cartofi noi fierți, unt, mărar proaspăt, sare de mare",          18m, Img("1512621776951-a57141f2eefd")),
                    ("Sos Tartare de Casă",        "Maioneză, castraveți murați, capere, estragon, lămâie",           8m, Img("1512621776951-a57141f2eefd")),
                    ("Salată Verde cu Lămâie",     "Amestec de salate, castraveți, vinegretă cu lămâie și miere",     14m, Img("1512621776951-a57141f2eefd")),
                }),
            });

        await EnsureRestaurantAsync(db, hours,
            "verdepur@eatup.ro", "Verde Pur Manager", "0754444444",
            "Verde Pur",
            "Verde Pur este primul restaurant 100% vegan din Craiova, dedicat celor care aleg un stil de viață conștient. Preparatele noastre dovedesc că mâncarea vegană poate fi și spectaculos de gustoasă, cu ingrediente organice și superfoods internaționale. Bun pentru tine și pentru planetă.",
            "Strada Nicolae Titulescu 28, Craiova", 44.3423, 23.8012,
            "Vegan", "Vegan", "Craiova", 8, 40, 30,
            Img("1546069901-ba9599a7e63c", 300),
            Img("1546069901-ba9599a7e63c", 1200),
            new (string, int, (string, string, decimal, string)[])[] {
                ("Burgeri & Sandwichuri", 0, new[] {
                    ("Burger Portobello",          "Ciupercă portobello grilată, avocado, roșii uscate, rucola, humus", 32m, Img("1546069901-ba9599a7e63c")),
                    ("Wrap cu Humus și Falafel",   "Falafel crocant, humus, legume, salată, sos tahini-lămâie",       28m, Img("1546069901-ba9599a7e63c")),
                    ("Sandwich Avocado Integral",  "Avocado zdrobit, roșii cherry, ridichi, germeni, pâine integrală", 26m, Img("1512621776951-a57141f2eefd")),
                }),
                ("Boluri & Salate", 1, new[] {
                    ("Buddha Bowl Vegan",          "Tofu baked, quinoa, năut crocant, legume coapte, tahini dressing", 36m, Img("1546069901-ba9599a7e63c")),
                    ("Poke Vegan cu Tofu",         "Orez, tofu marinat, mango, edamame, avocado, sos ponzu vegan",    38m, Img("1546069901-ba9599a7e63c")),
                    ("Salată Tabbouleh",           "Bulgur, roșii, castravete, ceapă verde, pătrunjel, lămâie",       24m, Img("1512621776951-a57141f2eefd")),
                    ("Quinoa Mediterranean",       "Quinoa, roșii uscate, măsline Kalamata, capere, balsamic",        34m, Img("1546069901-ba9599a7e63c")),
                }),
                ("Smoothies & Băuturi", 2, new[] {
                    ("Smoothie Verde Spirulina",   "Spanac, banană, ananas, spirulina, lapte de cocos, semințe chia", 20m, Img("1509042239860-f550ce710b93")),
                    ("Golden Milk Turmeric",       "Lapte de migdale cald, turmeric, scorțișoară, ghimbir, piper",   16m, Img("1509042239860-f550ce710b93")),
                    ("Detox Juice Sfeclă Ghimbir", "Sfeclă roșie, mere verzi, ghimbir proaspăt, lămâie, țelină",     18m, Img("1509042239860-f550ce710b93")),
                    ("Kombucha de Casă",           "Kombucha fermentată natural cu ghimbir și lime, 330ml",          14m, Img("1509042239860-f550ce710b93")),
                }),
                ("Deserturi Vegane", 3, new[] {
                    ("Raw Cheesecake cu Fructe",   "Bază de nuci-curmale, cremă caju cu fructe de pădure, raw",       28m, Img("1533134242-0027ab0f73c2")),
                    ("Brownies Vegani Ciocolată",  "Brownies cu ciocolată neagră 85%, curmale, nuci, ulei de cocos",  22m, Img("1564355808539-befd3c0d5777")),
                    ("Energy Balls (4 buc)",       "Bile energizante cu cacao, curmale, fulgi cocos și semințe",      18m, Img("1542838132-92c7cf754fef")),
                }),
            });
    }

    private static async Task EnsureRestaurantAsync(
        ApplicationDbContext db, string hours,
        string userEmail, string userName, string userPhone,
        string name, string description, string address,
        double lat, double lng, string category, string categories, string city,
        decimal deliveryFee, decimal minOrder, int estTime,
        string logo, string cover,
        (string CatName, int DisplayOrder, (string Name, string Desc, decimal Price, string Image)[] Items)[] menu)
    {
        var user = await GetOrCreateUserAsync(db, userEmail, userName, userPhone);

        var rest = await db.Restaurants.FirstOrDefaultAsync(r => r.Name == name);
        if (rest == null)
        {
            rest = new Restaurant
            {
                UserId = user.Id, Name = name, Description = description,
                Address = address, Lat = lat, Lng = lng,
                Category = category, Categories = categories, City = city,
                DeliveryFee = deliveryFee,
                MinOrderAmount = minOrder, EstimatedDeliveryTime = estTime,
                Logo = logo, CoverImage = cover,
                Rating = 0, TotalReviews = 0, IsApproved = true,
                OpeningHoursJson = hours
            };
            db.Restaurants.Add(rest);
            await db.SaveChangesAsync();
        }
        else
        {
            rest.Description           = description;
            rest.Address               = address;
            rest.Lat                   = lat;
            rest.Lng                   = lng;
            rest.Category              = category;
            rest.Categories            = categories;
            rest.City                  = city;
            rest.DeliveryFee           = deliveryFee;
            rest.MinOrderAmount        = minOrder;
            rest.EstimatedDeliveryTime = estTime;
            rest.OpeningHoursJson      = hours;
            rest.IsApproved            = true;
            rest.Logo                  = logo;
            rest.CoverImage            = cover;
            await db.SaveChangesAsync();
        }

        await EnsureMenuAsync(db, rest, menu);
    }

    private static async Task<User> GetOrCreateUserAsync(
        ApplicationDbContext db, string email, string name, string phone)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user != null) return user;

        user = new User
        {
            Name = name, Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Rest@1234"),
            Role = UserRole.Restaurant, Phone = phone,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task EnsureMenuAsync(
        ApplicationDbContext db, Restaurant restaurant,
        (string CatName, int DisplayOrder, (string Name, string Desc, decimal Price, string Image)[] Items)[] categories)
    {
        foreach (var (catName, displayOrder, items) in categories)
        {
            var cat = await db.MenuCategories
                .FirstOrDefaultAsync(c => c.RestaurantId == restaurant.Id && c.Name == catName);
            if (cat == null)
            {
                cat = new MenuCategory
                {
                    RestaurantId = restaurant.Id,
                    Name = catName,
                    DisplayOrder = displayOrder
                };
                db.MenuCategories.Add(cat);
                await db.SaveChangesAsync();
            }

            foreach (var (itemName, desc, price, image) in items)
            {
                var existing = await db.MenuItems.FirstOrDefaultAsync(
                    i => i.RestaurantId == restaurant.Id && i.Name == itemName);
                if (existing == null)
                {
                    db.MenuItems.Add(new MenuItem
                    {
                        RestaurantId = restaurant.Id, CategoryId = cat.Id,
                        Name = itemName, Description = desc, Price = price,
                        Image = image,
                        IsAvailable = true, IsApproved = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Image = image;
                }
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task UpdateLocalImagePathsAsync(ApplicationDbContext db)
    {
        var dirty = false;

        var restImages = new Dictionary<string, (string? Logo, string? Cover)>
        {
            ["Taj Mahal"]       = ("/img/restaurants/tajmahal.jpg", "/img/restaurants/tajmahal.jpg"),
            ["Carbon & Flăcări"]= (null,                            "/img/restaurants/carbon.jpg"),
            ["La Mama"]         = (null,                            "/img/restaurants/lamama.jpg"),
        };
        var restNames = restImages.Keys.ToList();
        var rests = await db.Restaurants.Where(r => restNames.Contains(r.Name)).ToListAsync();
        Console.WriteLine($"[SeedData] Found {rests.Count}/{restNames.Count} restaurants for image update.");
        foreach (var rest in rests)
        {
            if (!restImages.TryGetValue(rest.Name, out var imgs)) continue;
            if (imgs.Logo != null && rest.Logo != imgs.Logo)          { rest.Logo      = imgs.Logo;  dirty = true; }
            if (imgs.Cover != null && rest.CoverImage != imgs.Cover)  { rest.CoverImage = imgs.Cover; dirty = true; }
        }
        if (dirty) { await db.SaveChangesAsync(); dirty = false; }

        var menuImages = new Dictionary<string, string>
        {
            ["Sarmale cu Mămăligă"]        = "/img/menu/sarmale.jpg",
            ["Tochitura Moldovenească"]     = "/img/menu/toc.jpg",
            ["Mușchi de Porc la Grătar"]    = "/img/menu/muschi.jpg",
            ["Friptură de Miel cu Usturoi"] = "/img/menu/miel.jpg",
            ["Papanași cu Smântână"]        = "/img/menu/papanasi.jpg",
            ["Cozonac Tradițional"]         = "/img/menu/cozonac.jpg",
            ["Plăcintă cu Brânză"]          = "/img/menu/placinta.jpg",
            ["Cremă de Zahăr Ars"]          = "/img/menu/crema.jpg",
            ["Dorayaki cu Anko"]            = "/img/menu/dorayaki.jpg",
            ["Cheesecake Japonez Pufos"]    = "/img/menu/cheesecake.jpg",
            ["Napoli cu Anșoa"]             = "/img/menu/napoli.jpg",
            ["Panna Cotta cu Fructe"]       = "/img/menu/panna.jpg",
            ["Tort de Ciocolată Neagră"]    = "/img/menu/tortcioco.jpg",
            ["Cheesecake Fructe Pădure"]    = "/img/menu/cheesecakefructe.jpg",
            ["Macaron Asortate (4 buc)"]    = "/img/menu/macaron.jpg",
            ["Înghețată Artizanală 3 Bile"] = "/img/menu/inghetata.jpg",
            ["Churros cu Sos Ciocolată"]    = "/img/menu/churos.jpg",
            ["Tres Leches Cake"]            = "/img/menu/tres.jpg",
            ["Antricot de Vită 300g"]       = "/img/menu/antricot.jpg",
            ["T-Bone 400g"]                 = "/img/menu/tbone.jpg",
            ["Piept Pui la Grătar"]         = "/img/menu/piept.jpg",
            ["Mici de Casă (5 buc)"]        = "/img/menu/mici.jpg",
            ["Cârnați de Casă la Grătar"]   = "/img/menu/carnati.jpg",
            ["Fleică Marinată cu Usturoi"]  = "/img/menu/fleica.jpg",
            ["Pulpă de Miel cu Rozmarin"]   = "/img/menu/pulpa.jpg",
            ["Classic Burger"]              = "/img/menu/burger.jpg",
            ["Crispy Chicken Burger"]       = "/img/menu/crispy.jpg",
            ["Brownie cu Înghețată"]        = "/img/menu/brownieinghetata.jpg",
            ["Quinoa Bowl cu Avocado"]      = "/img/menu/quinoa.jpg",
            ["Wrap cu Pui la Grill"]        = "/img/menu/wrappui.jpg",
            ["Green Detox cu Ghimbir"]      = "/img/menu/green.jpg",
            ["Supă Cremă de Linte Roșie"]   = "/img/menu/supa.jpg",
            ["Hummus cu Legume Crude"]      = "/img/menu/legume.jpg",
            ["Kebab Pui"]                   = "/img/menu/kebabpui.jpg",
            ["Kebab Vită"]                  = "/img/menu/kebabvita.jpg",
            ["Adana Kebab Picant"]          = "/img/menu/adana.jpg",
            ["Baklava cu Fistic"]           = "/img/menu/baklava.jpg",
            ["Butter Chicken"]              = "/img/menu/butter.jpg",
            ["Palak Paneer"]                = "/img/menu/palak.jpg",
            ["Tikka Masala de Pui"]         = "/img/menu/tikka.jpg",
            ["Tandoori Chicken Half"]       = "/img/menu/tandori.jpg",
            ["Seekh Kebab (4 buc)"]         = "/img/menu/seekh.jpg",
            ["Samosa cu Legume (2 buc)"]    = "/img/menu/samosa.jpg",
            ["Jeera Rice"]                  = "/img/menu/jeera.jpg",
            ["Pilaf Vegetarian Basmati"]    = "/img/menu/pilaf.jpg",
            ["Kheer cu Cardamom"]           = "/img/menu/kheer.jpg",
            ["Raw Cheesecake cu Fructe"]    = "/img/menu/raw.jpg",
            ["Brownies Vegani Ciocolată"]   = "/img/menu/brownie.jpg",
            ["Energy Balls (4 buc)"]        = "/img/menu/energy.jpg",
        };

        var targetNames = menuImages.Keys.ToList();
        var items = await db.MenuItems.Where(i => targetNames.Contains(i.Name)).ToListAsync();
        Console.WriteLine($"[SeedData] Found {items.Count}/{targetNames.Count} menu items for image update.");
        foreach (var item in items)
        {
            if (menuImages.TryGetValue(item.Name, out var path) && item.Image != path)
            {
                item.Image = path;
                dirty = true;
            }
        }
        if (dirty) await db.SaveChangesAsync();
        Console.WriteLine($"[SeedData] Menu image save: dirty={dirty}");
    }

    private static async Task TrySeedAllReviewsAsync(ApplicationDbContext db)
    {
        var a = await db.Users.FirstOrDefaultAsync(u => u.Email == "andrei@example.com");
        var m = await db.Users.FirstOrDefaultAsync(u => u.Email == "maria@example.com");
        var r = await db.Users.FirstOrDefaultAsync(u => u.Email == "radu@example.com");
        if (a == null || m == null || r == null) return;
        await SeedAllReviewsAsync(db, a, m, r);
    }

    private static async Task SeedAllReviewsAsync(
        ApplicationDbContext db, User a, User m, User r)
    {
        var allDefs = new (string RestName, (User Cust, int Rating, string Comment)[])[]
        {
            ("La Mama", new[] {
                (a, 5, "Sarmalele cu mămăligă m-au dus cu gândul la bunica. Gust autentic, porție generoasă!"),
                (m, 5, "Ciorbă de burtă densă și gustoasă, papanașii la desert au fost vârful mesei. Nota 10!"),
                (r, 3, "Tochitura moldovenească puțin uscată față de așteptări, dar gustul de ansamblu bun."),
                (a, 4, "Mușchiul de porc la grătar a venit perfect gătit, cu legumele coapte delicios. Revin!"),
                (m, 4, "Borșul de porc cu tăiței — o surpriză plăcută. Atmosferă autentică românească!"),
            }),
            ("Sushi Tokyo", new[] {
                (m, 5, "California Roll-urile sunt proaspete și impecabile. Tonkotsu Ramen — o revelație!"),
                (a, 4, "Sashimi de somon excelent, porțiile generoase. Dragon Roll spectaculos. Recomand!"),
                (r, 5, "Cel mai bun sushi din București! Gyoza crocante, Temaki Ton plin de savoare. Vin des!"),
                (m, 4, "Miso Ramen Vegetarian savuros și hrănitor. Mochi Ice Cream la desert, delicios!"),
            }),
            ("Pizza Napoli", new[] {
                (a, 5, "Diavola picantă și crocantă, exact cum mă așteptam de la o pizza napolitană autentică!"),
                (m, 4, "Margherita cu mozzarella fior di latte — simplă, perfectă. Livrare în 30 de minute!"),
                (r, 3, "Quattro Formaggi puțin prea sărată pentru gustul meu, dar ingredientele sunt bune."),
                (a, 4, "Tartufo cu trufe a fost surpriza serii! Tiramisu-ul, fenomenal. Recomand cu drag."),
                (m, 5, "Spaghetti Carbonara autentice, Napoli cu anșoa divin. Cea mai bună pizza din Victoriei!"),
            }),
            ("Dulce Viență", new[] {
                (r, 5, "Tort de ciocolată neagră senzațional și un cappuccino perfect. Ambianță cozy, vii mereu!"),
                (a, 5, "Cheesecake-ul cu fructe de pădure a fost fenomenal. Pancakes cu căpșuni, la fel!"),
                (m, 4, "Smoothie Bowl Tropical a fost o explozie de culori și arome. Recomand cu drag!"),
                (r, 4, "Croissant proaspăt și un latte cu vanilie. Livrare rapidă, ambalaj îngrijit. Revin!"),
            }),
            ("Taco Loco", new[] {
                (m, 4, "Tacos al Pastor autentici, cu carne bine marinată și salsa proaspătă. Delicioși!"),
                (a, 5, "Burrito Supreme a fost o masă completă! Guacamole de casă, perfect cremos."),
                (r, 4, "Fish Tacos cu sriracha mayo — combinație câștigătoare. Nachos Supreme extrem de bun!"),
                (m, 3, "Quesadilla puțin prea unsuroasă, dar Churros cu ciocolată au compensat!"),
                (a, 5, "Cea mai bună mâncare mexicană din București! Porții generoase, totul proaspăt!"),
            }),
            ("Marele Zid", new[] {
                (r, 5, "Har Gao și Siu Mai la dim sum — revelator! Rată Lăcuită a meritat fiecare leu."),
                (m, 4, "Lo Mein cu pui plin de savoare, Fried Rice pregătit corect. Livrare rapidă!"),
                (a, 4, "Pui în sos dulce-picant clasic și gustos. Tofu Mapo, surprinzător de bun și picant!"),
                (r, 5, "Char Siu Bao pufos și suculent! Supa Wonton caldă și hrănitoare. Cel mai bun!"),
            }),
            ("Carbon & Flăcări", new[] {
                (a, 5, "Coaste de porc BBQ fraged ca untul! Sosul de casă face diferența. Numărul 1 din București!"),
                (m, 4, "Micii de casă au fost perfecți, aproape ca la bunici. Coleslaw — excelentă garnitură!"),
                (r, 5, "Antricotul de 300g a venit exact medium-rare cum am cerut. Experiență de top!"),
                (a, 4, "Fleică marinată — un clasic bine executat. Porumb la grătar ca bonus delicios."),
                (m, 4, "T-Bone impresionant, Pulpa de Miel a câștigat seara. Recomand tuturor carnorilor!"),
            }),
            ("Burgeria", new[] {
                (r, 5, "Double Smash este cel mai bun burger din Craiova! Carnea angus smash e perfectă."),
                (a, 4, "Bacon Burger bun, cartofii prăjiți sunt extraordinar de crocanți. Revin cu siguranță!"),
                (m, 3, "Classic Burger corect, nimic spectaculos, dar consistent și bine executat."),
                (r, 5, "Truffle Burger a fost o experiență de lux! Maioneza de trufe — senzațional."),
                (a, 4, "Crispy Chicken crocant și suculent. Onion Rings perfecte, Sweet Potato Fries delicioși!"),
            }),
            ("Salata Verde", new[] {
                (m, 5, "Poke Bowl cu somon a fost o revelație — proaspăt, echilibrat, plin de savoare!"),
                (r, 4, "Caesar Salad clasică, bine executată. Smoothie-ul Berry a fost perfect după birou."),
                (a, 4, "Buddha Bowl cu tofu copt — o surpriză plăcută. Green Detox, cel mai bun suc din Craiova!"),
                (m, 5, "Quinoa Bowl a devenit mâncarea mea zilnică. Wrap cu pui la grill, suculent și proaspăt!"),
            }),
            ("Shaorma Palace", new[] {
                (a, 5, "Shaorma de pui cu sos alb — cea mai bună din Craiova! Carne fragedă, legume crocante!"),
                (m, 5, "Dürüm Kebab a venit cald și bine împachetat. Shaorma Mixtă, porție supergenerosă!"),
                (r, 4, "Adana Kebab picant și suculent. Platoul Shaorma pentru 2 persoane, valoare excelentă!"),
                (a, 4, "Falafel Plate crocant și gustos. Hummus de casă cremos. Revin săptămânal!"),
                (m, 3, "Kebab Vită puțin uscat de data asta, dar Baklava cu fistic la desert a compensat."),
            }),
            ("Taj Mahal", new[] {
                (r, 5, "Butter Chicken cu Naan cu unt — combinație divină! Nu știam că există în Craiova!"),
                (m, 4, "Tikka Masala autentică, bine echilibrată ca picant. Biryani de pui aromat, generos!"),
                (a, 5, "Dal Makhani cremos și hrănitor. Gulab Jamun la desert — o explozie de dulceață!"),
                (r, 4, "Palak Paneer consistent și gustos. Samosa crocantă. Mango Lassi răcoritor perfect!"),
            }),
            ("Pescărie La Dunăre", new[] {
                (m, 4, "Doradă la grătar cu lămâie — proaspătă și bine pregătită. Ciorbă de pește excelentă!"),
                (a, 5, "Platoul Seafood a fost spectaculos! Creveți Garlic Butter, cei mai buni mâncați vreodată!"),
                (r, 4, "Calamari crocante, Midii în sos de vin alb senzaționale. Sos Tartare de casă, perfect!"),
                (m, 3, "Tonul la grătar puțin prea uscat, dar Bisque de Creveți a compensat excelent."),
                (a, 5, "Păstrăv cu lămâie copt perfect, Salata de Seafood proaspătă. Recomand cu drag!"),
            }),
            ("Verde Pur", new[] {
                (m, 5, "Burger Portobello suculent și savuros! Nu mi-a lipsit deloc carnea. Fenomenal!"),
                (r, 4, "Buddha Bowl vegan — o surpriză plăcută. Kombucha de casă, unicat în Craiova!"),
                (a, 5, "Raw Cheesecake mi-a schimbat definitiv percepția despre deserturile vegane. Magistral!"),
                (m, 4, "Poke Vegan colorat și proaspăt. Energy Balls perfecte ca gustare. Revin mereu!"),
            }),
        };

        var baseDate = DateTime.UtcNow.AddDays(-90);
        int dayOffset = 0;

        foreach (var (restName, reviews) in allDefs)
        {
            var rest = await db.Restaurants.FirstOrDefaultAsync(x => x.Name == restName);
            if (rest == null) { dayOffset += reviews.Length; continue; }

            if (await db.Reviews.AnyAsync(x => x.RestaurantId == rest.Id))
            { dayOffset += reviews.Length; continue; }

            foreach (var (cust, rating, comment) in reviews)
            {
                var orderDate = baseDate.AddDays(dayOffset++);
                int orderNum = await db.Orders.CountAsync(o => o.RestaurantId == rest.Id) + 1;
                var order = new Order
                {
                    CustomerId            = cust.Id,
                    RestaurantId          = rest.Id,
                    RestaurantOrderNumber = orderNum,
                    Status                = OrderStatus.Delivered,
                    ItemsJson             = "[]",
                    Subtotal              = 50m,
                    DeliveryFee           = rest.DeliveryFee,
                    Discount              = 0,
                    Total                 = 50m + rest.DeliveryFee,
                    DeliveryAddress       = "Adresă de test",
                    PaymentMethod         = PaymentMethod.Cash,
                    PaymentStatus         = PaymentStatus.Paid,
                    CreatedAt             = orderDate,
                    UpdatedAt             = orderDate
                };
                db.Orders.Add(order);
                await db.SaveChangesAsync();

                db.Reviews.Add(new Review
                {
                    CustomerId   = cust.Id,
                    RestaurantId = rest.Id,
                    OrderId      = order.Id,
                    Rating       = rating,
                    Comment      = comment,
                    CreatedAt    = orderDate.AddHours(2)
                });
                await db.SaveChangesAsync();
            }

            var rs = await db.Reviews.Where(x => x.RestaurantId == rest.Id).ToListAsync();
            rest.Rating       = rs.Count > 0 ? Math.Round((decimal)rs.Average(x => x.Rating), 2) : 0;
            rest.TotalReviews = rs.Count;
            await db.SaveChangesAsync();
        }
    }
}
