# AutoParts — SkyNet Manufacturing Company Template

A responsive website for a parts manufacturer (suspension: coilovers, struts, shocks, control arms, sway bars, bushings, leaf springs, lift kits, air suspension): a product catalog with a vehicle fitment finder, compare, quote requests, a dealer locator, capabilities, industries, news, careers with online applications and a single-user admin with a block page editor and a media library, built for **ASP.NET Core (.NET 10)** on the **SkyNet Framework**.
It is universal: rename the company, set your town, units, warranty, vehicle makes and models and office hours, and the whole site follows. Data is stored in JSON files, with no database.
Free and open source. **100% AI-driven coding — built by Claude.**

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/TheSkyLite.SkyNet?label=TheSkyLite.SkyNet)](https://www.nuget.org/packages/TheSkyLite.SkyNet)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![AI-driven](https://img.shields.io/badge/code-100%25%20AI--driven-9A5B2E)](#100-ai-driven-coding)

**Live demo:** https://www.theskylite.com/AutoParts

![AutoParts home page](docs/screenshot-home.png)

> **Fictional company.** Ironvale Suspension Works, the town of Dutton Falls, the vehicle makes Arden, Halcyon, Kestrel and Voss and every product, dealer, person, price and address are invented. The product photos, plant scenes, portraits, sample videos and PDF documents are made in code. There are no stock images, no real part numbers and no payment service.

---

## Features

### Company website (10 pages)

- **Every page is built from blocks** that you edit in **Pages**: page top, text, picture and text, cards, steps, numbers, people, boxes, questions, quote, picture or video, call to action, plus the blocks filled from the data (product families, featured products, vehicle finder, dealers, news, jobs, forms).
- **Home:**
  - a hero with a background **video**, the **vehicle fitment finder** (year, make, model) and live counts: products in the catalog, dealers and installers, years in business;
  - nine product families with live counts, featured products, why-us cards, industries served, the latest news and a quote band.
- **Open now / closed now** bar on every page, worked out in the company's own time zone from the office hours.
- **Products:**
  - the vehicle finder, search by name or part number, category, series (street, track, off-road, heavy duty), price range, in stock only;
  - sort by newest, price, name or most viewed; grid or list view; paging; the URL keeps the search, so a filtered catalog can be shared;
  - cards show the fitment line, price (MSRP), stock (in stock, low stock, built to order, out of stock) and Featured.
- **Compare up to three products** side by side: price, category, series, availability, warranty, fitment, install time and every specification, with US units and metric beside them. Compared products stay in the browser (no account), with a count in the header.
- **Product page:**
  - a photo and **video** gallery with a full-screen **lightbox** (arrow keys, swipe, thumbnails);
  - price, stock and lead time, warranty, features, **specifications in US units with metric in brackets** (or metric first, or US only, from Settings), the **fitment table** by make, model, years and position, what is in the box, installation time and torque, **PDF downloads** (installation guides, spec sheets, catalog);
  - **Request a quote** (quantity, vehicle, state), **Ask about this part**, **Find a dealer** for this part number, **Compare** and **Share**; related products.
- **Capabilities:** forging, machining, heat treatment, coating, assembly and the test lab, by the numbers, certifications and the OEM supply program.
- **Industries:** performance aftermarket, fleet and municipal, off-road and overland, OEM supply, each with its own program.
- **Dealers:** a **tile map of the United States** with dealer counts per state; tap a state to filter; search by shop name or city, filter by type (retail store, installer, distributor, online store) and service; Premier dealers first; paging; **Become a dealer** inquiry form.
- **News:** product launches, tech notes, plant news, trade shows and community posts with a tag filter, paging and full articles.
- **Careers:** open positions by department with pay, shift and type; the job page has the full description and an **application form with a résumé upload** (PDF or Word, 5 MB), saved on the server and downloadable in the admin.
- **About:** the story, the timeline, the leadership team and certifications.
- **Contact:** address, phone, office hours and three forms in tabs: a **multi-line quote request** (several part numbers with quantities, vehicle, state), a message with topics and a **warranty claim** (part number, purchase date, dealer, photos by email). **Product news by email** sign-up in every footer.
- **Light and dark theme.** Responsive layouts for desktop, tablet and phone.

### Admin (11 pages + sign-in)

- **One admin account.** Sign in with the sample account (filled in on the sign-in page) or your own password (PBKDF2). Failed attempts are throttled; sessions last 8 hours and renew while you work.
- **Dashboard:** new and open quote requests with their value, won this month, messages, live products, applications; the newest quote requests, what needs attention (unanswered inquiries, new warranty claims, stale quotes, products out of stock or low, drafts, new applications), most viewed products and quote requests by category for the last 90 days.
- **Products:** search, filters by category, series, status and stock, sort and paging. The editor has tabs:
  - **Details:** name, part number, category, series, price, stock, lead time, warranty, status (draft, live, retired), featured, shown in the catalog;
  - **Text:** summary, description, features, what is in the box, installation time and torque;
  - **Specs:** rows with name, value and unit (in, lb, psi, lb/in, ft-lb, degrees or free text), converted to metric on the website;
  - **Fitment:** make, model, years and position per row, from the makes and models in Settings;
  - **Media:** photos and videos from Media (upload new ones right in the picker), reorder, the first photo is the cover; PDF documents from Media;
  - **History** of status and price changes (added automatically when you save).
- **Quotes & inquiries:** every quote request, product question, dealer inquiry, warranty claim and message (30 samples). Filters by kind, status, period and text; the quote modal shows the parts with catalog prices and total, the message, the vehicle and the notes. Set the status (New, Quoted with the quoted amount, Won, Lost, Closed), reply by email, add notes, **Export CSV**.
- **Dealers:** every shop with type, tier (authorized, premier), services, hours and contact; filter by state, type and shown; add, edit, deactivate.
- **News:** posts with a cover picture from Media, tag, publish date and views; drafts stay off the website.
- **Careers:** positions (department, type, shift, pay, description, open or closed) and **applications** with the résumé download, status (New, Reviewing, Interview, Offer, Hired, Declined) and notes.
- **Pages:** edit every public page in place, the way it looks:
  - click any text to change it; bold, italic, links and lists in the longer texts; `{name}` and `{town}` show the values from Settings;
  - change a picture or video from Media; drag blocks by the handle or use the arrows; hide a block without deleting it;
  - add blocks (page top, text, picture and text, cards, steps, numbers, people, boxes, questions, quote, picture or video, call to action), add, move and remove cards, steps, people and questions;
  - blocks filled from the data (products, dealers, news, jobs, forms) can be moved, retitled or hidden but not removed;
  - **Save page** puts it live; the last five versions are kept and can be restored.
- **Media:** one library for photos, videos and PDF documents (240 samples):
  - upload photos (resized in the browser to 1600 px), videos (MP4 or WebM up to 60 MB, sent in 1 MB parts, with a cover frame taken in the browser) and PDFs (up to 20 MB, with a cover picture of the first page);
  - name, description for screen readers and tags; search, filter by kind, tag and used or not;
  - **Used on** shows the products, posts and pages that use each item; items in use cannot be deleted.
- **Settings:** company details, accent color, theme, founded year, **units on the website** (US with metric beside, metric with US beside, US only), standard warranty, warranty and quote texts, shipping line, certifications, **vehicle makes and models** with body style and model years for the fitment finder, finder years, office hours, time zone and 24-hour clock.
- **Activity log** of everything the admin and the website do, including refused sign-ins, and **My account** (name, title, user name, password).
- **Every list** has a search box, filters and paging.

### Sign in

| User name | Password |
|---|---|
| `admin` | `Ironvale2026` |

- The sign-in page fills in the sample account and shows the password below the button.
- Change the password in **My account**: the filled-in fields and the hint go away.
- Deleting `data/admin.json` brings back the sample sign-in.

![Admin sign in](docs/screenshot-admin-login.png)

![Desktop, tablet and phone](docs/screenshot-responsive.png)

![Products](docs/screenshot-products.png)

![Product page](docs/screenshot-product.png)

![Specifications and fitment](docs/screenshot-specs.png)

![Compare](docs/screenshot-compare.png)

![Dealers](docs/screenshot-dealers.png)

![Capabilities](docs/screenshot-capabilities.png)

![Careers: apply for a position](docs/screenshot-careers.png)

![Contact: quote request](docs/screenshot-quote.png)

![Dashboard](docs/screenshot-admin-dashboard.png)

![Products](docs/screenshot-admin-products.png)

![Product editor: fitment](docs/screenshot-admin-product.png)

![Product editor: photos, videos and documents](docs/screenshot-admin-product-media.png)

![Quotes & inquiries](docs/screenshot-admin-quotes.png)

![A quote request](docs/screenshot-admin-quote.png)

![Dealers](docs/screenshot-admin-dealers.png)

![News](docs/screenshot-admin-news.png)

![Careers: applications](docs/screenshot-admin-careers.png)

![Pages: edit the website in place](docs/screenshot-admin-pages.png)

![Media library](docs/screenshot-admin-media.png)

![Settings: vehicle makes and models](docs/screenshot-admin-settings.png)

![Activity log](docs/screenshot-admin-activity.png)

- **Painted artwork:** 66 products in 3 views each, 27 plant and shop scenes and 6 portraits, rendered in code by Claude as 467 WebP files, plus 3 short sample videos made from the paintings and 6 sample PDF documents.
- **No payment service:** the website takes quote requests, not orders, so there are no card details, keys or fees to manage.
- **No front-end build:** plain HTML, CSS and vanilla JavaScript; no npm, bundler or SPA framework.
- **No database:** everything lives in JSON files under `data/`.

---

## Getting started

**Requirements:** .NET 10 SDK and Visual Studio (or any editor with the `dotnet` CLI).

```bash
git clone https://github.com/hkim6000/SkyNet-AutoPartsWebsite-Asp.net.Core-Full-Source.git
cd SkyNet-AutoPartsWebsite-Asp.net.Core-Full-Source
dotnet run
```

Or open `AutoParts.csproj` in Visual Studio and press **F5**.
The SkyNet package (`TheSkyLite.SkyNet` 1.0.6) restores automatically from NuGet.
The app opens on **Home**, the startup page set in `appConfig/application.cfg`.

**Admin:** click **Admin sign in** in the footer (or open `/AdminLogin`) and press **Sign in**. See [Sign in](#sign-in).

**Dates:** the sample quotes, applications, posts and product history carry dates relative to the day you first run the site, so "new" badges, the dashboard and the 90-day chart look right. This happens once; after that dates stay as they are.

---

## Make it yours

1. **Settings:** name, town, address, phone, email, legal line, accent color, units, warranty, shipping line, certifications, your vehicle makes and models, office hours and time zone.
2. **Pages:** rewrite the text of every page, change the pictures and videos, reorder or hide blocks and add your own.
3. **Media:** upload your own photos, videos and PDFs, then delete the samples you no longer use.
4. **Products:** replace the sample products with your own. Add photos and documents from Media, fill in specifications and fitment, and set the status to Live.
5. **Dealers, News, Careers:** add your shops, posts and open positions.
6. **Sample people:** delete the sample quote requests, inquiries and applications in the admin, or start from a `data/site.json` with those lists empty.
7. **Files:** uploaded photos go to `images/uploads/full` (1600 px) and `images/uploads/thumbs` (640 px), videos to `images/uploads/video` and PDFs to `images/uploads/docs` with their covers in `images/uploads/thumbs`; résumés go to `data/files/resumes`. The sample product photos live in `images/products/full` and `images/products/thumbs`, the scenes in `images/scenes`, the portraits in `images/people`, the sample videos in `images/media` and the sample PDFs in `images/docs`.

---

## Going live

1. **Change the sample password** in My account.
2. **Give the site write access** to these folders. On IIS, grant the application pool identity (for example `IIS AppPool\AutoParts`) **Modify** permission on:

   | Folder | What is written |
   |---|---|
   | `data/` | `site.json`, `admin.json`, `activity.json`, `subscribers.json`, résumés in `data/files/resumes` |
   | `images/uploads/` | uploaded photos, videos and PDFs |
   | `temp/` | the admin session (`temp/admin`) and uploads in progress (`temp/uploads`) |

3. **Block private files.** Open `https://<your-site>/data/admin.json` in a browser: it must answer 404. If your host serves it, block the `data/` folder in the server settings. Résumés are served only to the signed-in admin.
4. **Use HTTPS.** The session token travels in a cookie and in each request.
5. **Never commit** your live `data/admin.json`, `data/activity.json`, `data/subscribers.json`, uploaded résumés or `temp/`.
6. **Check your local rules** for storing the contact details of customers, dealers and job applicants.

> **Redeploying:** publishing copies the files in `data/` from your project over the live ones. Download the live `data/` folder first (or exclude it from the publish) so you keep your products and quotes. Turn off "Remove additional files at destination" so `temp/` and `images/uploads/` are not deleted.

Changing the password signs the admin out everywhere.

---

## Project structure

```
AutoParts/
├── appConfig/application.cfg        app settings
├── codes/
│   ├── Models/AutoPartsModel.cs     data DTOs (Settings, Make, Part, SpecRow, Fitment, Dealer, Inquiry, Post, Job, JobApp, MediaItem, PageDoc, Block, ...)
│   ├── Core/Store.cs                JSON storage, sample account, time zone, one-time date shift, résumé files
│   ├── Core/Access.cs               areas, passwords
│   ├── Core/Auth.cs                 the admin session, sign-in throttle, admin menu
│   ├── Core/Ui.cs                   shared HTML helpers, option lists, unit conversion, money, media, period filter, paging, CSV
│   ├── Core/Blocks.cs               page blocks: the catalog, text cleaning, checks before saving, where media is used
│   ├── Home.cs  Products.cs  Product.cs  Capabilities.cs  Industries.cs  Dealers.cs  News.cs  Careers.cs  About.cs  Contact.cs
│   └── AdminLogin.cs  Admin.cs  AdminProducts.cs  AdminQuotes.cs  AdminDealers.cs  AdminNews.cs  AdminCareers.cs
│       AdminPages.cs  AdminMedia.cs  AdminSettings.cs  AdminActivity.cs  AdminAccount.cs
├── htmls/  scripts/  styles/        page markup, JavaScript and CSS
├── data/
│   ├── site.json                    settings, makes and models, products, dealers, inquiries, posts, jobs and applications, media, page blocks
│   ├── admin.json                   the admin account and password hash (created on the first run)
│   ├── activity.json                the activity log (latest 5,000)
│   ├── subscribers.json             news-by-email sign-ups
│   └── files/resumes/               uploaded résumés (14 samples)
├── images/products/full/  thumbs/   painted product photos (1600 px and 640 px, WebP)
├── images/scenes/                   painted plant and shop scenes (WebP)
├── images/people/                   painted portraits (WebP)
├── images/media/                    sample videos (MP4) and their covers
├── images/docs/                     sample PDF documents and their covers
├── images/uploads/                  photos, videos and PDFs uploaded in the admin (created at run time)
├── temp/                            the admin session and uploads in progress (created at run time)
└── Program.cs
```

### One page = four files, one name

| File | Holds |
|---|---|
| `codes/Products.cs` | the page class (`: WebPage`) and its server methods |
| `htmls/Products.html` | markup with `{plhd_*}` placeholders |
| `scripts/Products.js` | one IIFE namespace, `ProductsJs` |
| `styles/Products.css` | styles, every class prefixed (`pr-`) |

---

## SkyNet in action

The browser calls a C# method, the method returns an `ApiResponse`, and only those parts of the page change.

```js
// scripts/Products.js
curPage = 1;
url();
call('Search', args());
```

```csharp
// codes/Products.cs
Filter f = Read(site, k => GetDataValue(k));
response.SetElementContents("pr-results", Results(site, f, Store.Today(site)));
response.ExecuteScript("ProductsJs.shown();");
```

| Page | Requests |
|---|---|
| every public page | `Subscribe`, `Compared` |
| Products | `Search`, `Compare` |
| Product | `Send`, `QuoteSend` |
| Dealers | `Find`, `DealerSend` |
| News / Careers | `Filter` / `Filter`, `Apply` |
| Contact | `QuoteSend`, `MsgSend`, `WarSend` |
| every admin page | `Load`, `SignOut` (AdminLogin: `SignIn`) |
| AdminProducts | `List`, `Form`, `Save`, `Delete`, `MediaPick`, `MediaUp` |
| AdminQuotes | `List`, `Open`, `Status`, `Note`, `Delete`, `Csv` |
| AdminDealers | `List`, `Form`, `Save`, `Delete` |
| AdminNews | `List`, `Form`, `Save`, `Delete`, `MediaPick`, `MediaUp` |
| AdminCareers | `List`, `Apps`, `Form`, `Save`, `Delete`, `Open`, `Status`, `Note`, `Resume`, `DeleteApp` |
| AdminPages | `Show`, `AddBlock`, `Save`, `Versions`, `Restore`, `MediaPick`, `MediaUp` |
| AdminMedia | `List`, `Open`, `SaveInfo`, `Upload`, `Chunk`, `VideoDone`, `DocDone`, `Remove` |
| AdminSettings / AdminAccount / AdminActivity | `Save` / `Save` / `List` |

Learn more: [SkyNet Developer Guide](https://www.theskylite.com/documents/SkyNet_Developer_Guide.html)

---

## 100% AI-driven coding

Every file in this template was generated by Claude (Anthropic's AI) from short instructions, directed and reviewed by the author: C#, HTML, CSS, JavaScript, the painted product photos, scenes and portraits, the sample videos and PDFs, and the data.
No line was written by hand.

| | |
|---|---|
| Pages | 22 (10 public, 11 admin + sign-in) |
| Lines of code | ~29,000 (C#, HTML, CSS, JavaScript) |
| Images, video and documents | 66 products × 3 views, 27 scenes and 6 portraits (467 WebP), 3 sample videos, 6 sample PDFs |
| Sample products | 66 (63 live, 2 drafts, 1 retired) in 9 families, fitted to 4 makes and 20 models |
| Sample dealers / posts / positions | 40 / 13 / 8 |
| Sample quotes, inquiries / applications | 30 / 14 |
| Media library / editable pages | 240 items / 8 pages, 38 blocks |
| Lines written by hand | 0 |

SkyNet's page model is small and predictable: one class, four files, `$ApiRequest` → `ApiResponse`.
With so few, consistent rules, AI can generate complete, working pages with very few errors.

---

## License

- **This template** (all source files, artwork and data in this repository): [MIT License](LICENSE). Free to use, modify and redistribute, including commercially.
- **SkyNet Framework** (`TheSkyLite.SkyNet` NuGet package): proprietary, free to use including commercial use; see the license included in the package.

---

## Links

- Live demo: https://www.theskylite.com/AutoParts
- SkyNet Framework: https://www.theskylite.com
- More website templates: https://www.theskylite.com/SkyNetTemp
- NuGet package: https://www.nuget.org/packages/TheSkyLite.SkyNet
- SkyNet Agent (build your own site with Claude Code): https://github.com/hkim6000/SkyNet-Agent
- Template #1 — Beauty store: https://github.com/hkim6000/SkyNet-BeautyWebsite-Asp.net.Core-Full-Source
- Template #2 — Pharma company: https://github.com/hkim6000/SkyNet-PharmaWebsite-Asp.net.Core-Full-Source
- Template #3 — Travel blog: https://github.com/hkim6000/SkyNet-TravelWebsite-Asp.net.Core-Full-Source
- Template #4 — Fast food restaurant: https://github.com/hkim6000/SkyNet-FastFoodWebsite-Asp.net.Core-Full-Source
- Template #5 — Fine dining restaurant: https://github.com/hkim6000/SkyNet-FineDiningWebsite-Asp.net.Core-Full-Source
- Template #6 — Car dealership: https://github.com/hkim6000/SkyNet-CarDealerWebsite-Asp.net.Core-Full-Source
- Template #7 — Web comic: https://github.com/hkim6000/SkyNet-WebComicWebsite-Asp.net.Core-Full-Source
- Template #8 — Art gallery: https://github.com/hkim6000/SkyNet-ArtGalleryWebsite-Asp.net.Core-Full-Source
- Template #9 — Artist blog: https://github.com/hkim6000/SkyNet-ArtistBlogWebsite-Asp.net.Core-Full-Source
- Template #10 — Newspaper: https://github.com/hkim6000/SkyNet-SkyLitePostWebsite-Asp.net.Core-Full-Source
- Template #11 — Hotel: https://github.com/hkim6000/SkyNet-SkyHotelWebsite-Asp.net.Core-Full-Source
- Template #12 — Clinic: https://github.com/hkim6000/SkyNet-ClinicWebsite-Asp.net.Core-Full-Source
- Template #13 — Real estate agency: https://github.com/hkim6000/SkyNet-RealEstateWebsite-Asp.net.Core-Full-Source
- Template #14 — Dog shelter: https://github.com/hkim6000/SkyNet-DogShelterWebsite-Asp.net.Core-Full-Source
- SkyNet project template: https://github.com/hkim6000/ASPNETCoreEmpty.SkyNet

© 2026 HC Kim
