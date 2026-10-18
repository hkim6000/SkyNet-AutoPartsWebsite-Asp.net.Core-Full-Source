using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class Product : WebPage
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
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--pd-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_nav}", Nav("pd", v.Nav, false))
                .Replace("{plhd_drawer}", Nav("pd", v.Nav, true))
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
                return "<i class=\"pd-shut\"></i>Fitment desk opens today at " + Ui.HmText(site, d!.Start);
            }
            for (int i = 1; i <= 7; i++)
            {
                DateTime n = now.Date.AddDays(i);
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)n.DayOfWeek);
                if (o != null && o.Open)
                {
                    return "<i class=\"pd-shut\"></i>Closed now &middot; opens " + (i == 1 ? "tomorrow" : Ui.DayNames[(int)n.DayOfWeek]) + " at " + Ui.HmText(site, o.Start);
                }
            }
            return "<i class=\"pd-shut\"></i>Closed";
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
                response.SetElementContents("pd-e-nlmail", "Enter an email address like name@example.com.");
                return response;
            }
            bool added = await Store.AddSubscriber(mail);
            response.ExecuteScript("ProductJs.subscribed('" + (added ? "Thanks. Product news lands about once a month." : "You are already on the list.") + "');");
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
            return "<article class=\"pd-pc\" data-pid=\"" + Ui.Attr(p.Id) + "\"><a class=\"pd-pc-img\" href=\"" + href + "\" tabindex=\"-1\" aria-hidden=\"true\">" + Ui.Pic(Ui.Cover(site, p), string.Empty, string.Empty, true) + (text != string.Empty ? "<span class=\"pd-badge pd-b-" + cls + "\">" + text + "</span>" : string.Empty) + "</a><button type=\"button\" class=\"pd-cmpb\" data-cmp=\"" + Ui.Attr(p.Id) + "\" aria-pressed=\"false\" onclick=\"ProductJs.cmp(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 4v16M15 4v16\"/><path d=\"M4 9h5M15 9h5M4 15h5M15 15h5\"/></svg><span>Compare</span></button><div class=\"pd-pc-b\"><span class=\"pd-pcat\"><span>" + Ui.Enc(Ui.CategoryName(p.Category)) + "</span><em>" + Ui.Enc(Ui.SeriesName(p.Series)) + "</em></span><span class=\"pd-pname\"><a href=\"" + href + "\">" + Ui.Enc(p.Name) + "</a></span><span class=\"pd-pfit\">" + FitSummary(site, p) + "</span><span class=\"pd-pc-f\"><span class=\"pd-price\">" + Ui.Money(p.Price) + "<small>MSRP</small></span>" + Ui.Pill("pd", Ui.Stocks, p.Stock) + "</span></div></article>";
        }

        private async Task<View> Render(SiteData site)
        {
            DateTime today = Store.Today(site);
            string id = Q("id", 20);
            Part? d = site.Products.FirstOrDefault(x => x.Id == id || x.Sku.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (d == null || !Ui.Public(d))
            {
                return new View { Title = "Product not found", Main = "<section class=\"pd-phead pd-wrap\"><h1>We could not find that product</h1><p class=\"pd-lead\">The link may be old, or the product may have been retired from the catalog.</p><p style=\"margin-top:20px\"><a class=\"pd-btn pd-btn-accent\" href=\"Products\">Browse the catalog</a></p></section>", Nav = "products" };
            }
            using (await Store.Hold())
            {
                SiteData live = await Store.LoadSite();
                Part? lv = live.Products.FirstOrDefault(x => x.Id == d.Id);
                if (lv != null)
                {
                    lv.Views++;
                    await Store.SaveSite(live);
                }
            }
            string name = Ui.Enc(d.Name);
            var pics = d.Photos.Select((p, i) => new { f = p, t = Ui.Poster(site, p), v = Ui.IsVideo(p), c = d.Name + (Ui.IsVideo(p) ? ", video" : ", view " + (i + 1)) });
            StringBuilder sb = new StringBuilder("<div class=\"pd-wrap pd-crumb\" style=\"padding-top:22px\"><a href=\"Products\">Products</a><span>/</span><a href=\"Products?cat=" + d.Category + "\">" + Ui.Enc(Ui.CategoryName(d.Category)) + "</a><span>/</span><span>" + Ui.Enc(d.Sku) + "</span></div>");
            sb.Append("<div class=\"pd-wrap pd-dtop\"><div class=\"pd-gal\" id=\"pd-gal\" data-photos=\"" + Ui.Attr(JsonSerializer.Serialize(pics)) + "\">");
            for (int i = 0; i < d.Photos.Count && i < 3; i++)
            {
                bool vid = Ui.IsVideo(d.Photos[i]);
                sb.Append("<button type=\"button\" class=\"pd-gi pd-gi" + i + "\" onclick=\"ProductJs.show(" + i + ")\" aria-label=\"Open " + (vid ? "video" : "photo") + " " + (i + 1) + " of " + d.Photos.Count + "\">" + Ui.Pic(vid ? Ui.Poster(site, d.Photos[i]) : i == 0 ? d.Photos[i] : Ui.Thumb(d.Photos[i]), d.Name, string.Empty, false, i == 0) + (vid ? "<span class=\"pd-gplay\"></span>" : string.Empty) + "</button>");
            }
            int warranty = d.WarrantyMonths > 0 ? d.WarrantyMonths : site.Settings.WarrantyMonths;
            sb.Append("</div><aside class=\"pd-card\"><span class=\"pd-pcat\"><span>" + Ui.Enc(Ui.CategoryName(d.Category)) + "</span><em>" + Ui.Enc(Ui.SeriesName(d.Series)) + " series</em></span><h1>" + name + "</h1><p class=\"pd-sku\">Part number <b class=\"pd-code\">" + Ui.Enc(d.Sku) + "</b></p><p class=\"pd-sum\">" + Ui.Enc(d.Summary) + "</p>");
            sb.Append("<div class=\"pd-pricebox\"><div><span class=\"pd-price\">" + Ui.Money(d.Price) + "</span><small>MSRP, before tax and freight</small></div><div class=\"pd-stockl\">" + Ui.Pill("pd", Ui.Stocks, d.Stock) + (d.Stock == "order" ? "<small>Ships in about " + Ui.Plural(Math.Max(1, d.LeadDays), "day", "days") + "</small>" : d.Stock == "out" ? "<small>Ask for the next build date</small>" : "<small>" + (d.LeadDays > 0 ? "Ships in " + Ui.Plural(d.LeadDays, "day", "days") : "Ships the same day") + "</small>") + "</div></div>");
            sb.Append("<div class=\"pd-fits\"><b>Fits</b><ul>" + string.Concat(d.Fitments.Take(3).Select(f => "<li>" + Ui.FitLine(site, f) + (f.Position != "set" ? " <small>" + Ui.NameOf(Ui.Positions, f.Position) + "</small>" : string.Empty) + "</li>")) + "</ul>" + (d.Fitments.Count > 3 ? "<a class=\"pd-more\" href=\"#fitment\">All " + d.Fitments.Count + " applications &rarr;</a>" : string.Empty) + "</div>");
            sb.Append("<div class=\"pd-cta2\"><button type=\"button\" class=\"pd-btn pd-btn-accent pd-btn-wide\" onclick=\"ProductJs.quote()\">Request a quote</button><a class=\"pd-btn pd-btn-ghost\" href=\"Dealers?sku=" + Uri.EscapeDataString(d.Sku) + "\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 21s-6.5-6-6.5-11a6.5 6.5 0 0 1 13 0c0 5-6.5 11-6.5 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.4\"/></svg> Find a dealer</a><button type=\"button\" class=\"pd-btn pd-btn-ghost pd-cmpx\" data-cmp=\"" + Ui.Attr(d.Id) + "\" aria-pressed=\"false\" onclick=\"ProductJs.cmp(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 4v16M15 4v16\"/><path d=\"M4 9h5M15 9h5M4 15h5M15 15h5\"/></svg> <span>Compare</span></button></div><button type=\"button\" class=\"pd-share\" onclick=\"ProductJs.share()\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 15V4M7.5 8.5L12 4l4.5 4.5\"/><path d=\"M5 13v6h14v-6\"/></svg> Share this product</button><p class=\"pd-hint\">" + Ui.Plural(warranty, "month", "months") + " warranty &middot; " + Ui.Enc(site.Settings.ShipFrom) + "</p></aside></div>");
            sb.Append("<div class=\"pd-wrap pd-dbody\"><div class=\"pd-dmain\"><section><h2>Overview</h2>");
            foreach (string para in d.Text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append("<p class=\"pd-story\">" + Ui.Enc(para.Trim()) + "</p>");
            }
            if (d.Features.Count > 0)
            {
                sb.Append("<ul class=\"pd-feats\">" + string.Concat(d.Features.Select(f => "<li><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg><span>" + Ui.Enc(f) + "</span></li>")) + "</ul>");
            }
            sb.Append("</section>");
            if (d.Specs.Count > 0)
            {
                sb.Append("<section id=\"specs\"><h2>Specifications</h2><dl class=\"pd-specs\">" + string.Concat(d.Specs.Select(s => "<div><dt>" + Ui.Enc(s.Name) + "</dt><dd>" + Ui.Measure(site, s.Value, s.Unit) + "</dd></div>")) + "</dl><p class=\"pd-hint\">" + (site.Settings.Units == "both" ? "US units with metric in brackets." : site.Settings.Units == "metric" ? "Metric units." : "US units.") + " Specifications can change between production lots; the sheet in the box is the reference.</p></section>");
            }
            sb.Append("<section id=\"fitment\"><h2>Fitment</h2><div class=\"pd-ftbl\"><div class=\"pd-ftr pd-fth\"><span>Make</span><span>Model</span><span>Years</span><span>Position</span><span>Note</span></div>" + string.Concat(d.Fitments.Select(f => "<div class=\"pd-ftr\"><span data-l=\"Make\">" + Ui.Enc(Ui.MakeName(site, f.Make)) + "</span><span data-l=\"Model\">" + Ui.Enc(Ui.ModelName(site, f.Make, f.Model)) + "</span><span data-l=\"Years\">" + f.From.ToString(Ui.Inv) + "&ndash;" + f.To.ToString(Ui.Inv) + "</span><span data-l=\"Position\">" + Ui.NameOf(Ui.Positions, f.Position) + "</span><span data-l=\"Note\">" + (f.Note != string.Empty ? Ui.Enc(f.Note) : "&ndash;") + "</span></div>")) + "</div><p class=\"pd-hint\">Not listed? <a href=\"Contact#quote\">Ask the fitment desk</a>: many applications are available before they reach the catalog.</p></section>");
            sb.Append("<section><h2>In the box</h2><ul class=\"pd-feats\">" + string.Concat(d.Box.Select(f => "<li><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg><span>" + Ui.Enc(f) + "</span></li>")) + "</ul></section>");
            sb.Append("<section><h2>Installation</h2><div class=\"pd-inst\"><div><b>" + (d.InstallMinutes > 0 ? "About " + Ui.Plural((int)Math.Round(d.InstallMinutes / 60.0), "hour", "hours") : "Varies") + "</b><span>with a lift or jack stands and hand tools</span></div><div><b>Torque</b><span>" + Ui.Enc(d.Torque) + "</span></div></div>" + (d.Docs.Count > 0 ? "<div class=\"pd-docs\">" + string.Concat(d.Docs.Select(x => DocLink(site, x))) + "</div>" : string.Empty) + "<p class=\"pd-hint\">Our <a href=\"Dealers\">installers</a> fit and align what they sell.</p></section></div>");
            sb.Append("<aside class=\"pd-askd\" id=\"pd-ask\"><div class=\"pd-box\"><h2>Ask about this part</h2><form class=\"pd-form\" id=\"pd-af\" onsubmit=\"return ProductJs.send()\" novalidate><label class=\"pd-field\"><span>Your name</span><input type=\"text\" id=\"pd-aname\" maxlength=\"80\" autocomplete=\"name\"></label><em class=\"pd-err\" id=\"pd-e-aname\" role=\"alert\"></em><label class=\"pd-field\"><span>Email</span><input type=\"email\" id=\"pd-amail\" maxlength=\"120\" autocomplete=\"email\"></label><em class=\"pd-err\" id=\"pd-e-amail\" role=\"alert\"></em><label class=\"pd-field\"><span>Phone <small>(optional)</small></span><input type=\"tel\" id=\"pd-aphone\" maxlength=\"30\" autocomplete=\"tel\"></label><label class=\"pd-field\"><span>Your question</span><textarea id=\"pd-amsg\" rows=\"4\" maxlength=\"1000\" placeholder=\"Will this clear my wheels? Which spring rate is in the kit?\"></textarea></label><em class=\"pd-err\" id=\"pd-e-amsg\" role=\"alert\"></em><label class=\"pd-hp\" aria-hidden=\"true\">Website<input type=\"text\" id=\"pd-aweb\" tabindex=\"-1\" autocomplete=\"off\"></label><input type=\"hidden\" id=\"pd-apid\" value=\"" + Ui.Attr(d.Id) + "\"><button type=\"submit\" class=\"pd-btn pd-btn-wide\" id=\"pd-asend\">Send question</button><p class=\"pd-hint\">The fitment desk answers within one business day.</p></form><div id=\"pd-adone\"></div></div></aside></div>");
            List<Part> more = site.Products.Where(x => x.Id != d.Id && Ui.Public(x) && (x.Category == d.Category || x.Fitments.Any(f => d.Fitments.Any(g => g.Make == f.Make && g.Model == f.Model)))).OrderByDescending(x => x.Fitments.Any(f => d.Fitments.Any(g => g.Make == f.Make && g.Model == f.Model))).ThenByDescending(x => x.Views).Take(4).ToList();
            if (more.Count > 0)
            {
                sb.Append("<section class=\"pd-sec pd-wrap\"><div class=\"pd-shead\"><div><span class=\"pd-kick\">Goes with it</span><h2 class=\"pd-h2\">Related products</h2></div><a class=\"pd-more\" href=\"Products\">All products &rarr;</a></div><div class=\"pd-cards pd-c4\">");
                foreach (Part x in more)
                {
                    sb.Append(Card(site, x, today));
                }
                sb.Append("</div></section>");
            }
            sb.Append("<div class=\"pd-lb\" id=\"pd-lb\" role=\"dialog\" aria-modal=\"true\" aria-label=\"Photos\" hidden><div class=\"pd-lbtop\"><span id=\"pd-lbn\"></span><span id=\"pd-lbc\"></span><button type=\"button\" class=\"pd-ibtn pd-lbx\" aria-label=\"Close photos\" onclick=\"ProductJs.hide()\"><svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6L6 18\"/></svg></button></div><div class=\"pd-lbstage\"><button type=\"button\" class=\"pd-lbnav\" aria-label=\"Previous photo\" onclick=\"ProductJs.step(-1)\"><svg viewBox=\"0 0 24 24\" width=\"26\" height=\"26\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M15 5l-7 7 7 7\"/></svg></button><img id=\"pd-lbimg\" alt=\"\"><video id=\"pd-lbvid\" controls playsinline hidden></video><button type=\"button\" class=\"pd-lbnav\" aria-label=\"Next photo\" onclick=\"ProductJs.step(1)\"><svg viewBox=\"0 0 24 24\" width=\"26\" height=\"26\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 5l7 7-7 7\"/></svg></button></div><div class=\"pd-lbthumbs\" id=\"pd-lbthumbs\"></div></div>");
            sb.Append("<div class=\"pd-qm\" id=\"pd-qm\" role=\"dialog\" aria-modal=\"true\" aria-labelledby=\"pd-qmt\" hidden><div class=\"pd-qmbox\"><button type=\"button\" class=\"pd-ibtn pd-qmx\" aria-label=\"Close\" onclick=\"ProductJs.quoteClose()\"><svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6L6 18\"/></svg></button><h2 id=\"pd-qmt\">Request a quote</h2><p class=\"pd-hint\">" + Ui.Enc(site.Settings.QuoteText) + "</p><form class=\"pd-form\" id=\"pd-qf\" onsubmit=\"return ProductJs.quoteSend()\" novalidate><div class=\"pd-qprod\">" + Ui.Pic(Ui.Cover(site, d), d.Name, string.Empty, true) + "<span><b>" + name + "</b><small>" + Ui.Enc(d.Sku) + " &middot; " + Ui.Money(d.Price) + " each</small></span><label class=\"pd-field pd-qty\"><span>Quantity</span><input type=\"number\" id=\"pd-qqty\" min=\"1\" max=\"999\" value=\"1\"></label></div><div class=\"pd-row2\"><label class=\"pd-field\"><span>Your name</span><input type=\"text\" id=\"pd-qname\" maxlength=\"80\" autocomplete=\"name\"></label><label class=\"pd-field\"><span>Company <small>(optional)</small></span><input type=\"text\" id=\"pd-qcomp\" maxlength=\"80\" autocomplete=\"organization\"></label></div><em class=\"pd-err\" id=\"pd-e-qname\" role=\"alert\"></em><div class=\"pd-row2\"><label class=\"pd-field\"><span>Email</span><input type=\"email\" id=\"pd-qmail\" maxlength=\"120\" autocomplete=\"email\"></label><label class=\"pd-field\"><span>Phone <small>(optional)</small></span><input type=\"tel\" id=\"pd-qphone\" maxlength=\"30\" autocomplete=\"tel\"></label></div><em class=\"pd-err\" id=\"pd-e-qmail\" role=\"alert\"></em><div class=\"pd-row2\"><label class=\"pd-field\"><span>Your vehicle</span><input type=\"text\" id=\"pd-qveh\" maxlength=\"80\" placeholder=\"" + Ui.Attr((d.Fitments.Count > 0 ? d.Fitments[0].To.ToString(Ui.Inv) + " " + Ui.MakeName(site, d.Fitments[0].Make) + " " + Ui.ModelName(site, d.Fitments[0].Make, d.Fitments[0].Model) : "Year, make and model")) + "\"></label><label class=\"pd-field\"><span>State</span><select id=\"pd-qstate\"><option value=\"\">Choose</option>" + Ui.Opts(Ui.States.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label></div><label class=\"pd-field\"><span>Anything else <small>(optional)</small></span><textarea id=\"pd-qmsg\" rows=\"3\" maxlength=\"1000\" placeholder=\"Ship to a dealer, a deadline, other parts you need\"></textarea></label><label class=\"pd-hp\" aria-hidden=\"true\">Website<input type=\"text\" id=\"pd-qweb\" tabindex=\"-1\" autocomplete=\"off\"></label><input type=\"hidden\" id=\"pd-qpid\" value=\"" + Ui.Attr(d.Id) + "\"><div class=\"pd-hero-b\" style=\"margin-top:6px\"><button type=\"submit\" class=\"pd-btn pd-btn-accent\" id=\"pd-qsend\">Send quote request</button><button type=\"button\" class=\"pd-btn pd-btn-ghost\" onclick=\"ProductJs.quoteClose()\">Cancel</button></div></form><div id=\"pd-qdone\"></div></div></div>");
            return new View { Title = d.Name + " · " + d.Sku, Desc = d.Summary + " " + Ui.Money(d.Price).Replace("&minus;", "-") + " MSRP at " + site.Settings.Name + ".", Main = sb.ToString(), Nav = "products" };
        }

        private static string DocLink(SiteData site, string src)
        {
            MediaItem? m = site.Media.FirstOrDefault(x => x.Full == src);
            string title = m?.Title ?? Path.GetFileNameWithoutExtension(src);
            return "<a class=\"pd-doc\" href=\"" + Ui.Attr(src) + "\" target=\"_blank\" rel=\"noopener\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 3h8l4 4v14H6z\"/><path d=\"M14 3v4h4M9 12h6M9 16h6\"/></svg><span><b>" + Ui.Enc(title) + "</b><small>PDF" + (m != null && m.Pages > 0 ? " &middot; " + Ui.Plural(m.Pages, "page", "pages") : string.Empty) + "</small></span><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 4v11M7.5 10.5L12 15l4.5-4.5\"/><path d=\"M5 19h14\"/></svg></a>";
        }

        public async Task<ApiResponse> Send()
        {
            ApiResponse response = new ApiResponse();
            string name = Regex.Replace(Ui.Clip(GetDataValue("aname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("amail"), 120).Trim();
            string msg = Ui.Clip(GetDataValue("amsg"), 1000).Trim();
            if (name.Length < 2)
            {
                response.ExecuteScript("ProductJs.fieldErr('aname','Please enter your name.');ProductJs.sent(false);");
                return response;
            }
            if (!Ui.Email(mail))
            {
                response.ExecuteScript("ProductJs.fieldErr('amail','Enter an email address like name@example.com.');ProductJs.sent(false);");
                return response;
            }
            if (msg.Length < 3)
            {
                response.ExecuteScript("ProductJs.fieldErr('amsg','Please write your question.');ProductJs.sent(false);");
                return response;
            }
            string sku = string.Empty;
            if (Ui.Clip(GetDataValue("aweb"), 50) == string.Empty)
            {
                using (await Store.Hold())
                {
                    SiteData site = await Store.LoadSite();
                    Part? d = site.Products.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("apid"), 20) && Ui.Public(x));
                    sku = d?.Sku ?? string.Empty;
                    if (!site.Inquiries.Any(x => x.Email.Equals(mail, StringComparison.OrdinalIgnoreCase) && x.Message == msg))
                    {
                        site.Inquiries.Add(new Inquiry { Id = Store.NextId(site.Inquiries.Select(x => x.Id), "Q-", 5000), Kind = "question", Sku = sku, Name = name, Email = mail, Phone = Ui.CleanPhone(GetDataValue("aphone")), Message = msg, Topic = "fitment", Created = DateTime.UtcNow });
                        await Store.SaveSite(site);
                    }
                }
                await Store.Log("web", "question", name + (sku != string.Empty ? " · " + sku : string.Empty), true);
            }
            response.ExecuteScript("ProductJs.sent(true,'" + Ui.Js("Thanks, " + name.Split(' ')[0] + ". The fitment desk will answer within one business day.") + "');");
            return response;
        }

        public async Task<ApiResponse> QuoteSend()
        {
            ApiResponse response = new ApiResponse();
            string name = Regex.Replace(Ui.Clip(GetDataValue("qname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("qmail"), 120).Trim();
            int qty = int.TryParse(GetDataValue("qqty"), out int qv) ? Math.Clamp(qv, 1, 999) : 1;
            if (name.Length < 2)
            {
                response.ExecuteScript("ProductJs.fieldErr('qname','Please enter your name.');ProductJs.quoted(false);");
                return response;
            }
            if (!Ui.Email(mail))
            {
                response.ExecuteScript("ProductJs.fieldErr('qmail','Enter an email address like name@example.com.');ProductJs.quoted(false);");
                return response;
            }
            string id = string.Empty;
            if (Ui.Clip(GetDataValue("qweb"), 50) == string.Empty)
            {
                using (await Store.Hold())
                {
                    SiteData site = await Store.LoadSite();
                    Part? d = site.Products.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("qpid"), 20) && Ui.Public(x));
                    if (d == null)
                    {
                        response.ExecuteScript("ProductJs.quoted(false);ProductJs.toast('That product is no longer available.');");
                        return response;
                    }
                    Inquiry q = new Inquiry { Id = Store.NextId(site.Inquiries.Select(x => x.Id), "Q-", 5000), Kind = "quote", Name = name, Company = Ui.Clip(GetDataValue("qcomp"), 80), Email = mail, Phone = Ui.CleanPhone(GetDataValue("qphone")), State = Ui.Has(Ui.States, GetDataValue("qstate")) ? GetDataValue("qstate") : string.Empty, Vehicle = Ui.Clip(GetDataValue("qveh"), 80), Message = Ui.Clip(GetDataValue("qmsg"), 1000), Created = DateTime.UtcNow };
                    q.Items.Add(new QuoteLine { Product = d.Id, Qty = qty });
                    site.Inquiries.Add(q);
                    id = q.Id;
                    await Store.SaveSite(site);
                }
                await Store.Log("web", "quote", id + " · " + name, true);
            }
            response.ExecuteScript("ProductJs.quoted(true,'" + Ui.Js("Thanks, " + name.Split(' ')[0] + ". Your request number is " + id + ". The fitment desk answers within one business day.") + "');");
            return response;
        }
    }
}
