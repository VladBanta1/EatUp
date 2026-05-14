# EatUp — Documentație completă a proiectului
### Proiect licență | ASP.NET Core MVC + MySQL | Universitatea din Craiova

> Platformă de comandă mâncare online inspirată din Wolt / Glovo / Bolt Food.
> Trei roluri de utilizator: Customer, Restaurant, Admin.

---

## Tech Stack

| Layer | Tehnologie |
|---|---|
| Framework | ASP.NET Core MVC (.NET 10), C# |
| Views | Razor Pages + Bootstrap 5 |
| Database | MySQL |
| ORM | Entity Framework Core (Pomelo provider) |
| Auth | Cookie Authentication (custom, nu Identity) |
| Password hashing | BCrypt.Net-Next |
| Real-time | SignalR (`/hubs/orders`) |
| Payments | Stripe.net (PaymentIntent flow) |
| Email | MailKit (Gmail SMTP, fire-and-forget) |
| Maps | Leaflet.js + OpenStreetMap (Nominatim geocoding) |
| File uploads | IFormFile → `/wwwroot/uploads/` |
| Charts | Chart.js (CDN) |
| Font | Inter (Google Fonts) |

---

## Roluri și autorizare

Trei roluri definite în `UserRole` enum: `Customer`, `Restaurant`, `Admin`.

Autorizarea se face prin cookie claims. La login se emit claims:
- `ClaimTypes.NameIdentifier` → `user.Id`
- `ClaimTypes.Name` → `user.Name`
- `ClaimTypes.Email` → `user.Email`
- `"Role"` → `user.Role.ToString()`
- `"Avatar"` → avatar path (sau logo restaurant pentru rol Restaurant)

Trei policy-uri definite în `Program.cs`:
- `CustomerOnly` → `RequireClaim("Role", "Customer")`
- `RestaurantOnly` → `RequireClaim("Role", "Restaurant")`
- `AdminOnly` → `RequireClaim("Role", "Admin")`

Cookie-ul expiră în 7 zile cu sliding expiration.

---

## Schema bazei de date

### Users
```
Id              int PK
Name            varchar(100)
Email           varchar(200) unique
PasswordHash    text         — BCrypt hash
Phone           varchar(20)  nullable
Role            enum         Customer | Restaurant | Admin
Avatar          text         nullable — path relativ /uploads/...
Address         text         nullable
City            varchar(100) nullable — "București" | "Craiova"
Lat             double       nullable
Lng             double       nullable
IsBlocked       bool         default false
CreatedAt       datetime     UTC
```

### Restaurants
```
Id                    int PK
UserId                int FK → Users
Name                  varchar(200)
Description           text nullable
Address               text
Lat                   double
Lng                   double
Logo                  text nullable — path relativ
CoverImage            text nullable — path relativ
Category              varchar(100) — categoria principală (primul element din Categories)
Categories            text         — categorii multiple, comma-separated e.g. "Pizza,Italian"
City                  varchar(100) — "București" | "Craiova"
DeliveryFee           decimal(10,2)
MinOrderAmount        decimal(10,2)
EstimatedDeliveryTime int          — minute
Rating                decimal(3,2) — medie recenzii, recalculată la fiecare review
TotalReviews          int          — număr total recenzii
IsApproved            bool         default false
IsBlocked             bool         default false
RejectionReason       text         nullable
OpeningHoursJson      text         nullable — JSON cu program pe zile
CreatedAt             datetime     UTC
```

**Format OpeningHoursJson:**
```json
{
  "Monday":    { "Open": "09:00", "Close": "22:00" },
  "Tuesday":   { "IsClosed": true },
  "Wednesday": { "Open": "09:00", "Close": "22:00" },
  ...
}
```

### MenuCategories
```
Id           int PK
RestaurantId int FK → Restaurants
Name         varchar(100)
DisplayOrder int
```

### MenuItems
```
Id           int PK
RestaurantId int FK → Restaurants
CategoryId   int FK → MenuCategories
Name         varchar(200)
Description  text    nullable
Price        decimal(10,2)
Image        text    nullable — path relativ
IsAvailable  bool    default true
IsApproved   bool    default false — necesită aprobare admin
CreatedAt    datetime UTC
```

### MenuItemChangeRequests
```
Id             int PK
MenuItemId     int FK → MenuItems nullable (null pentru Create)
RestaurantId   int FK → Restaurants
Type           enum    Create | Update | Delete
ProposedDataJson text  — JSON cu datele propuse
Status         enum    Pending | Approved | Rejected
AdminNote      text    nullable
CreatedAt      datetime UTC
ReviewedAt     datetime nullable
```

### Orders
```
Id                   int PK
CustomerId           int FK → Users
RestaurantId         int FK → Restaurants
RestaurantOrderNumber int  — număr secvențial per restaurant (#1, #2, ...)
Status               enum  Pending|Accepted|Preparing|ReadyForPickup|OutForDelivery|Delivered|Rejected|Cancelled
ItemsJson            text  — snapshot JSON al coșului la momentul plasării comenzii
Subtotal             decimal(10,2)
DeliveryFee          decimal(10,2)
Discount             decimal(10,2) default 0
Total                decimal(10,2)
PaymentMethod        enum  Card | Cash
PaymentStatus        enum  Pending | Paid | Failed
StripePaymentIntentId text nullable
DeliveryAddress      text
DeliveryBlock        varchar(50)  nullable
DeliveryStaircase    varchar(50)  nullable
DeliveryApartment    varchar(100) nullable
DeliveryLat          double       nullable
DeliveryLng          double       nullable
PhoneNumber          text         nullable — telefon pentru livrare
DeliveryComment      text         nullable — "fără ceapă, etaj 3" etc.
PromoCodeId          int FK → PromoCodes nullable
CourierLat           double       nullable — poziție curier live
CourierLng           double       nullable
RejectionReason      text         nullable
AcceptedAt           datetime     nullable
PreparingAt          datetime     nullable
ReadyAt              datetime     nullable
OutForDeliveryAt     datetime     nullable
DeliveredAt          datetime     nullable
RejectedAt           datetime     nullable
CreatedAt            datetime     UTC
UpdatedAt            datetime     UTC
```

### OrderItems
```
Id            int PK
OrderId       int FK → Orders
MenuItemId    int FK → MenuItems
NameSnapshot  varchar(200) — copie a numelui la momentul comenzii
PriceSnapshot decimal(10,2) — copie a prețului la momentul comenzii
Quantity      int
```

### Reviews
```
Id           int PK
CustomerId   int FK → Users
RestaurantId int FK → Restaurants
OrderId      int FK → Orders nullable
Rating       int (1-5)
Comment      text nullable
CreatedAt    datetime UTC
```
> Un client poate lăsa o singură recenzie per restaurant (nu per comandă).
> La submit: recalculează `Rating` și `TotalReviews` pe restaurant.

### PromoCodes
```
Id            int PK
Code          varchar — uppercase unique
Description   text nullable
DiscountType  enum   Percentage | Fixed
DiscountValue decimal(10,2)
MinOrderAmount decimal(10,2)
MaxUses       int (0 = nelimitat)
UsedCount     int
ExpiresAt     datetime nullable
IsActive      bool
CreatedAt     datetime UTC
```

### Favorites
```
Id           int PK
CustomerId   int FK → Users
RestaurantId int FK → Restaurants
CreatedAt    datetime UTC
UNIQUE(CustomerId, RestaurantId)
```

### ContactMessages
```
Id         int PK
Name       varchar(100)
Email      varchar(200)
Subject    varchar(200)
Message    text
SentAt     datetime UTC
IsRead     bool default false
IsReplied  bool default false
```

---

## Structura aplicației

### Controllers

| Controller | Policy | Responsabilitate |
|---|---|---|
| `HomeController` | public | Pagina principală, filtrare/sortare restaurante |
| `AccountController` | public | Register, Login, Logout, pagini de stare |
| `RestaurantsController` | public (+ CustomerOnly pentru favorite) | Detaliu restaurant, toggle favorite |
| `CartController` | CustomerOnly | Coș, checkout, Stripe, plasare comandă, recomandări |
| `OrdersController` | CustomerOnly | Istoric comenzi, tracking |
| `ProfileController` | CustomerOnly | Editare profil, schimbare parolă |
| `FavoritesController` | CustomerOnly | Lista favorite |
| `RestaurantController` | RestaurantOnly | Dashboard, comenzi, meniu, change requests, profil restaurant |
| `AdminController` | AdminOnly | Dashboard, restaurante, useri, comenzi, promo codes, change requests, mesaje |
| `PagesController` | public | About, FAQ, Contact, Terms, Privacy, Parteneri |

---

## Pagini și rute

### Public
```
GET  /                          Pagina principală cu restaurante
GET  /restaurants/{id}          Detaliu restaurant + meniu
GET  /account/login
POST /account/login
GET  /account/register
POST /account/register
GET  /account/register-restaurant
POST /account/register-restaurant
GET  /account/pending           "Cerere în așteptare" (după înregistrare restaurant)
GET  /account/suspended         Cont blocat
GET  /account/restaurant-suspended
GET  /about
GET  /how-it-works
GET  /become-partner
GET  /faq
GET  /contact
POST /contact
GET  /terms
GET  /privacy
GET  /partner-restaurants
```

### Customer (CustomerOnly)
```
GET  /cart
POST /Cart/Add                  AJAX — adaugă în coș
POST /Cart/Remove               AJAX — șterge item
POST /Cart/UpdateQuantity       AJAX — modifică cantitate
POST /Cart/Clear                AJAX — golește coș
POST /Cart/ApplyPromo           AJAX — validează cod promo
POST /Cart/CreatePaymentIntent  AJAX — inițializează Stripe
POST /Cart/PlaceOrder           AJAX — plasează comanda
GET  /Cart/Recommendations      AJAX — recomandări produse (drinks/sides/desserts)

GET  /orders                    Istoric comenzi
GET  /orders/{id}               Tracking comandă
POST /orders/{id}/review        Lasă recenzie
POST /orders/{id}/cancel        Anulează comandă (dacă e Pending)

GET  /profile                   Editare profil
POST /profile/save-profile      Salvează profil
POST /profile/change-password   Schimbă parola
POST /profile/delete-avatar     Șterge avatar

GET  /favorites                 Lista restaurante favorite
POST /restaurants/favorite/{id} AJAX — toggle favorite
```

### Restaurant (RestaurantOnly)
```
GET  /restaurant/dashboard      → redirect la Stats dacă aprobat, altfel Pending
GET  /restaurant/stats          Statistici vânzări și grafice
GET  /restaurant/orders         Comenzi în timp real
GET  /restaurant/menu           Gestionare meniu
GET  /restaurant/change-requests Lista change requests trimise
GET  /restaurant/profile        Editare profil restaurant

POST /restaurant/update-profile
POST /restaurant/add-category
POST /restaurant/delete-category
POST /restaurant/submit-item-change  Trimite change request (create/update/delete)
POST /restaurant/toggle-availability AJAX — activează/dezactivează item
```

### Admin (AdminOnly)
```
GET  /admin                     Dashboard platformă
GET  /admin/restaurants         Tab Pending + tab Toate
POST /admin/restaurants/approve/{id}
POST /admin/restaurants/reject/{id}
POST /admin/restaurants/block/{id}
POST /admin/restaurants/unblock/{id}

GET  /admin/change-requests     Change requests pending
POST /admin/change-requests/approve/{id}
POST /admin/change-requests/reject/{id}

GET  /admin/users               Toți utilizatorii
POST /admin/users/block/{id}
POST /admin/users/unblock/{id}

GET  /admin/orders              Toate comenzile platformei
GET  /admin/promo-codes         Coduri promo
POST /admin/promo-codes/create
POST /admin/promo-codes/toggle/{id}
POST /admin/promo-codes/delete/{id}

GET  /admin/messages            Mesaje contact
POST /admin/messages/mark-read/{id}
```

---

## Flux autentificare

1. **Customer register**: formular Nume / Email / Telefon / Parolă / Oraș → BCrypt hash → cookie sign-in → redirect `/`
2. **Restaurant register**: formular extins (date restaurant + ore funcționare + logo/cover upload) → `IsApproved=false` → pagina Pending
3. **Login**: verificare BCrypt → verificare `IsBlocked` → emitere claims → redirect pe bază de rol
4. **Logout**: `SignOutAsync` + `Session.Clear()`

---

## Coșul de cumpărături

Coșul este stocat în **server-side Session** ca JSON (`"Cart"` key). Model:
```csharp
Cart { RestaurantId, RestaurantName, DeliveryFee, List<CartItem> }
CartItem { MenuItemId, Name, Price, Quantity }
```

Comportamente:
- Dacă utilizatorul adaugă un produs de la alt restaurant → răspuns `differentRestaurant=true` → modal de confirmare
- Cantitate maximă per item: 99
- Butonul minus la qty=1 → elimină itemul
- `CartCount` stocată și în `Session["CartCount"]` (int) pentru badge navbar

---

## Pagina de checkout (cart)

- Adresă de livrare pre-populată din profil (dacă există)
- Telefon pre-populat din profil
- Câmp "Mențiuni comandă" (DeliveryComment)
- Hartă Leaflet pentru selectare adresă delivery cu reverse geocoding Nominatim
- Câmpuri opționale: Bloc, Scară, Apartament
- Selectare metodă plată: Card (Stripe) sau Cash
- Câmp cod promo cu validare AJAX
- La Card: `CreatePaymentIntent` → Stripe Elements → confirm → `PlaceOrder`
- La Cash: `PlaceOrder` direct cu `PaymentStatus=Pending`
- La plasare: creare `Order` + `OrderItems` în DB, golire sesiune coș, notificare SignalR restaurant, email confirmare client

---

## Real-time (SignalR — OrderHub)

Hub-ul la `/hubs/orders`. Metode client → server:

| Metodă | Cine o apelează | Ce face |
|---|---|---|
| `JoinRestaurantGroup(restaurantId)` | Restaurant la load pagină comenzi | Intră în grupul `restaurant-{id}` |
| `JoinOrderGroup(orderId)` | Client la load pagina tracking | Intră în grupul `order-{id}` |
| `JoinCustomerGroup(customerId)` | Client la load orice pagină | Intră în grupul `customer-{id}` |
| `UpdateOrderStatus(orderId, newStatus)` | Restaurant | Avansează statusul (tranziție validată strict: Pending→Accepted→...→Delivered) |
| `RejectOrder(orderId, reason)` | Restaurant | Respinge comanda cu motiv |

Evenimente server → client:

| Eveniment | Grup | Payload |
|---|---|---|
| `order_incoming` | `restaurant-{id}` | Obiect complet comandă |
| `order_status_changed` | `order-{id}` + `customer-{id}` | orderId, newStatus |
| `order_updated` | `restaurant-{id}` | orderId, newStatus |
| `courier_location` | `order-{id}` | lat, lng |

**Simulare curier:** La tranziția `OutForDelivery`, pornește `Task.Run` background care interpolează poziția curierului (smoothstep ease-in-out) în 90 de pași × 1 secundă, de la coordonatele restaurantului la coordonatele de livrare. La final, marchează automat comanda ca `Delivered`.

Garanție: simularea verifică la fiecare pas că statusul comenzii este încă `OutForDelivery` (oprire dacă cineva a schimbat manual).

---

## Sistemul de meniu și change requests

Restaurantele nu pot modifica meniul direct. Orice adăugare / editare / ștergere creează un `MenuItemChangeRequest` cu `Status=Pending` și `ProposedDataJson` cu datele propuse.

**ProposedDataJson** conține: `{ name, description, price, categoryId, image, isAvailable }` pentru Create/Update; pentru Delete doar confirmarea.

Adminul aprobă sau respinge din `/admin/change-requests`:
- **Aprobare**: aplică modificarea efectivă pe `MenuItem`, marchează request ca `Approved`, trimite email restaurantului
- **Respingere**: marchează `Rejected`, adaugă `AdminNote`, trimite email

La restaurant, itemele cu request pending afișează badge "Aprobare în așteptare".

---

## Notificări email (MailKit)

Toate trimiterile sunt fire-and-forget (`Task.Run`) pentru a nu bloca răspunsul HTTP.

| Trigger | Destinatar | Conținut |
|---|---|---|
| Comandă plasată | Client | Sumar produse, total, timp estimat |
| Comandă acceptată | Client | Restaurantul a confirmat |
| Comandă în preparare | Client | Mâncarea se pregătește |
| Comandă în livrare | Client | Pe drum! |
| Comandă livrată | Client | Link recenzie |
| Comandă respinsă | Client | Motivul respingerii |
| Restaurant aprobat | Restaurant | Bun venit, cont activ |
| Restaurant respins | Restaurant | Motivul respingerii |
| Change request aprobat | Restaurant | Itemul aprobat |
| Change request respins | Restaurant | Itemul + nota admin |

---

## Vizibilitate restaurante

Un restaurant apare pe pagina principală și hartă **doar dacă**:
- `IsApproved = true`
- `IsBlocked = false`
- Are cel puțin 1 `MenuItem` cu `IsApproved = true`

Panoul restaurant afișează un banner de avertizare dacă restaurantul este aprobat dar nu are niciun produs aprobat.

---

## Pagina principală — Filtre și sortare

**Filtre active:**
- 🟢 Deschis acum (calculat in-memory cu `OpeningHoursHelper.IsOpenNow`)
- 🚚 Livrare gratuită (`DeliveryFee == 0`)
- ⭐ Rating 4+ (`Rating >= 4`)
- 📍 Filtru oraș: Toate / București / Craiova (doar pentru clienți autentificați — se alege automat din profil)

**Categorii (pills):**
Pizza | Sushi | Salate | Românesc | Fast Food | Asian | Mexican | Cafenea | Grill | Vegan | Indian | Seafood

**Sortare (tabs):**
- **Cel mai rapid** — `OrderBy(EstimatedDeliveryTime)` SQL (default)
- **Populare** — `OrderByDescending(TotalReviews)` SQL
- **💸 Cel mai ieftin** — sortat in-memory după media prețurilor preparatelor de mâncare (excluzând băuturi, deserturi, garnituri — detectate prin keyword matching pe numele categoriei)
- **✨ Recomandate** — sortat in-memory cu scor personalizat (disponibil doar pentru clienți autentificați; redirecționează la login altfel)

**Algoritm "Recomandate":**
```
scor = (număr comenzi anterioare de la restaurantul X) × 3
     + (numărul de categorii preferate care match cu categoriile restaurantului) × 1
```
Categoriile preferate = categoriile de produse comandate cel mai des din istoricul comenzilor `Delivered`.

**Algoritm "Cel mai ieftin":**
- Calculează media prețurilor felurilor principale per restaurant (feluri = items din categorii care NU sunt băuturi/deserturi/garnituri)
- Keyword detection pe `Category.Name`: băuturi (băutură, bautura, suc, cafea, apă, ceai, bere, vin, cocktail...), deserturi (desert, dulciuri, tort...), garnituri (garnituri, salate, supe...)
- Restaurantele fără feluri principale detectabile apar la final

---

## Pagina restaurant — Recomandări produse (modal)

După ce un client adaugă un produs în coș, poate apărea un modal cu sugestii:

**Logică smart (per sesiune, tracking în `sessionStorage`):**
- Primul produs principal adăugat (coșul era gol) → modal cu **băuturi** (fallback: **garnituri**)
- Adaugă ceva cu coșul deja plin → modal cu **deserturi**
- Fiecare tip de modal apare **o singură dată** per sesiune per restaurant

**Conținut modal:**
- Carousel cu peek effect: arată 1 card complet + jumătate din al doilea
- Săgeți navigare stânga/dreapta + indicator pagină (x / total)
- Max 4 produse recomandate
- Produsele ordonate: comandate anterior de user → cele mai populare în restaurant (numărat din `OrderItems`)
- Badge "Comandat înainte ✓" pe produsele deja comandate

**Endpoint:** `GET /Cart/Recommendations?restaurantId=X&type=drinks|sides|desserts`
- Autentificat CustomerOnly
- Returnează JSON cu lista de produse potrivite

---

## Profilul clientului

- Editare: Nume, Telefon, Adresă, Oraș, Avatar (upload imagine)
- Schimbare parolă cu verificare parolă curentă
- Zona de pericol: buton Deconectare
- Adresa și telefonul se pre-populează automat la checkout

---

## Panoul restaurant

**3 tab-uri vizibile pe orice pagină restaurant:** Dashboard | Comenzi | Meniu

**Dashboard (Stats):**
- Grafic venituri 7/30 zile (toggle) — Chart.js
- Grafic comenzi per zi
- Top 5 produse cel mai vândute
- Rating mediu

**Comenzi (real-time):**
- Conectare automată la SignalR la load pagină
- Comenzi noi apar instant cu beep audio
- Butoane de acțiune per status:
  - Pending → [Acceptă] [Respinge (cu modal + motiv)]
  - Accepted → [Marchează ca în preparare]
  - Preparing → [Gata pentru ridicare]
  - ReadyForPickup → [În livrare]
  - OutForDelivery → [Livrat]
- Tab "Finalizate" cu filtre pe dată/status
- Fiecare comandă afișează: #nr, client, produse, total, metodă plată, adresă, telefon, mențiuni

**Meniu:**
- Vizualizare categorii + produse organizate
- Adăugare categorie nouă (instant, fără aprobare)
- Reordonare categorii (drag-and-drop)
- Adăugare/editare/ștergere produs → creează `MenuItemChangeRequest`
- Toggle disponibilitate produs (instant, fără aprobare)
- Badge "Aprobare în așteptare" pe produse cu request activ

**Profil restaurant:**
- Editare: Nume, Descriere, Categorii (checkboxes), Taxa livrare, Comandă minimă, Timp estimat, Program pe zile, Logo, Cover, Telefon
- Zona de pericol: buton Deconectare

---

## Panoul admin

**Dashboard:**
- Carduri: total useri, total restaurante aprobate, comenzi azi, venit platformă azi (20% din subtotalul comenzilor livrate)
- Grafice Chart.js: comenzi/zi și venit/zi pe ultimele 30 de zile
- Tabel comenzi recente (ultimele 10)
- Contoare: restaurante pending, change requests pending

**Revenue**: afișat ca **20% din subtotalul comenzilor Delivered** (comision platformă)

**Restaurante:**
- Tab Pending: fiecare aplicație cu toate datele, butoane Aprobare/Respingere (cu modal motiv)
- Tab Toate: căutare, blocare/deblocare
- La aprobare/respingere: trimite email restaurantului

**Change Requests:**
- Tabel toate request-urile pending din toate restaurantele
- Modal cu before/after comparison pentru Update
- Aprobare → aplică pe MenuItems + email restaurant
- Respingere → notă admin + email restaurant

**Useri:** Căutare, blocare/deblocare (useri blocați nu se pot loga)

**Comenzi:** Tabel toate comenzile platformei, filtru status/dată/restaurant, modal detaliu

**Promo Codes:** Creare, activare/dezactivare, ștergere

**Mesaje Contact:** Tabel mesaje primite, marcare citit/necitit

---

## Recenzii

- Accesibil din `/orders/{id}` dacă statusul este `Delivered`
- Un client poate lăsa **o singură recenzie per restaurant** (nu per comandă individuală)
- Rating 1-5 stele + comentariu opțional
- La submit: recalculează `Restaurant.Rating` și `Restaurant.TotalReviews`
- Recenziile se afișează pe pagina restaurantului (primele 10)

---

## Favorite

- Buton inimă pe cardurile de restaurant și pe pagina de detaliu restaurant
- Toggle AJAX fără reload
- Pagina `/favorites`: grilă de restaurante favorite în același format card ca Home
- Funcțional și pentru useri neautentificați (redirecționează la login)

---

## Pagini publice informaționale

Toate accesibile public, fără autentificare:
- `/about` — Despre EatUp
- `/how-it-works` — Cum funcționează
- `/become-partner` — Devino partener
- `/faq` — Întrebări frecvente
- `/contact` — Formular contact (AJAX, salvat în DB ca `ContactMessage`)
- `/terms` — Termeni și condiții
- `/privacy` — Politică confidențialitate
- `/partner-restaurants` — Lista tuturor restaurantelor partenere

---

## Seed data (SeedData.cs)

La pornire, dacă DB-ul este gol, se inserează:
- 1 cont admin
- 5 restaurante aprobate cu logo, cover, program, categorii
- Fiecare restaurant: 3 categorii de meniu + 8 produse aprobate cu imagini
- 3 conturi customer
- 3 coduri promo active

---

## UI Design

- **Culori:** Navy `#0f172a` (primary), Portocaliu `#f97316` (accent), Alb (cards)
- **Font:** Inter (Google Fonts)
- **Stil:** Modern, premium, inspirat din Wolt
- **Grid restaurante:** 3 coloane desktop, 2 tabletă, 1 mobil
- **Skeleton loaders** pe lista de restaurante (400ms delay, înlocuiesc placeholder-ele)
- **Toast notifications** pentru: adăugare coș, favorite toggle, promo aplicat, erori
- **Navbar:** colaps hamburger pe mobil, badge coș, avatar cu redirect la profil (click direct, fără dropdown)
- **Cart sidebar** pe desktop (sticky), bottom bar pe mobil
- **Erori custom:** pagini 404 și 403 branded

---

## Migrări EF Core (în ordine)

1. `InitialCreate` — schema completă inițială
2. `AddOrderPhoneAndComment` — PhoneNumber, DeliveryComment pe Orders
3. `AddRestaurantOrderNumberAndTimestamps` — RestaurantOrderNumber, timestamp-uri status pe Orders
4. `AddCityAndCategories` — City, Categories pe Restaurants; City pe Users
5. `AddContactMessages` — tabelul ContactMessages
6. `AddOrderDeliveryDetails` — DeliveryBlock, DeliveryStaircase, DeliveryApartment pe Orders
7. `ReviewPerRestaurant_And_IsReplied` — IsReplied pe ContactMessages, schimbat Review unique constraint

---

## Configurare (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=eatup;..."
  },
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_..."
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "...",
    "Password": "..."
  }
}
```

---

## Fișiere și structură proiect

```
EatUp/
├── Controllers/
│   ├── HomeController.cs        Pagina principală
│   ├── AccountController.cs     Auth
│   ├── RestaurantsController.cs Detaliu restaurant, favorite
│   ├── CartController.cs        Coș, checkout, Stripe, recomandări
│   ├── OrdersController.cs      Tracking comenzi client
│   ├── ProfileController.cs     Profil client
│   ├── FavoritesController.cs   Favorite
│   ├── RestaurantController.cs  Panou restaurant
│   ├── AdminController.cs       Panou admin
│   └── PagesController.cs       Pagini statice + contact
├── Hubs/
│   └── OrderHub.cs              SignalR hub (real-time comenzi + curier)
├── Models/
│   ├── User.cs
│   ├── Restaurant.cs
│   ├── MenuCategory.cs
│   ├── MenuItem.cs
│   ├── MenuItemChangeRequest.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── Review.cs
│   ├── PromoCode.cs
│   ├── Favorite.cs
│   ├── ContactMessage.cs
│   ├── Cart.cs                  Model sesiune coș (nu în DB)
│   └── Enums.cs                 UserRole, OrderStatus, PaymentMethod, etc.
├── ViewModels/
│   ├── HomeViewModel.cs         RestaurantCardViewModel + HomeViewModel
│   ├── RestaurantDetailViewModel.cs
│   ├── CartViewModel.cs
│   ├── OrderTrackingViewModel.cs
│   ├── MenuManagementViewModel.cs
│   ├── RestaurantOrdersViewModel.cs
│   ├── RestaurantStatsViewModel.cs
│   ├── AdminDashboardViewModel.cs
│   ├── RegisterViewModel.cs
│   ├── RegisterRestaurantViewModel.cs
│   └── LoginViewModel.cs
├── Views/
│   ├── Home/Index.cshtml         Pagina principală
│   ├── Restaurants/Detail.cshtml Detaliu restaurant + modal recomandări
│   ├── Cart/Index.cshtml         Checkout
│   ├── Orders/Index.cshtml       Istoric comenzi
│   ├── Orders/Details.cshtml     Tracking + recenzie
│   ├── Profile/Index.cshtml      Profil client
│   ├── Favorites/Index.cshtml    Favorite
│   ├── Restaurant/               Toate paginile panou restaurant
│   ├── Admin/                    Toate paginile panou admin
│   ├── Account/                  Login, Register, pagini stare
│   ├── Pages/                    Pagini informaționale
│   └── Shared/_Layout.cshtml     Layout comun
├── Data/
│   ├── ApplicationDbContext.cs
│   └── SeedData.cs
├── Services/
│   ├── IEmailService.cs
│   └── EmailService.cs
├── Helpers/
│   └── OpeningHoursHelper.cs    IsOpenNow + GetScheduleSummary
├── Migrations/                  7 migrări EF Core
└── Program.cs                   Setup DI, middleware, routes
```

---

## Note pentru sesiunile Claude Code

La începutul fiecărei sesiuni noi, paste:

```
Citește PLAN.md. Lucrăm la EatUp, platformă ASP.NET Core MVC .NET 10.
Toate funcționalitățile descrise în PLAN.md sunt implementate și funcționale.
Continuă cu: [descrie ce vrei să faci]
```
