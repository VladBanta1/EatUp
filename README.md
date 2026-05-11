# EatUp — Platformă de Livrare Mâncare

> Aplicație web full-stack pentru comandarea mâncării online, inspirată de Wolt, Glovo și Bolt Food.

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4?logo=dotnet)
![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?logo=mysql&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-Real--time-00B4D8)

EatUp este o platformă de livrare mâncare dezvoltată ca proiect de licență, care conectează clienți, restaurante și echipa administrativă într-o singură aplicație web. Clienții pot răsfoi restaurante, plasa comenzi și urmări livrarea în timp real pe hartă. Restaurantele primesc notificări instant, gestionează meniurile și procesează comenzile dintr-un panou dedicat. Administratorii controlează întreaga platformă: aprobă restaurante, moderează meniuri, gestionează utilizatori și monitorizează statistici.

---

## Cuprins

1. [Descrierea proiectului](#1-descrierea-proiectului)
2. [Funcționalități principale](#2-funcționalități-principale)
3. [Tehnologii folosite](#3-tehnologii-folosite)
4. [Arhitectura aplicației](#4-arhitectura-aplicației)
5. [Schema bazei de date](#5-schema-bazei-de-date)
6. [Cerințe de sistem](#6-cerințe-de-sistem)
7. [Instalare și configurare](#7-instalare-și-configurare)
8. [Configurare opțională](#8-configurare-opțională)
9. [Date de test — conturi demo](#9-date-de-test--conturi-demo)
10. [Ghid de testare complet](#10-ghid-de-testare-complet)
11. [Structura proiectului](#11-structura-proiectului)
12. [API Endpoints](#12-api-endpoints)
13. [Funcționalități timp real — SignalR](#13-funcționalități-timp-real--signalr)
14. [Capturi de ecran](#14-capturi-de-ecran)
15. [Limitări cunoscute](#15-limitări-cunoscute)
16. [Autor](#16-autor)

---

## 1. Descrierea proiectului

### Problema rezolvată

Piața de livrare mâncare din România a crescut semnificativ în ultimii ani, însă soluțiile existente (Tazz, Bolt Food) sunt sisteme închise, complexe și greu de studiat din punct de vedere tehnic. EatUp oferă o implementare completă, de la zero, a aceluiași model de business — cu toate cele trei perspective: client, restaurant și administrare.

### Publicul țintă

| Actor | Descriere |
|---|---|
| **Client** | Persoana care caută restaurante, comandă mâncare și urmărește livrarea |
| **Restaurant** | Managerul care gestionează meniul și procesează comenzile primite |
| **Admin** | Echipa platformei care aprobă parteneri, moderează conținut și monitorizează activitatea |

### Context academic

Proiect de licență realizat la **Universitatea din Craiova**, Facultatea de Automatică, Calculatoare și Electronică, specializarea Calculatoare și Tehnologia Informației, anul universitar 2025–2026. Proiectul demonstrează aplicarea practică a tehnologiilor web moderne: arhitectura MVC, comunicare în timp real cu SignalR, integrare cu API-uri externe (Stripe, Nominatim, MailKit) și persistența datelor cu Entity Framework Core pe MySQL.

---

## 2. Funcționalități principale

### Client (Customer)

- **Navigare restaurante** — grid cu filtre multiple: categorie culinară, oraș (București / Craiova), sortare după rating / timp de livrare / popularitate, filtre „deschis acum", „livrare gratuită", „rating 4+"
- **Căutare full-text** — după numele restaurantului sau după produsele din meniu
- **Hartă interactivă** — restaurante afișate ca markere pe hartă Leaflet/OpenStreetMap pe pagina principală
- **Detalii restaurant** — pagina de detalii cu hero, meniu organizat pe categorii, secțiune recenzii cu rating agregat, indicator deschis/închis bazat pe programul restaurantului
- **Favorite** — adaugare/eliminare restaurante din lista de favorite, pagină dedicată
- **Coș de cumpărături** — sesiune persistentă, qty +/−, eliminare produse, validare comandă minimă
- **Cod promoțional** — aplicare cod cu validare tip (procentual / sumă fixă), valoare minimă, limită utilizări, dată expirare
- **Locație livrare pe hartă** — click pe harta Leaflet din pagina coș pentru a plasa pin; geocodare inversă automată via Nominatim pentru completarea adresei
- **Plată cu cardul** — integrare Stripe (PaymentIntent + confirmCardPayment în browser)
- **Plată cu numerar** — la ușă, fără configurare suplimentară
- **Comandă cu apartament / etaj / bloc** — câmpuri opționale suplimentare pentru adresa de livrare
- **Urmărire comandă în timp real** — pagina de tracking cu cronologie vizuală a statusurilor, marker animat al curierului pe hartă, actualizări live via SignalR
- **Sunet notificare** — chime Web Audio API la fiecare schimbare de status
- **Anulare comandă** — posibilă atâta timp cât comanda este în starea Pending sau Accepted
- **Recenzie restaurant** — formular cu rating stele (1–5) și comentariu, disponibil după livrare, o singură recenzie per restaurant
- **Istoric comenzi** — lista tuturor comenzilor cu status și detalii
- **Profil utilizator** — editare nume, telefon, adresă, oraș, avatar (upload imagine)
- **Schimbare parolă** — cu validare parolă curentă
- **Avertizare oraș diferit** — banner pe pagina restaurantului când restaurantul nu livrează în orașul clientului

### Restaurant (Restaurant Manager)

- **Înregistrare cu formulare extins** — date cont, detalii restaurant, coordonate via click pe hartă, imagini logo/copertă, program pe zile (luni–duminică cu opțiune „închis")
- **Așteptare aprobare** — pagina de pending afișată după înregistrare până la aprobarea adminului
- **Panou comenzi în timp real** — primire notificări instant (sunet + toast + popup) pentru comenzi noi, actualizare automată a cardurilor de comandă fără refresh
- **Gestionare status comenzi** — butoane pentru avansare progresivă: Pending → Accepted → Preparing → Ready for Pickup → Out for Delivery → Delivered
- **Respingere comandă** — cu motiv text trimis clientului
- **Notificare anulare client** — sunet + toast când clientul anulează comanda
- **Gestionare meniu** — categorii cu drag-and-drop pentru reordonare (SortableJS), adăugare/redenumire/ștergere categorii via AJAX
- **Produse meniu** — adăugare, editare, ștergere cu trimitere spre aprobare admin (sistem change requests)
- **Disponibilitate produs** — toggle rapid activare/dezactivare fără aprobare
- **Statistici restaurant** — grafice Chart.js pentru comenzi și venituri pe ultimele 7 / 30 zile, top 5 produse comandate
- **Profil restaurant** — editare toate detaliile inclusiv program orar, logo, copertă; avatarul din navbar se actualizează instant
- **Cont suspendat** — redirect automat și deconectare dacă restaurantul este blocat de admin

### Admin

- **Dashboard cu KPI-uri** — număr restaurante active, comenzi azi, venit platformă azi (comision 20% din subtotal), utilizatori noi azi
- **Grafice dashboard** — evoluție comenzi și venituri pe ultimele 30 de zile (Chart.js)
- **Aprobarea restaurantelor** — vizualizare aplicații pendinte, aprobare sau respingere cu motiv (trimite email automat)
- **Gestionare restaurante** — lista tuturor restaurantelor cu filtre pe tab-uri, blocare/deblocare
- **Gestionare utilizatori** — căutare, blocare/deblocare utilizatori
- **Gestionare comenzi** — filtrare după status, restaurant, interval dată; vizualizare detalii comandă în modal
- **Change requests meniu** — aprobarea sau respingerea modificărilor de meniu propuse de restaurante; aplicare automată în baza de date la aprobare
- **Coduri promoționale** — creare, activare/dezactivare, ștergere
- **Mesaje de contact** — citire mesaje primite din formularul de contact, marcare citit, marcare răspuns trimis
- **Notificări timp real** — sunet + toast la comenzi noi pe orice pagină admin

---

## 3. Tehnologii folosite

| Tehnologie | Versiune | Rol |
|---|---|---|
| ASP.NET Core MVC | .NET 10.0 | Framework principal, routing, controller/view pipeline |
| Entity Framework Core | 9.0.0 | ORM pentru accesul la baza de date |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 | Provider MySQL pentru EF Core |
| MySQL | 8.0+ | Baza de date relațională |
| ASP.NET Core SignalR | (inclus în .NET) | Comunicare bidirecțională în timp real WebSocket |
| Bootstrap | 5.3 | Framework CSS, componente UI responsive |
| Leaflet.js | 1.9.4 | Hărți interactive cu OpenStreetMap |
| Chart.js | 4.4.0 | Grafice pentru statistici și dashboard |
| SortableJS | latest CDN | Drag-and-drop pentru reordonarea categoriilor de meniu |
| Stripe.net | 51.1.0 | Integrare plăți cu cardul (PaymentIntent API) |
| MailKit | 4.16.0 | Trimitere email-uri tranzacționale via SMTP |
| BCrypt.Net-Next | 4.1.0 | Hashing parole (bcrypt cu salt automat) |
| Web Audio API | (browser native) | Sunete notificare generate programatic |
| Nominatim API | OpenStreetMap | Geocodare inversă (coordonate → adresă text) |
| Razor Views | (inclus în .NET) | Template engine server-side pentru HTML |
| Cookie Authentication | (inclus în .NET) | Autentificare bazată pe cookie-uri cu claims |

---

## 4. Arhitectura aplicației

### Pattern MVC

Aplicația respectă strict pattern-ul **Model-View-Controller**:

- **Modele** — clasele din `Models/` și `ViewModels/` reprezintă datele. Entitățile EF Core sunt mapate direct pe tabelele MySQL. ViewModels sunt folosite pentru a transmite date compuse spre views.
- **Controlere** — clasele din `Controllers/` gestionează requesturile HTTP, interoghează baza de date prin `ApplicationDbContext`, și returnează views sau JSON.
- **Views** — fișierele `.cshtml` din `Views/` sunt template-uri Razor care primesc un ViewModel și generează HTML. Layout-ul principal (`_Layout.cshtml`) injectează SignalR și sistemul de notificări.

### Structura folderelor

```
EatUp/
├── Controllers/           # Controllere MVC
│   ├── AccountController.cs       # Autentificare, înregistrare
│   ├── AdminController.cs         # Panou administrare
│   ├── CartController.cs          # Coș, comenzi, Stripe
│   ├── FavoritesController.cs     # Favorite
│   ├── HomeController.cs          # Pagina principală cu filtre
│   ├── OrdersController.cs        # Tracking comenzi client
│   ├── PagesController.cs         # Pagini statice (About, FAQ etc.)
│   ├── ProfileController.cs       # Profil utilizator
│   ├── RestaurantController.cs    # Panou restaurant
│   └── RestaurantsController.cs   # Detalii restaurant, toggle favorite
├── Data/
│   ├── ApplicationDbContext.cs    # Context EF Core cu toate DbSet-urile
│   └── SeedData.cs                # Seed automat: conturi, restaurante, meniu, recenzii
├── Helpers/
│   └── OpeningHoursHelper.cs      # Logică parsare/verificare program orar
├── Hubs/
│   └── OrderHub.cs                # Hub SignalR pentru comenzi și livrare
├── Migrations/                    # Migrări EF Core generate automat
├── Models/                        # Entități baza de date
│   ├── Cart.cs, CartItem.cs       # Modele sesiune (non-entitate)
│   ├── ContactMessage.cs
│   ├── Enums.cs                   # UserRole, OrderStatus, PaymentMethod etc.
│   ├── Favorite.cs
│   ├── MenuItem.cs, MenuCategory.cs, MenuItemChangeRequest.cs
│   ├── Order.cs, OrderItem.cs
│   ├── PromoCode.cs
│   ├── Restaurant.cs
│   ├── Review.cs
│   └── User.cs
├── Services/
│   ├── IEmailService.cs           # Interfață serviciu email
│   └── EmailService.cs            # Implementare SMTP cu MailKit (fire-and-forget)
├── ViewModels/                    # Modele compuse pentru views
│   ├── AdminDashboardViewModel.cs
│   ├── CartViewModel.cs
│   ├── HomeViewModel.cs
│   ├── LoginViewModel.cs
│   ├── MenuManagementViewModel.cs
│   ├── OrderTrackingViewModel.cs
│   ├── RegisterRestaurantViewModel.cs
│   ├── RegisterViewModel.cs
│   ├── RestaurantDetailViewModel.cs
│   ├── RestaurantOrdersViewModel.cs
│   └── RestaurantStatsViewModel.cs
├── Views/                         # Template-uri Razor
│   ├── Account/                   # Login, Register, Pending, Suspended
│   ├── Admin/                     # Dashboard, Restaurants, Users, Orders, ChangeRequests, PromoCodes, Messages
│   ├── Favorites/                 # Lista favorite
│   ├── Home/                      # Index (pagina principală)
│   ├── Orders/                    # Index (istoric), Details (tracking)
│   ├── Pages/                     # About, HowItWorks, BecomePartner, FAQ, Contact, Terms, Privacy, PartnerRestaurants
│   ├── Profile/                   # Profil client
│   ├── Restaurant/                # Dashboard, Orders, Menu, ChangeRequests, Stats, Profile
│   ├── Restaurants/               # Detail (pagina restaurant)
│   └── Shared/                    # _Layout.cshtml, Error.cshtml
├── wwwroot/                       # Fișiere statice
│   ├── css/                       # Stiluri personalizate
│   ├── img/
│   │   ├── menu/                  # Imagini produse locale
│   │   └── restaurants/           # Logo-uri și coperte locale
│   └── js/                        # JavaScript personalizat
├── appsettings.json               # Configurare: connection string, Stripe, Email
├── Program.cs                     # Configurare aplicație, middleware, seed la startup
└── EatUp.csproj                   # Referințe pachete NuGet, TargetFramework
```

### Fluxul de autentificare

1. Utilizatorul completează formularul de login (`POST /Account/Login`)
2. Controllerul verifică email + parolă (BCrypt), verifică `IsBlocked`, verifică `IsApproved` (pentru restaurante)
3. Se creează un `ClaimsPrincipal` cu claims: `NameIdentifier` (userId), `Name`, `Role`, `Email`, `Avatar`
4. Se apelează `HttpContext.SignInAsync("Cookies", principal)` — cookie-ul este scris în browser
5. La fiecare request, middleware-ul de autentificare validează cookie-ul și populează `User`
6. Controllerele cu `[Authorize(Policy = "CustomerOnly/RestaurantOnly/AdminOnly")]` verifică claim-ul `Role`
7. La logout, `HttpContext.SignOutAsync("Cookies")` invalidează cookie-ul

### Arhitectura SignalR

Hub-ul `OrderHub` este montat la `/hubs/orders`. Conexiunea este deschisă de `_Layout.cshtml` la încărcarea oricărei pagini pentru utilizatori autentificați.

**Grupuri de notificare:**
- `restaurant-{id}` — managerul restaurantului (panou comenzi, notificări)
- `order-{id}` — clientul care urmărește o comandă specifică
- `customer-{id}` — clientul (actualizări live pe pagina istoric)

---

## 5. Schema bazei de date

### Tabele și relații

```
Users (1) ────────────────── (1) Restaurants
  │                                   │
  │ (1)                               │ (1)
  ├── Orders (N) ──── (1) PromoCode   ├── MenuCategories (N)
  │     │                             │         │
  │     └── OrderItems (N) ─── (1) MenuItems (N)
  │                                   │
  ├── Reviews (N) ─────────── (1) Restaurants
  │
  └── Favorites (N) ──────── (1) Restaurants
```

### Descrierea tabelelor

#### `Users`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| Name | VARCHAR(100) | Nume complet |
| Email | VARCHAR(200) UNIQUE | Email (și username) |
| PasswordHash | TEXT | Hash BCrypt |
| Phone | VARCHAR(20) | Telefon |
| Role | ENUM | Customer / Restaurant / Admin |
| Avatar | TEXT | URL imagine profil |
| Address | TEXT | Adresă implicită livrare |
| City | VARCHAR(100) | Oraș implicit |
| Lat, Lng | DOUBLE | Coordonate GPS |
| IsBlocked | TINYINT(1) | Cont blocat de admin |
| CreatedAt | DATETIME | Data creării |

#### `Restaurants`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| UserId | INT FK→Users | Managerul restaurantului |
| Name | VARCHAR(200) | Numele restaurantului |
| Description | TEXT | Descriere |
| Address | TEXT | Adresă |
| Lat, Lng | DOUBLE | Coordonate GPS |
| Logo | TEXT | URL logo |
| CoverImage | TEXT | URL imagine copertă |
| Category | VARCHAR(100) | Categorie principală |
| Categories | TEXT | Liste categorii (comma-separated) |
| City | VARCHAR(100) | Orașul restaurantului |
| DeliveryFee | DECIMAL(10,2) | Taxă livrare RON |
| MinOrderAmount | DECIMAL(10,2) | Comandă minimă RON |
| EstimatedDeliveryTime | INT | Timp estimat livrare (minute) |
| Rating | DECIMAL(3,2) | Rating mediu (calculat) |
| TotalReviews | INT | Număr total recenzii |
| IsApproved | TINYINT(1) | Aprobat de admin |
| IsBlocked | TINYINT(1) | Blocat de admin |
| RejectionReason | TEXT | Motivul respingerii |
| OpeningHoursJson | TEXT | Program orar JSON (zile → open/close) |
| CreatedAt | DATETIME | Data înregistrării |

#### `MenuCategories`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| RestaurantId | INT FK→Restaurants | Restaurantul proprietar |
| Name | VARCHAR(100) | Numele categoriei |
| DisplayOrder | INT | Ordinea de afișare |

#### `MenuItems`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| RestaurantId | INT FK→Restaurants | Restaurantul |
| CategoryId | INT FK→MenuCategories | Categoria |
| Name | VARCHAR(200) | Numele produsului |
| Description | TEXT | Descriere |
| Price | DECIMAL(10,2) | Prețul RON |
| Image | TEXT | URL imagine |
| IsAvailable | TINYINT(1) | Disponibil (toggle restaurant) |
| IsApproved | TINYINT(1) | Aprobat de admin |
| CreatedAt | DATETIME | Data adăugării |

#### `Orders`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| CustomerId | INT FK→Users | Clientul |
| RestaurantId | INT FK→Restaurants | Restaurantul |
| Status | ENUM | Pending/Accepted/Preparing/ReadyForPickup/OutForDelivery/Delivered/Rejected/Cancelled |
| RestaurantOrderNumber | INT | Număr secvențial per restaurant (#1, #2...) |
| ItemsJson | TEXT | JSON cu produsele comandate (snapshot) |
| Subtotal | DECIMAL(10,2) | Valoare produse |
| DeliveryFee | DECIMAL(10,2) | Taxă livrare |
| Discount | DECIMAL(10,2) | Reducere aplicată |
| Total | DECIMAL(10,2) | Total final |
| PaymentMethod | ENUM | Card / Cash |
| PaymentStatus | ENUM | Pending / Paid / Failed |
| StripePaymentIntentId | VARCHAR | ID tranzacție Stripe |
| DeliveryAddress | TEXT | Adresa de livrare |
| DeliveryBlock, DeliveryStaircase, DeliveryApartment | VARCHAR | Detalii adresă |
| DeliveryLat, DeliveryLng | DOUBLE | Coordonate GPS livrare |
| PromoCodeId | INT FK→PromoCodes | Cod promoțional aplicat |
| CourierLat, CourierLng | DOUBLE | Poziție curent curier (simulată) |
| PhoneNumber | VARCHAR | Telefon contact |
| DeliveryComment | TEXT | Comentariu livrare |
| RejectionReason | TEXT | Motiv respingere restaurant |
| AcceptedAt, PreparingAt, ReadyAt, OutForDeliveryAt, DeliveredAt, RejectedAt | DATETIME | Timestamp-uri statusuri |
| CreatedAt, UpdatedAt | DATETIME | Creare și ultima actualizare |

#### `OrderItems`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| OrderId | INT FK→Orders | Comanda |
| MenuItemId | INT FK→MenuItems | Produsul |
| NameSnapshot | VARCHAR(200) | Numele la momentul comenzii |
| PriceSnapshot | DECIMAL(10,2) | Prețul la momentul comenzii |
| Quantity | INT | Cantitate |

#### `Reviews`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| CustomerId | INT FK→Users | Clientul |
| RestaurantId | INT FK→Restaurants | Restaurantul |
| OrderId | INT FK→Orders (nullable) | Comanda asociată |
| Rating | INT | Rating 1–5 stele |
| Comment | TEXT | Comentariu opțional |
| CreatedAt | DATETIME | Data recenziei |

*Constrângere unică: un client poate lăsa o singură recenzie per restaurant.*

#### `PromoCodes`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| Code | VARCHAR(50) UNIQUE | Codul promoțional |
| Description | TEXT | Descriere afișată clientului |
| DiscountType | ENUM | Percentage / Fixed |
| DiscountValue | DECIMAL(10,2) | Valoarea reducerii |
| MinOrderAmount | DECIMAL(10,2) | Comandă minimă pentru aplicare |
| MaxUses | INT | Număr maxim utilizări (0 = nelimitat) |
| UsedCount | INT | Număr utilizări actuale |
| ExpiresAt | DATETIME | Data expirării (nullable) |
| IsActive | TINYINT(1) | Activ/inactiv |
| CreatedAt | DATETIME | Data creării |

#### `Favorites`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| CustomerId | INT FK→Users | Clientul |
| RestaurantId | INT FK→Restaurants | Restaurantul |
| CreatedAt | DATETIME | Data adăugării |

*Constrângere unică: (CustomerId, RestaurantId)*

#### `MenuItemChangeRequests`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| MenuItemId | INT FK→MenuItems (nullable) | Produsul modificat (null pentru Create) |
| RestaurantId | INT FK→Restaurants | Restaurantul |
| Type | ENUM | Create / Update / Delete |
| ProposedDataJson | TEXT | JSON cu datele propuse |
| Status | ENUM | Pending / Approved / Rejected |
| AdminNote | TEXT | Nota adminului la respingere |
| CreatedAt | DATETIME | Data cererii |
| ReviewedAt | DATETIME | Data procesării |

#### `ContactMessages`
| Coloană | Tip | Descriere |
|---|---|---|
| Id | INT PK | Cheie primară |
| Name | VARCHAR(100) | Numele expeditorului |
| Email | VARCHAR(200) | Email expeditor |
| Subject | VARCHAR(200) | Subiect |
| Message | TEXT | Conținut mesaj |
| SentAt | DATETIME | Data trimiterii |
| IsRead | TINYINT(1) | Marcat ca citit |
| IsReplied | TINYINT(1) | Marcat ca răspuns trimis |

---

## 6. Cerințe de sistem

| Cerință | Versiune minimă | Note |
|---|---|---|
| Windows | 10 / 11 | macOS și Linux sunt compatibile dar nedebugate |
| .NET SDK | 10.0 | [download.microsoft.com](https://download.microsoft.com/dotnet) |
| Visual Studio | 2022 (v17.12+) | Cu workload-ul **ASP.NET and web development** |
| MySQL | 8.0+ | Recomandat via XAMPP 8.2+ |
| Browser modern | Chrome / Edge / Firefox | Safari nedebudat |
| Git | Orice versiune recentă | Opțional, pentru clonare |

> **Alternativă IDE:** Dacă nu aveți Visual Studio, puteți folosi Visual Studio Code cu extensia C# Dev Kit, plus comenzile `dotnet` din terminal.

---

## 7. Instalare și configurare

### Pasul 1 — Descărcarea proiectului

**Varianta A — Git:**
```bash
git clone https://github.com/[username]/EatUp.git
cd EatUp
```

**Varianta B — ZIP:**
Descărcați arhiva ZIP din GitHub → Extrageți → Deschideți folderul `EatUp`.

---

### Pasul 2 — Instalarea prerequisitelor

1. Descărcați și instalați **.NET SDK 10.0** de pe [dot.net](https://dotnet.microsoft.com/download)
   - Verificați instalarea: `dotnet --version` (trebuie să returneze `10.x.x`)

2. Descărcați și instalați **XAMPP** de pe [apachefriends.org](https://www.apachefriends.org/)
   - Alegeți versiunea cu MySQL 8.x
   - Instalați cu componentele Apache și MySQL bifate

3. Instalați **Visual Studio 2022** (Community Edition este gratuită) de pe [visualstudio.microsoft.com](https://visualstudio.microsoft.com/)
   - La instalare, bifați workload-ul **ASP.NET and web development**

---

### Pasul 3 — Pornirea MySQL

1. Deschideți **XAMPP Control Panel**
2. Apăsați **Start** lângă **MySQL**
3. Verificați că portul 3306 este disponibil (culoarea verde confirmă)
4. **Nu este nevoie să creați manual baza de date** — EatUp o creează automat la primul start

---

### Pasul 4 — Configurarea `appsettings.json`

Deschideți fișierul `EatUp/EatUp/appsettings.json` și ajustați după nevoie:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=eatup_db;User=root;Password=;"
  },
  "Stripe": {
    "PublishableKey": "pk_test_ÎNLOCUIȚI_CU_CHEIA_VOASTRĂ",
    "SecretKey": "sk_test_ÎNLOCUIȚI_CU_CHEIA_VOASTRĂ"
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "adresa_voastra@gmail.com",
    "Password": "parola_aplicatie_gmail",
    "DisplayName": "EatUp"
  }
}
```

| Câmp | Valoare implicită | Descriere |
|---|---|---|
| `DefaultConnection` | `root` fără parolă, port `3306` | Credențialele MySQL din XAMPP |
| `Database` | `eatup_db` | Baza de date se creează automat |
| `Stripe:PublishableKey` | — | Opțional, vedere [Secțiunea 8](#8-configurare-opțională) |
| `Stripe:SecretKey` | — | Opțional, vedere [Secțiunea 8](#8-configurare-opțională) |
| `Email:User` | — | Opțional, vedere [Secțiunea 8](#8-configurare-opțională) |
| `Email:Password` | — | **Nu** parola Gmail, ci un App Password |

> **Important:** Dacă MySQL din XAMPP are parolă pentru root, completați câmpul `Password=` din connection string.

---

### Pasul 5 — Deschiderea proiectului în Visual Studio

1. Deschideți Visual Studio 2022
2. **File → Open → Project/Solution**
3. Navigați la folderul `EatUp/EatUp/` și selectați `EatUp.csproj`
4. Așteptați restaurarea pachetelor NuGet (bara de status din jos)

---

### Pasul 6 — Rularea aplicației

**Din Visual Studio:**
- Apăsați **F5** (cu debugger) sau **Ctrl+F5** (fără debugger)
- Alegeți profilul `https` din dropdown-ul de lângă butonul Run

**Din terminal:**
```bash
cd EatUp/EatUp
dotnet run
```

La primul start, aplicația face automat:
1. **Rulează migrările EF Core** — creează schema bazei de date `eatup_db`
2. **Execută seed-ul de date** — creează conturile demo, cele 13 restaurante cu meniuri complete, codurile promoționale și recenziile

> Seed-ul rulează la fiecare start, dar este **idempotent** — nu duplică datele existente.

---

### Pasul 7 — Accesarea aplicației

Deschideți browser-ul la adresa afișată în consolă, de obicei:

```
https://localhost:7xxx
```

sau

```
http://localhost:5xxx
```

Puteți loga imediat cu oricare din conturile de test din [Secțiunea 9](#9-date-de-test--conturi-demo).

---

## 8. Configurare opțională

### Stripe — plăți cu cardul

Fără configurarea Stripe, opțiunea de plată cu cardul nu funcționează, dar **plata cu numerar funcționează fără nicio configurare**.

**Pași:**

1. Creați un cont gratuit pe [dashboard.stripe.com](https://dashboard.stripe.com)
2. Din meniul lateral, accesați **Developers → API Keys**
3. Copiați **Publishable key** (începe cu `pk_test_`) și **Secret key** (începe cu `sk_test_`)
4. Completați în `appsettings.json`:

```json
"Stripe": {
  "PublishableKey": "pk_test_...",
  "SecretKey": "sk_test_..."
}
```

**Card de test:**

| Câmp | Valoare |
|---|---|
| Număr card | `4242 4242 4242 4242` |
| Data expirare | Orice dată viitoare (ex: `12/28`) |
| CVC | Orice 3 cifre (ex: `123`) |
| Cod poștal | Orice 5 cifre (ex: `12345`) |

---

### Email — notificări automate

Fără configurarea email-ului, aplicația funcționează normal — notificările email sunt trimise în mod fire-and-forget și eșecurile sunt silențioase.

**Pași pentru Gmail:**

1. Activați **2-Factor Authentication** pe contul Gmail
2. Accesați [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords)
3. Generați un **App Password** (selectați App: Mail, Device: Other → "EatUp")
4. Copiați parola de 16 caractere generată
5. Completați în `appsettings.json`:

```json
"Email": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "User": "adresa_voastra@gmail.com",
  "Password": "xxxx xxxx xxxx xxxx",
  "DisplayName": "EatUp"
}
```

**Email-uri trimise automat:**
- Comandă plasată (client)
- Comandă acceptată, în preparare, ieșită la livrare, livrată (client)
- Comandă respinsă cu motiv (client)
- Restaurant aprobat / respins (manager)
- Change request aprobat / respins (manager)

---

## 9. Date de test — conturi demo

Toate conturile sunt create automat la primul start al aplicației.

### Admin

| Rol | Email | Parolă |
|---|---|---|
| Administrator | `admin@eatup.ro` | `Admin@1234` |

### Clienți

| Rol | Email | Parolă | Descriere |
|---|---|---|---|
| Customer | `andrei@example.com` | `Client@123` | Andrei Ionescu |
| Customer | `maria@example.com` | `Client@123` | Maria Popescu |
| Customer | `radu@example.com` | `Client@123` | Radu Constantin |

### Restaurante (toate cu parola `Rest@1234`)

| Oraș | Restaurant | Email |
|---|---|---|
| București | La Mama | `lamama@eatup.ro` |
| București | Sushi Tokyo | `tokyo@eatup.ro` |
| București | Pizza Napoli | `napoli@eatup.ro` |
| București | Dulce Viență | `dulceviata@eatup.ro` |
| București | Taco Loco | `tacoloco@eatup.ro` |
| București | Marele Zid | `marelezid@eatup.ro` |
| București | Carbon & Flăcări | `carbon@eatup.ro` |
| Craiova | Burgeria | `burgeria@eatup.ro` |
| Craiova | Salata Verde | `salata@eatup.ro` |
| Craiova | Shaorma Palace | `shaorma@eatup.ro` |
| Craiova | Taj Mahal | `tajmahal@eatup.ro` |
| Craiova | Pescărie La Dunăre | `pescarie@eatup.ro` |
| Craiova | Verde Pur | `verdepur@eatup.ro` |

### Coduri promoționale disponibile

| Cod | Tip | Valoare | Comandă minimă |
|---|---|---|---|
| `WELCOME10` | Procentual | 10% | 30 RON |
| `EATUP20` | Sumă fixă | 20 RON | 100 RON |
| `SUMMER15` | Procentual | 15% | 50 RON |

---

## 10. Ghid de testare complet

### Flow 1 — Client: plasare și urmărire comandă

1. Deschideți aplicația în browser și apăsați **Autentificare**
2. Logați-vă cu `andrei@example.com` / `Client@123`
3. Pe pagina principală, explorați filtrele: categorii (Pizza, Sushi, Grill etc.), orașe, sortare
4. Căutați „Carbon" în bara de căutare → vedeți Carbon & Flăcări
5. Apăsați pe un restaurant → pagina de detalii cu meniu
6. Adăugați câteva produse în coș (minim comanda a restaurantului)
7. Apăsați iconița coș din navbar → **Checkout**
8. Pe pagina coș:
   - Aplicați codul `WELCOME10` în câmpul de promo → vedeți reducerea aplicată
   - În secțiunea **Locație livrare**, apăsați pe hartă pentru a plasa pin-ul
   - Completați datele de livrare și telefonul
9. Alegeți metoda de plată:
   - **Numerar la livrare** → apăsați **Plasează comanda**
   - **Card** → introduceți `4242 4242 4242 4242`, orice dată viitoare, orice CVC → **Plasează comanda**
10. Sunteți redirecționat la pagina de tracking a comenzii
11. Vedeți cronologia cu statusul **În așteptare** activ
12. Deschideți un alt tab de browser și logați-vă cu contul restaurantului (`carbon@eatup.ro` / `Rest@1234`)
13. Pe panoul restaurantului (Orders), vedeți comanda nouă cu sunet și notificare
14. Apăsați **Acceptă** pe cardul comenzii
15. Reveniți pe tab-ul clientului → statusul se actualizează la **Acceptată** fără refresh
16. Continuați să avansați statusul din panoul restaurantului: Preparing → Ready → Out for Delivery
17. Când statusul devine **Out for Delivery**, pe harta clientului apare un marker animat al curierului care se deplasează spre adresa de livrare
18. Simularea durează ~90 secunde, după care comanda este marcată automat **Livrată**
19. Pe pagina de tracking, apare formularul de recenzie → acordați 5 stele

---

### Flow 2 — Restaurant: gestionare meniu și comenzi

1. Logați-vă cu `pizza@napoli.eatup.ro` → `napoli@eatup.ro` / `Rest@1234`

   *(sau orice alt cont restaurant)*

2. **Panou Comenzi** (meniul din stânga → Comenzi):
   - Vedeți comenzile active și istoricul
   - Plasați o comandă din cont de client pentru a genera o comandă live
   - Primiți sunet + toast + cardul comenzii apare în timp real

3. **Gestionare Meniu** (meniu → Meniu):
   - Trageți categoriile pentru a le reordona (SortableJS)
   - Apăsați **+** lângă o categorie pentru a adăuga un produs nou
   - Completați formularul și apăsați **Trimite spre aprobare**
   - Produsul apare în tab-ul **Change Requests** cu status Pending

4. **Statistici** (meniu → Statistici):
   - Vedeți grafice de comenzi și venituri pe 7 / 30 de zile
   - Top 5 produse comandate

5. **Profil** (meniu → Profil):
   - Modificați programul de lucru (închideți marți)
   - Salvați → indicator **Deschis/Închis** se actualizează pe pagina publică

---

### Flow 3 — Admin: gestionarea platformei

1. Logați-vă cu `admin@eatup.ro` / `Admin@1234`

2. **Dashboard** — vedeți KPI-uri și grafice

3. **Restaurante → tab Pending** — dacă există restaurante noi neaprobate:
   - Apăsați **Aprobă** → restaurantul devine vizibil publicului
   - SAU apăsați **Respinge** → introduceți motivul

4. **Change Requests** — aprobați sau respingeți modificările de meniu trimise de restaurante:
   - Aprobarea unui request de tip Create adaugă produsul direct în meniu
   - Aprobarea unui Update actualizează produsul existent
   - Aprobarea unui Delete elimină produsul

5. **Utilizatori** → căutați „maria" → apăsați **Blochează** → logați-vă cu `maria@example.com` → vedeți mesajul de cont suspendat

6. **Coduri Promo** → creați un cod nou: `TEST50`, tip Procentual, 50%, fără comandă minimă → testați-l la checkout

7. **Comenzi** → filtrați după status `Delivered` → vedeți toate comenzile finalizate

8. **Mesaje** → deschideți un mesaj → apăsați **Marchează ca citit** → apăsați **Răspunde** → completați răspunsul

---

### Flow 4 — Înregistrare restaurant nou

1. Pe pagina de login apăsați **Înregistrează-ți restaurantul**
2. Completați:
   - Datele contului (email nou, parolă)
   - Datele restaurantului (nume, descriere, categorie, taxă livrare, comandă minimă, timp estimat)
   - Adresa + click pe hartă pentru coordonate GPS
   - Logo și imagine copertă (upload opțional)
   - Programul de lucru pe zile
3. Apăsați **Înregistrează restaurantul** → pagina de pending
4. Logați-vă ca admin → **Restaurante → Pending** → Aprobă
5. Logați-vă ca managerul nou → panoul de comenzi este activ

---

## 11. Structura proiectului

```
EatUp/                             ← Root repository
├── EatUp/                         ← Proiectul ASP.NET Core
│   ├── Controllers/
│   │   ├── AccountController.cs   # Login, Register customer/restaurant, Logout
│   │   ├── AdminController.cs     # Toate acțiunile admin
│   │   ├── CartController.cs      # Coș, Stripe PaymentIntent, PlaceOrder
│   │   ├── FavoritesController.cs # Lista favorite
│   │   ├── HomeController.cs      # Pagina principală cu filtre
│   │   ├── OrdersController.cs    # Tracking, anulare, recenzii (Customer)
│   │   ├── PagesController.cs     # Pagini statice + formular contact
│   │   ├── ProfileController.cs   # Profil client
│   │   ├── RestaurantController.cs# Tot panoul restaurant
│   │   └── RestaurantsController.cs # Pagina publică restaurant + favorite toggle
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── SeedData.cs
│   ├── Helpers/
│   │   └── OpeningHoursHelper.cs  # Deserializare JSON program, calcul IsOpenNow
│   ├── Hubs/
│   │   └── OrderHub.cs            # SignalR: UpdateOrderStatus, RejectOrder, SimulateCourier
│   ├── Migrations/                # Auto-generate, nu modificați manual
│   ├── Models/
│   │   ├── Cart.cs               # Model sesiune (nu este entitate EF)
│   │   ├── ContactMessage.cs
│   │   ├── Enums.cs              # Toate enum-urile aplicației
│   │   ├── Favorite.cs
│   │   ├── MenuItem.cs
│   │   ├── MenuCategory.cs
│   │   ├── MenuItemChangeRequest.cs
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   ├── PromoCode.cs
│   │   ├── Restaurant.cs
│   │   ├── Review.cs
│   │   └── User.cs
│   ├── Services/
│   │   ├── IEmailService.cs
│   │   └── EmailService.cs       # SMTP fire-and-forget cu MailKit
│   ├── ViewModels/               # ViewModel pentru fiecare view complex
│   ├── Views/
│   │   ├── Account/              # Login, Register, RegisterRestaurant, Pending, Suspended
│   │   ├── Admin/                # Index, Restaurants, Users, Orders, ChangeRequests, PromoCodes, Messages
│   │   ├── Favorites/            # Index
│   │   ├── Home/                 # Index (pagina principală)
│   │   ├── Orders/               # Index (istoric), Details (tracking live)
│   │   ├── Pages/                # About, HowItWorks, BecomePartner, Faq, Contact, Terms, Privacy, PartnerRestaurants
│   │   ├── Profile/              # Index (editare profil client)
│   │   ├── Restaurant/           # Dashboard, Orders, Menu, ChangeRequests, Stats, Profile
│   │   ├── Restaurants/          # Detail (pagina publică restaurant)
│   │   └── Shared/               # _Layout.cshtml (navbar, SignalR, toast, chime)
│   ├── wwwroot/
│   │   ├── css/                  # site.css cu variabile CSS și stiluri custom
│   │   ├── img/
│   │   │   ├── menu/             # Imagini locale produse
│   │   │   └── restaurants/      # Logo-uri și coperte locale
│   │   └── js/                   # site.js
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── EatUp.csproj
│   └── Program.cs
└── PLAN.md                        # Document de planificare al proiectului
```

---

## 12. API Endpoints

### AccountController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/Account/Register` | — | Formular înregistrare client |
| POST | `/Account/Register` | — | Creează cont client |
| GET | `/Account/RegisterRestaurant` | — | Formular înregistrare restaurant |
| POST | `/Account/RegisterRestaurant` | — | Creează cont restaurant, trimite spre aprobare |
| GET | `/Account/Login` | — | Formular login |
| POST | `/Account/Login` | — | Autentificare, setare cookie |
| POST | `/Account/Logout` | — | Deconectare |
| GET | `/Account/Pending` | — | Pagina restaurant neaprobat |
| GET | `/Account/Suspended` | — | Pagina cont suspendat |
| GET | `/Account/RestaurantSuspended` | — | Pagina restaurant blocat |
| GET | `/Account/AccessDenied` | — | Pagina acces interzis |

### HomeController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/` sau `/Home/Index` | — | Pagina principală cu filtre restaurante |

### RestaurantsController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/restaurants/{id}` | — | Pagina publică restaurant |
| POST | `/restaurants/favorite/{id}` | CustomerOnly | Toggle favorite, returnează JSON |

### OrdersController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/orders` | CustomerOnly | Istoricul comenzilor |
| GET | `/orders/{id}` | CustomerOnly | Tracking comandă |
| POST | `/orders/{id}/cancel` | CustomerOnly | Anulare comandă |
| POST | `/orders/{id}/review` | CustomerOnly | Trimitere recenzie |

### CartController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/Cart/Index` | CustomerOnly | Pagina coș |
| POST | `/Cart/Add` | CustomerOnly | Adaugă produs (JSON) |
| POST | `/Cart/Clear` | CustomerOnly | Golește coșul |
| POST | `/Cart/Remove` | CustomerOnly | Elimină produs (JSON) |
| POST | `/Cart/UpdateQuantity` | CustomerOnly | Actualizează cantitate (JSON) |
| POST | `/Cart/ApplyPromo` | CustomerOnly | Aplică cod promo (JSON) |
| POST | `/Cart/CreatePaymentIntent` | CustomerOnly | Creează Stripe PaymentIntent (JSON) |
| POST | `/Cart/PlaceOrder` | CustomerOnly | Plasează comanda (JSON) |

### ProfileController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/Profile/Index` | CustomerOnly | Pagina profil |
| POST | `/Profile/SaveProfile` | CustomerOnly | Salvează modificările profil |
| POST | `/Profile/ChangePassword` | CustomerOnly | Schimbă parola |

### FavoritesController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/Favorites/Index` | CustomerOnly | Lista favorite |

### RestaurantController (panou restaurant)

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/Restaurant/Dashboard` | RestaurantOnly | Redirect la Stats sau Pending |
| GET | `/Restaurant/Orders` | RestaurantOnly | Panoul comenzi |
| GET | `/Restaurant/Menu` | RestaurantOnly | Gestionare meniu |
| GET | `/Restaurant/ChangeRequests` | RestaurantOnly | Cereri modificare |
| POST | `/Restaurant/AddCategory` | RestaurantOnly | Adaugă categorie (JSON) |
| POST | `/Restaurant/UpdateCategory` | RestaurantOnly | Redenumește categorie (JSON) |
| POST | `/Restaurant/DeleteCategory` | RestaurantOnly | Șterge categorie (JSON) |
| POST | `/Restaurant/ReorderCategories` | RestaurantOnly | Reordonare drag-drop (JSON) |
| POST | `/Restaurant/ToggleAvailability` | RestaurantOnly | Toggle disponibil (JSON) |
| POST | `/Restaurant/SubmitChangeRequest` | RestaurantOnly | Trimite cerere modificare (Form) |
| POST | `/Restaurant/RequestDelete` | RestaurantOnly | Cerere ștergere produs (JSON) |
| GET | `/Restaurant/Stats` | RestaurantOnly | Statistici restaurant |
| GET | `/Restaurant/Profile` | RestaurantOnly | Profil restaurant |
| POST | `/Restaurant/SaveProfile` | RestaurantOnly | Salvează profil restaurant |

### AdminController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/Admin/Index` | AdminOnly | Dashboard admin |
| GET | `/Admin/Restaurants` | AdminOnly | Gestionare restaurante |
| POST | `/Admin/ApproveRestaurant` | AdminOnly | Aprobă restaurant |
| POST | `/Admin/RejectRestaurant` | AdminOnly | Respinge restaurant |
| POST | `/Admin/BlockRestaurant` | AdminOnly | Blochează/deblochează restaurant |
| GET | `/Admin/Users` | AdminOnly | Gestionare utilizatori |
| POST | `/Admin/BlockUser` | AdminOnly | Blochează/deblochează utilizator |
| GET | `/Admin/Orders` | AdminOnly | Vizualizare comenzi cu filtre |
| GET | `/Admin/OrderDetail` | AdminOnly | Detalii comandă (partial view JSON) |
| GET | `/Admin/ChangeRequests` | AdminOnly | Cereri modificare meniu |
| POST | `/Admin/ApproveChangeRequest` | AdminOnly | Aprobă cerere |
| POST | `/Admin/RejectChangeRequest` | AdminOnly | Respinge cerere |
| GET | `/Admin/PromoCodes` | AdminOnly | Gestionare coduri promo |
| POST | `/Admin/CreatePromoCode` | AdminOnly | Creează cod promo |
| POST | `/Admin/TogglePromoCode` | AdminOnly | Activează/dezactivează cod |
| POST | `/Admin/DeletePromoCode` | AdminOnly | Șterge cod promo |
| GET | `/Admin/Messages` | AdminOnly | Mesaje contact |
| POST | `/Admin/MarkAsRead` | AdminOnly | Marchează mesaj citit (JSON) |
| POST | `/Admin/MarkAsReplied` | AdminOnly | Marchează răspuns trimis (JSON) |

### PagesController

| Metodă | Rută | Autorizare | Descriere |
|---|---|---|---|
| GET | `/about` | — | Despre EatUp |
| GET | `/how-it-works` | — | Cum funcționează |
| GET | `/become-partner` | — | Devino partener |
| GET | `/faq` | — | Întrebări frecvente |
| GET | `/contact` | — | Pagina contact |
| POST | `/contact` | — | Trimitere mesaj contact (JSON) |
| GET | `/terms` | — | Termeni și condiții |
| GET | `/privacy` | — | Politica de confidențialitate |
| GET | `/partner-restaurants` | — | Toate restaurantele partenere |

---

## 13. Funcționalități timp real — SignalR

### Configurare

Hub-ul `OrderHub` este declarat în `Hubs/OrderHub.cs` și montat în `Program.cs` la ruta `/hubs/orders`. Conexiunea SignalR este inițiată din `Views/Shared/_Layout.cshtml` imediat după autentificare și se menține pe toate paginile aplicației.

### Grupuri de notificare

| Grup | Membrii | Scop |
|---|---|---|
| `restaurant-{id}` | Manager restaurant | Comenzi noi, actualizări status, anulări client |
| `order-{id}` | Clientul care urmărește comanda | Schimbări status, poziție curier |
| `customer-{id}` | Clientul (toate paginile) | Actualizări live pe pagina de istoric |

### Evenimente — Client → Server (apeluri hub)

| Metodă hub | Parametri | Descriere |
|---|---|---|
| `JoinRestaurantGroup(restaurantId)` | int | Managerul se abonează la grupul restaurantului |
| `JoinOrderGroup(orderId)` | int | Clientul se abonează la trackingul unei comenzi |
| `JoinCustomerGroup(customerId)` | int | Clientul se abonează la notificările personale |
| `UpdateOrderStatus(orderId, newStatus)` | int, string | Restaurantul avansează statusul comenzii |
| `RejectOrder(orderId, reason)` | int, string | Restaurantul respinge comanda cu motiv |

### Evenimente — Server → Client (broadcast)

| Eveniment | Destinatar | Parametri | Descriere |
|---|---|---|---|
| `order_incoming` | `restaurant-{id}` | orderId, orderNumber, html | Comandă nouă sosită (cardul HTML al comenzii) |
| `order_updated` | `restaurant-{id}` | orderId, newStatus | Actualizare status (pentru alte tab-uri ale restaurantului) |
| `order_cancelled` | `restaurant-{id}` | orderId, orderNumber | Clientul a anulat comanda |
| `order_status_changed` | `order-{id}` și `customer-{id}` | orderId, newStatus | Status nou (pentru clientul care urmărește) |
| `courier_location` | `order-{id}` | lat, lng | Poziția curierului la fiecare secundă |

### Simularea curierului

Când restaurantul marchează o comandă **Out for Delivery**, hub-ul pornește `SimulateCourierAsync` într-un task background (`Task.Run`). Simularea:
- Durează **90 de secunde** (90 pași × 1 secundă/pas)
- Interpolează poziția GPS de la restaurat la adresa clientului cu funcția **smoothstep** (`t² × (3 - 2t)`) pentru mișcare naturală
- La fiecare pas, salvează `CourierLat`/`CourierLng` în baza de date și emite `courier_location`
- La final, marchează automat comanda **Delivered** și trimite toate notificările aferente
- Dacă restaurantul marchează manual Delivered înainte de final, simularea se oprește (gardă pe status)

---

## 14. Capturi de ecran

> *Capturile de ecran vor fi adăugate manual*

Capturi de ecran recomandate pentru documentație:

1. **Pagina principală** — gridul de restaurante cu filtre de categorie și oraș
2. **Pagina restaurant** — hero cu cover, meniu pe categorii, secțiunea recenzii
3. **Pagina coș** — produse, cod promo aplicat, harta locației de livrare, opțiunile de plată
4. **Tracking comandă** — cronologie statusuri, harta cu marker curier animat
5. **Panoul comenzi restaurant** — carduri comenzi active cu butoane de avansare status
6. **Gestionare meniu restaurant** — categorii drag-and-drop, produse cu toggle disponibilitate
7. **Statistici restaurant** — grafice Chart.js comenzi și venituri
8. **Dashboard admin** — KPI cards, grafice activitate platformă
9. **Gestionare restaurante admin** — tab-uri pending / toate
10. **Mesaje de contact admin** — tabelul cu mesaje și modalul de răspuns

---

## 15. Limitări cunoscute

| Limitare | Detalii |
|---|---|
| **Tracking curier simulat** | Nu există integrare GPS real. Curierul se deplasează matematic pe linie dreaptă de la restaurant la client în 90 secunde, indiferent de rute reale. |
| **Email manual** | Notificările email necesită configurarea unui cont Gmail cu App Password. Fără configurare, email-urile sunt ignorate silențios, aplicația funcționează normal. |
| **Stripe în modul test** | Plățile cu cardul nu sunt reale. Se folosesc chei de test Stripe și carduri de test. Nicio sumă reală nu este procesată. |
| **Notificări browser** | Nu există Push Notifications native. Sunetele și toast-urile funcționează doar dacă tab-ul aplicației este deschis în browser. |
| **Imagini Unsplash** | Unele imagini provin de la Unsplash și necesită conexiune la internet pentru încărcare. Imaginile locale din `wwwroot/img/` sunt independente de internet. |
| **Sesiune coș** | Coșul de cumpărături este stocat în sesiunea server-side (in-memory). La restartul aplicației, coșurile active se pierd. |
| **Single-server** | Aplicația nu este configurată pentru deployment multi-instanță. SignalR folosește memoria locală, fără backplane Redis. |
| **Fără OAuth** | Autentificarea este exclusiv prin email + parolă. Nu există login cu Google, Facebook etc. |

---

## 16. Autor

**Vlad-Vasile Banță**

Universitatea din Craiova  
Facultatea de Automatică, Calculatoare și Electronică  
Specializarea: Calculatoare și Tehnologia Informației  
An universitar: 2025–2026

**Coordonator științific:** *[se completează manual]*

---

*Proiect dezvoltat ca lucrare de licență. Toate datele de test (restaurante, meniuri, recenzii) sunt fictive și au scop demonstrativ.*
