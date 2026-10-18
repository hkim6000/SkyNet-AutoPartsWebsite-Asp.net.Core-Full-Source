using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class Products : WebPage
    {
        private sealed class View
        {
            public string Title { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
            public string Main { get; set; } = string.Empty;
            public string Nav { get; set; } = string.Empty;
        }

        private string Q(string key, int max = 80)
        {
            return Ui.Clip(QueryValue(key), max);
        }

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            View v = await Render(site);
            HtmlDoc.SetTitle((v.Title != string.Empty ? v.Title + " | " : string.Empty) + site.Settings.Name);
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", v.Desc != string.Empty ? v.Desc : site.Settings.Tagline);
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_name}", Ui.Enc(site.Settings.Name))
                .Replace("{plhd_town}", Ui.Enc(site.Settings.Town))
                .Replace("{plhd_tagline}", Ui.Enc(site.Settings.Tagline))
                .Replace("{plhd_addr}", Ui.Enc(site.Settings.Address))
                .Replace("{plhd_phone}", Ui.Enc(site.Settings.Phone))
                .Replace("{plhd_tel}", Ui.Attr(Regex.Replace(site.Settings.Phone, "[^0-9+]", string.Empty)))
                .Replace("{plhd_email}", Ui.Enc(site.Settings.Email))
                .Replace("{plhd_legal}", Ui.Enc(site.Settings.Legal))
                .Replace("{plhd_today}", TodayLine(site))
                .Replace("{plhd_theme}", site.Settings.Theme == "dark" ? "dark" : "light")
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--pr-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_nav}", Nav("pr", v.Nav, false))
                .Replace("{plhd_drawer}", Nav("pr", v.Nav, true))
                .Replace("{plhd_main}", v.Main);
        }

        private static string Nav(string p, string on, bool drawer)
        {
            (string Href, string Key, string Name)[] links = { ("Products", "products", "Products"), ("Capabilities", "capabilities", "Capabilities"), ("Industries", "industries", "Industries"), ("Dealers", "dealers", "Dealers"), ("News", "news", "News"), ("Careers", "careers", "Careers"), ("About", "about", "About"), ("Contact", "contact", "Contact") };
            StringBuilder sb = new StringBuilder();
            if (drawer)
            {
                sb.Append("<a href=\"Home\"" + (on == "home" ? " class=\"" + p + "-on\"" : string.Empty) + ">Home</a>");
            }
            foreach ((string href, string k, string n) in links)
            {
                sb.Append("<a href=\"" + href + "\"" + (k == on ? " class=\"" + p + "-on\" aria-current=\"page\"" : string.Empty) + ">" + Ui.Enc(n) + "</a>");
            }
            if (drawer)
            {
                sb.Append("<a href=\"Products?compare=1\">Compare products</a>");
            }
            return sb.ToString();
        }

        private static string TodayLine(SiteData site)
        {
            DateTime now = Store.Now(site);
            OpenDay? d = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)now.DayOfWeek);
            TimeSpan? from = d != null && d.Open ? Store.Hm(d.Start) : null;
            TimeSpan? to = d != null && d.Open ? Store.Hm(d.End) : null;
            if (from != null && to != null && now.TimeOfDay >= from.Value && now.TimeOfDay < to.Value)
            {
                return "<i></i>Fitment desk open until " + Ui.HmText(site, d!.End);
            }
            if (from != null && now.TimeOfDay < from.Value)
            {
                return "<i class=\"pr-shut\"></i>Fitment desk opens today at " + Ui.HmText(site, d!.Start);
            }
            for (int i = 1; i <= 7; i++)
            {
                DateTime n = now.Date.AddDays(i);
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)n.DayOfWeek);
                if (o != null && o.Open)
                {
                    return "<i class=\"pr-shut\"></i>Closed now &middot; opens " + (i == 1 ? "tomorrow" : Ui.DayNames[(int)n.DayOfWeek]) + " at " + Ui.HmText(site, o.Start);
                }
            }
            return "<i class=\"pr-shut\"></i>Closed";
        }

        private static string HoursRows(SiteData site, string p)
        {
            int today = (int)Store.Today(site).DayOfWeek;
            StringBuilder sb = new StringBuilder();
            foreach (int d in new[] { 1, 2, 3, 4, 5, 6, 0 })
            {
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == d);
                string t = o != null && o.Open ? Ui.HmText(site, o.Start) + " &ndash; " + Ui.HmText(site, o.End) : "Closed";
                sb.Append("<span class=\"" + p + "-hrow" + (d == today ? " " + p + "-now" : string.Empty) + "\"><span>" + Ui.DayNames[d] + "</span><span>" + t + "</span></span>");
            }
            return sb.ToString();
        }

        public async Task<ApiResponse> Subscribe()
        {
            ApiResponse response = new ApiResponse();
            string mail = Ui.Clip(GetDataValue("nlmail"), 120);
            if (!Ui.Email(mail))
            {
                response.SetElementContents("pr-e-nlmail", "Enter an email address like name@example.com.");
                return response;
            }
            bool added = await Store.AddSubscriber(mail);
            response.ExecuteScript("ProductsJs.subscribed('" + (added ? "Thanks. Product news lands about once a month." : "You are already on the list.") + "');");
            return response;
        }

        public async Task<ApiResponse> Compared()
        {
            ApiResponse response = new ApiResponse();
            string id = Ui.Clip(GetDataValue("cid"), 20);
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Part? d = site.Products.FirstOrDefault(x => x.Id == id && Ui.Public(x));
                if (d != null)
                {
                    d.Compares++;
                    await Store.SaveSite(site);
                }
            }
            return response;
        }

        private static string FitSummary(SiteData site, Part p)
        {
            if (p.Fitments.Count == 0)
            {
                return "Universal";
            }
            Fitment f = p.Fitments[0];
            string s = "Fits " + Ui.MakeName(site, f.Make) + " " + Ui.ModelName(site, f.Make, f.Model) + " " + f.From.ToString(Ui.Inv) + "&ndash;" + f.To.ToString(Ui.Inv);
            return p.Fitments.Count > 1 ? s + " +" + (p.Fitments.Count - 1) : s;
        }

        private static (string Cls, string Text) Badge(Part p, DateTime today)
        {
            if (p.Stock == "out")
            {
                return ("out", "Out of stock");
            }
            if (p.Stock == "order")
            {
                return ("order", "Built to order");
            }
            if (p.Featured)
            {
                return ("feat", "Featured");
            }
            if (p.Listed != null && p.Listed.Value.Date >= today.AddDays(-45))
            {
                return ("new", "New");
            }
            return (string.Empty, string.Empty);
        }

        private static string Card(SiteData site, Part p, DateTime today)
        {
            string href = "Product?id=" + Uri.EscapeDataString(p.Id);
            (string cls, string text) = Badge(p, today);
            return "<article class=\"pr-pc\" data-pid=\"" + Ui.Attr(p.Id) + "\"><a class=\"pr-pc-img\" href=\"" + href + "\" tabindex=\"-1\" aria-hidden=\"true\">" + Ui.Pic(Ui.Cover(site, p), string.Empty, string.Empty, true) + (text != string.Empty ? "<span class=\"pr-badge pr-b-" + cls + "\">" + text + "</span>" : string.Empty) + "</a><button type=\"button\" class=\"pr-cmpb\" data-cmp=\"" + Ui.Attr(p.Id) + "\" aria-pressed=\"false\" onclick=\"ProductsJs.cmp(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 4v16M15 4v16\"/><path d=\"M4 9h5M15 9h5M4 15h5M15 15h5\"/></svg><span>Compare</span></button><div class=\"pr-pc-b\"><span class=\"pr-pcat\"><span>" + Ui.Enc(Ui.CategoryName(p.Category)) + "</span><em>" + Ui.Enc(Ui.SeriesName(p.Series)) + "</em></span><span class=\"pr-pname\"><a href=\"" + href + "\">" + Ui.Enc(p.Name) + "</a></span><span class=\"pr-pfit\">" + FitSummary(site, p) + "</span><span class=\"pr-pc-f\"><span class=\"pr-price\">" + Ui.Money(p.Price) + "<small>MSRP</small></span>" + Ui.Pill("pr", Ui.Stocks, p.Stock) + "</span></div></article>";
        }

        private const int Per = 12;

        private sealed class Filter
        {
            public string Q { get; set; } = string.Empty;
            public string Cat { get; set; } = string.Empty;
            public string Series { get; set; } = string.Empty;
            public string Make { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public int Year { get; set; }
            public decimal Min { get; set; }
            public decimal Max { get; set; }
            public bool InStock { get; set; }
            public string Sort { get; set; } = "new";
            public string ViewMode { get; set; } = "grid";
            public int Page { get; set; } = 1;
        }

        private static readonly (string Key, string Name)[] Sorts = { ("new", "Newest first"), ("popular", "Most viewed"), ("price", "Price, low to high"), ("priceDesc", "Price, high to low"), ("name", "Name, A to Z") };

        private static decimal Dec(string v)
        {
            return decimal.TryParse(Regex.Replace(v ?? string.Empty, "[$,\\s]", string.Empty), NumberStyles.Number, Ui.Inv, out decimal d) ? Math.Clamp(d, 0, 100000) : 0;
        }

        private static Filter Read(SiteData site, Func<string, string> get)
        {
            Filter f = new Filter
            {
                Q = Ui.Clip(get("q"), 40),
                Cat = Ui.Has(Ui.Categories, get("cat")) ? get("cat") : string.Empty,
                Series = Ui.Has(Ui.Series, get("series")) ? get("series") : string.Empty,
                Make = site.Makes.Any(m => m.Key == get("make")) ? get("make") : string.Empty,
                Year = int.TryParse(get("year"), out int y) && y >= 1980 && y <= 2100 ? y : 0,
                Min = Dec(get("min")),
                Max = Dec(get("max")),
                InStock = get("stock") == "1",
                Sort = Sorts.Any(x => x.Key == get("sort")) ? get("sort") : "new",
                ViewMode = get("view") == "list" ? "list" : "grid",
                Page = int.TryParse(get("pg"), out int p) ? Math.Max(1, p) : 1
            };
            f.Model = f.Make != string.Empty && Ui.MakeOf(site, f.Make)!.Models.Any(m => m.Key == get("model")) ? get("model") : string.Empty;
            return f;
        }

        private static List<Part> Apply(SiteData site, Filter f)
        {
            IEnumerable<Part> q = site.Products.Where(Ui.Public);
            if (f.Q != string.Empty)
            {
                q = q.Where(p => (p.Name + " " + p.Sku + " " + p.Summary + " " + Ui.CategoryName(p.Category) + " " + string.Join(" ", p.Fitments.Select(x => Ui.MakeName(site, x.Make) + " " + Ui.ModelName(site, x.Make, x.Model)))).Contains(f.Q, StringComparison.OrdinalIgnoreCase));
            }
            if (f.Cat != string.Empty)
            {
                q = q.Where(p => p.Category == f.Cat);
            }
            if (f.Series != string.Empty)
            {
                q = q.Where(p => p.Series == f.Series);
            }
            if (f.Make != string.Empty || f.Year != 0)
            {
                q = q.Where(p => Ui.Fits(p, f.Make, f.Model, f.Year));
            }
            if (f.Min > 0)
            {
                q = q.Where(p => p.Price >= f.Min);
            }
            if (f.Max > 0)
            {
                q = q.Where(p => p.Price <= f.Max);
            }
            if (f.InStock)
            {
                q = q.Where(p => p.Stock is "in" or "low");
            }
            q = f.Sort switch
            {
                "popular" => q.OrderByDescending(p => p.Views),
                "price" => q.OrderBy(p => p.Price),
                "priceDesc" => q.OrderByDescending(p => p.Price),
                "name" => q.OrderBy(p => p.Name),
                _ => q.OrderByDescending(p => p.Listed ?? p.Created)
            };
            return q.ToList();
        }

        private static string Chip(string label, string key)
        {
            return "<button type=\"button\" class=\"pr-fchip\" onclick=\"ProductsJs.clear('" + key + "')\">" + Ui.Enc(label) + " <svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M7 7l10 10M17 7L7 17\"/></svg></button>";
        }

        private static string Results(SiteData site, Filter f, DateTime today)
        {
            List<Part> all = Apply(site, f);
            int pages = Math.Max(1, (all.Count + Per - 1) / Per);
            int page = Math.Min(f.Page, pages);
            StringBuilder chips = new StringBuilder();
            if (f.Q != string.Empty)
            {
                chips.Append(Chip("“" + f.Q + "”", "q"));
            }
            if (f.Cat != string.Empty)
            {
                chips.Append(Chip(Ui.CategoryName(f.Cat), "cat"));
            }
            if (f.Series != string.Empty)
            {
                chips.Append(Chip(Ui.SeriesName(f.Series) + " series", "series"));
            }
            if (f.Make != string.Empty || f.Year != 0)
            {
                chips.Append(Chip(((f.Year != 0 ? f.Year.ToString(Ui.Inv) + " " : string.Empty) + (f.Make != string.Empty ? Ui.MakeName(site, f.Make) + " " + Ui.ModelName(site, f.Make, f.Model) : "Any vehicle")).Trim(), "vehicle"));
            }
            if (f.Min > 0 || f.Max > 0)
            {
                chips.Append(Chip((f.Min > 0 ? Ui.Money(f.Min).Replace("&minus;", "-") : "$0") + " to " + (f.Max > 0 ? Ui.Money(f.Max) : "any"), "price"));
            }
            if (f.InStock)
            {
                chips.Append(Chip("In stock", "stock"));
            }
            StringBuilder sb = new StringBuilder("<div class=\"pr-rhead\"><b>" + Ui.Plural(all.Count, "product", "products") + "</b>" + (chips.Length > 0 ? "<div class=\"pr-fchips\">" + chips + "<button type=\"button\" class=\"pr-fclear\" onclick=\"ProductsJs.clearAll()\">Clear all</button></div>" : string.Empty) + "</div>");
            if (all.Count == 0)
            {
                sb.Append("<div class=\"pr-empty\"><p>No products match all of these filters. Try removing one, or ask the fitment desk: they know every application, including ones not in the catalog yet.</p><div class=\"pr-hero-b\"><button type=\"button\" class=\"pr-btn pr-btn-ghost\" onclick=\"ProductsJs.clearAll()\">Clear filters</button><a class=\"pr-btn pr-btn-accent\" href=\"Contact#quote\">Ask the fitment desk</a></div></div>");
                return sb.ToString();
            }
            sb.Append("<div class=\"pr-cards" + (f.ViewMode == "list" ? " pr-listv" : string.Empty) + "\">");
            foreach (Part p in all.Skip((page - 1) * Per).Take(Per))
            {
                sb.Append(Card(site, p, today));
            }
            sb.Append("</div>");
            if (pages > 1)
            {
                sb.Append("<div class=\"pr-pager\"><button type=\"button\" class=\"pr-btn pr-btn-sm pr-btn-ghost\" onclick=\"ProductsJs.page(" + (page - 1) + ")\"" + (page <= 1 ? " disabled" : string.Empty) + ">&larr; Previous</button><span>Page " + page + " of " + pages + "</span><button type=\"button\" class=\"pr-btn pr-btn-sm pr-btn-ghost\" onclick=\"ProductsJs.page(" + (page + 1) + ")\"" + (page >= pages ? " disabled" : string.Empty) + ">Next &rarr;</button></div>");
            }
            return sb.ToString();
        }

        private static string Sel(string id, string label, string any, IEnumerable<(string, string)> opts, string cur, string extra = "")
        {
            return "<label class=\"pr-field\"><span>" + label + "</span><select id=\"pr-" + id + "\" " + extra + " onchange=\"ProductsJs.go()\">" + (any != string.Empty ? "<option value=\"\">" + any + "</option>" : string.Empty) + Ui.Opts(opts, cur) + "</select></label>";
        }

        private static string Side(SiteData site, Filter f)
        {
            List<(string, string)> years = new List<(string, string)>();
            for (int y = site.Settings.YearTo; y >= site.Settings.YearFrom; y--)
            {
                years.Add((y.ToString(Ui.Inv), y.ToString(Ui.Inv)));
            }
            string makes = JsonSerializer.Serialize(site.Makes.Select(m => new { key = m.Key, name = m.Name, models = m.Models.Select(x => new { key = x.Key, name = x.Name }) }));
            StringBuilder sb = new StringBuilder("<aside class=\"pr-side\" id=\"pr-side\" aria-label=\"Filters\"><div class=\"pr-side-top\"><b>Filters</b><button type=\"button\" class=\"pr-ibtn\" aria-label=\"Close filters\" onclick=\"ProductsJs.filters(false)\"><svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6L6 18\"/></svg></button></div>");
            sb.Append("<form class=\"pr-find pr-sidefind\" data-makes=\"" + Ui.Attr(makes) + "\" onsubmit=\"return ProductsJs.go()\"><span class=\"pr-flabel\">Your vehicle</span><div class=\"pr-vrow\"><label class=\"pr-field\"><span class=\"pr-sr\">Year</span><select name=\"year\" id=\"pr-year\" onchange=\"ProductsJs.go()\"><option value=\"\">Any year</option>" + Ui.Opts(years, f.Year == 0 ? string.Empty : f.Year.ToString(Ui.Inv)) + "</select></label><label class=\"pr-field\"><span class=\"pr-sr\">Make</span><select name=\"make\" id=\"pr-make\"><option value=\"\">Any make</option>" + Ui.Opts(site.Makes.Select(m => (m.Key, m.Name)), f.Make) + "</select></label><label class=\"pr-field\"><span class=\"pr-sr\">Model</span><select name=\"model\" id=\"pr-model\" data-cur=\"" + Ui.Attr(f.Model) + "\" onchange=\"ProductsJs.go()\" disabled><option value=\"\">Any model</option></select></label></div></form>");
            sb.Append("<div class=\"pr-fgroup\"><span class=\"pr-flabel\">Category</span><div class=\"pr-flist\"><label class=\"pr-radio\"><input type=\"radio\" name=\"pr-cat\" value=\"\"" + (f.Cat == string.Empty ? " checked" : string.Empty) + " onchange=\"ProductsJs.go()\"><span>All categories</span></label>");
            foreach ((string k, string n) in Ui.Categories)
            {
                int cnt = site.Products.Count(p => Ui.Public(p) && p.Category == k);
                sb.Append("<label class=\"pr-radio\"><input type=\"radio\" name=\"pr-cat\" value=\"" + k + "\"" + (f.Cat == k ? " checked" : string.Empty) + " onchange=\"ProductsJs.go()\"><span>" + Ui.Enc(n) + "<em>" + cnt + "</em></span></label>");
            }
            sb.Append("</div></div><div class=\"pr-fgroup\"><span class=\"pr-flabel\">Series</span><div class=\"pr-choices\"><label class=\"pr-choice\"><input type=\"radio\" name=\"pr-series\" value=\"\"" + (f.Series == string.Empty ? " checked" : string.Empty) + " onchange=\"ProductsJs.go()\"><span>All</span></label>");
            foreach ((string k, string n) in Ui.Series)
            {
                sb.Append("<label class=\"pr-choice\"><input type=\"radio\" name=\"pr-series\" value=\"" + k + "\"" + (f.Series == k ? " checked" : string.Empty) + " onchange=\"ProductsJs.go()\"><span>" + Ui.Enc(n) + "</span></label>");
            }
            sb.Append("</div></div><div class=\"pr-fgroup\"><span class=\"pr-flabel\">Price (MSRP)</span><div class=\"pr-prow\"><label class=\"pr-field\"><span class=\"pr-sr\">Min</span><input type=\"text\" inputmode=\"numeric\" id=\"pr-min\" placeholder=\"Min\" maxlength=\"7\" value=\"" + (f.Min > 0 ? f.Min.ToString("0", Ui.Inv) : string.Empty) + "\" onchange=\"ProductsJs.go()\"></label><span>to</span><label class=\"pr-field\"><span class=\"pr-sr\">Max</span><input type=\"text\" inputmode=\"numeric\" id=\"pr-max\" placeholder=\"Max\" maxlength=\"7\" value=\"" + (f.Max > 0 ? f.Max.ToString("0", Ui.Inv) : string.Empty) + "\" onchange=\"ProductsJs.go()\"></label></div></div>");
            sb.Append("<div class=\"pr-fgroup\"><label class=\"pr-check\"><input type=\"checkbox\" id=\"pr-stock\"" + (f.InStock ? " checked" : string.Empty) + " onchange=\"ProductsJs.go()\"> In stock only</label></div><div class=\"pr-fgroup pr-fapply\"><button type=\"button\" class=\"pr-btn pr-btn-wide\" onclick=\"ProductsJs.filters(false)\">Show results</button></div></aside>");
            return sb.ToString();
        }

        private async Task<View> Render(SiteData site)
        {
            DateTime today = Store.Today(site);
            Filter f = Read(site, k => Q(k, 400));
            bool compare = Q("compare") == "1";
            List<Part> pub = site.Products.Where(Ui.Public).ToList();
            StringBuilder sb = new StringBuilder("<section class=\"pr-phead pr-wrap\"><span class=\"pr-kick\">Products</span><h1>" + (compare ? "Compare products" : f.Cat != string.Empty ? Ui.Enc(Ui.CategoryName(f.Cat)) : "The catalog") + "</h1><p class=\"pr-lead\">" + (compare ? "Up to three products side by side. Add more from any product card." : "Every product is built for a specific vehicle and tested as a set. Pick your vehicle on the left to see what fits.") + "</p></section>");
            if (compare)
            {
                sb.Append("<section class=\"pr-wrap\" id=\"pr-compare\"><p class=\"pr-none\">Loading your comparison&hellip;</p></section>");
                return new View { Title = "Compare products", Main = sb.ToString(), Nav = "products" };
            }
            sb.Append("<div class=\"pr-wrap pr-layout\">" + Side(site, f) + "<div class=\"pr-scrim2\" id=\"pr-fscrim\" onclick=\"ProductsJs.filters(false)\"></div><div class=\"pr-resw\"><div class=\"pr-fbar\"><button type=\"button\" class=\"pr-btn pr-btn-ghost pr-btn-sm pr-fopen\" onclick=\"ProductsJs.filters(true)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M4 7h10M18 7h2M4 17h4M12 17h8\"/><circle cx=\"16\" cy=\"7\" r=\"2\"/><circle cx=\"10\" cy=\"17\" r=\"2\"/></svg> Filters</button><label class=\"pr-qbox\"><span class=\"pr-sr\">Search</span><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"6.5\"/><path d=\"M16 16l4.5 4.5\"/></svg><input type=\"search\" id=\"pr-q\" list=\"pr-qlist\" placeholder=\"Part number, name or vehicle\" maxlength=\"40\" value=\"" + Ui.Attr(f.Q) + "\" oninput=\"ProductsJs.typed()\"></label>" + Ui.DataList("pr-qlist", pub.Select(p => p.Sku).Concat(site.Makes.Select(m => m.Name))) + "<label class=\"pr-sel\"><span class=\"pr-sr\">Sort</span><select id=\"pr-sort\" onchange=\"ProductsJs.go()\">" + Ui.Opts(Sorts.Select(x => (x.Key, x.Name)), f.Sort) + "</select></label><div class=\"pr-vt\" role=\"group\" aria-label=\"View\"><button type=\"button\" class=\"pr-vtb" + (f.ViewMode == "grid" ? " pr-act" : string.Empty) + "\" data-v=\"grid\" aria-pressed=\"" + (f.ViewMode == "grid" ? "true" : "false") + "\" onclick=\"ProductsJs.view('grid')\">Grid</button><button type=\"button\" class=\"pr-vtb" + (f.ViewMode == "list" ? " pr-act" : string.Empty) + "\" data-v=\"list\" aria-pressed=\"" + (f.ViewMode == "list" ? "true" : "false") + "\" onclick=\"ProductsJs.view('list')\">List</button></div><input type=\"hidden\" id=\"pr-view\" value=\"" + f.ViewMode + "\"></div><section id=\"pr-results\" aria-live=\"polite\">" + Results(site, f, today) + "</section></div></div>");
            return new View { Title = f.Cat != string.Empty ? Ui.CategoryName(f.Cat) : "Products", Desc = "Suspension products by " + site.Settings.Name + ": coilovers, struts, shocks, arms, sway bars, bushings, leaf springs, lift kits and air suspension.", Main = sb.ToString(), Nav = "products" };
        }

        public async Task<ApiResponse> Search()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            Filter f = Read(site, k => GetDataValue(k));
            response.SetElementContents("pr-results", Results(site, f, Store.Today(site)));
            response.ExecuteScript("ProductsJs.shown();");
            return response;
        }

        private static string SpecCell(SiteData site, Part p, string name)
        {
            SpecRow? s = p.Specs.FirstOrDefault(x => x.Name == name);
            return s == null ? "<span class=\"pr-dash\">&ndash;</span>" : Ui.Measure(site, s.Value, s.Unit);
        }

        public async Task<ApiResponse> Compare()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            List<string> ids = GetDataValue("ids").Split(',').Select(x => Ui.Clip(x, 20)).Where(x => x != string.Empty).Distinct().Take(3).ToList();
            List<Part> list = ids.Select(id => site.Products.FirstOrDefault(p => p.Id == id && Ui.Public(p))).Where(p => p != null).Select(p => p!).ToList();
            if (list.Count == 0)
            {
                response.SetElementContents("pr-compare", "<div class=\"pr-empty\"><p>Nothing to compare yet. Tap Compare on up to three product cards and come back here.</p><div class=\"pr-hero-b\"><a class=\"pr-btn pr-btn-accent\" href=\"Products\">Browse products</a></div></div>");
                return response;
            }
            List<string> names = list.SelectMany(p => p.Specs.Select(s => s.Name)).Distinct().ToList();
            StringBuilder sb = new StringBuilder("<div class=\"pr-ctbl\" style=\"--n:" + list.Count + "\"><div class=\"pr-crow pr-chead\"><div></div>");
            foreach (Part p in list)
            {
                sb.Append("<div class=\"pr-ccell\"><a href=\"Product?id=" + Uri.EscapeDataString(p.Id) + "\">" + Ui.Pic(Ui.Cover(site, p), p.Name, string.Empty, true) + "</a><b><a href=\"Product?id=" + Uri.EscapeDataString(p.Id) + "\">" + Ui.Enc(p.Name) + "</a></b><small>" + Ui.Enc(p.Sku) + "</small><button type=\"button\" class=\"pr-btn pr-btn-sm pr-btn-ghost\" onclick=\"ProductsJs.uncmp('" + Ui.Attr(p.Id) + "')\">Remove</button></div>");
            }
            sb.Append("</div>");
            void Row(string label, Func<Part, string> cell)
            {
                sb.Append("<div class=\"pr-crow\"><div class=\"pr-clabel\">" + label + "</div>");
                foreach (Part p in list)
                {
                    sb.Append("<div class=\"pr-ccell\">" + cell(p) + "</div>");
                }
                sb.Append("</div>");
            }
            Row("Price (MSRP)", p => "<b>" + Ui.Money(p.Price) + "</b>");
            Row("Category", p => Ui.Enc(Ui.CategoryName(p.Category)));
            Row("Series", p => Ui.Enc(Ui.SeriesName(p.Series)));
            Row("Availability", p => Ui.Pill("pr", Ui.Stocks, p.Stock) + (p.LeadDays > 0 ? "<small> " + Ui.Plural(p.LeadDays, "day", "days") + "</small>" : string.Empty));
            Row("Warranty", p => Ui.Plural(p.WarrantyMonths > 0 ? p.WarrantyMonths : site.Settings.WarrantyMonths, "month", "months"));
            Row("Fits", p => string.Join("<br>", p.Fitments.Select(f => Ui.FitLine(site, f))));
            Row("Install time", p => p.InstallMinutes > 0 ? "About " + Ui.Plural((int)Math.Round(p.InstallMinutes / 60.0), "hour", "hours") : "&ndash;");
            foreach (string n in names)
            {
                Row(Ui.Enc(n), p => SpecCell(site, p, n));
            }
            sb.Append("</div><div class=\"pr-hero-b\" style=\"margin-top:20px\"><a class=\"pr-btn pr-btn-ghost\" href=\"Products\">&larr; Back to products</a><a class=\"pr-btn pr-btn-accent\" href=\"Contact#quote\">Request a quote</a></div>");
            response.SetElementContents("pr-compare", sb.ToString());
            return response;
        }
    }
}
