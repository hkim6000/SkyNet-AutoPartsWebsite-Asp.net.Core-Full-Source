using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class Home : WebPage
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
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--hm-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_nav}", Nav("hm", v.Nav, false))
                .Replace("{plhd_drawer}", Nav("hm", v.Nav, true))
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
                return "<i class=\"hm-shut\"></i>Fitment desk opens today at " + Ui.HmText(site, d!.Start);
            }
            for (int i = 1; i <= 7; i++)
            {
                DateTime n = now.Date.AddDays(i);
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)n.DayOfWeek);
                if (o != null && o.Open)
                {
                    return "<i class=\"hm-shut\"></i>Closed now &middot; opens " + (i == 1 ? "tomorrow" : Ui.DayNames[(int)n.DayOfWeek]) + " at " + Ui.HmText(site, o.Start);
                }
            }
            return "<i class=\"hm-shut\"></i>Closed";
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
                response.SetElementContents("hm-e-nlmail", "Enter an email address like name@example.com.");
                return response;
            }
            bool added = await Store.AddSubscriber(mail);
            response.ExecuteScript("HomeJs.subscribed('" + (added ? "Thanks. Product news lands about once a month." : "You are already on the list.") + "');");
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
            return "<article class=\"hm-pc\" data-pid=\"" + Ui.Attr(p.Id) + "\"><a class=\"hm-pc-img\" href=\"" + href + "\" tabindex=\"-1\" aria-hidden=\"true\">" + Ui.Pic(Ui.Cover(site, p), string.Empty, string.Empty, true) + (text != string.Empty ? "<span class=\"hm-badge hm-b-" + cls + "\">" + text + "</span>" : string.Empty) + "</a><button type=\"button\" class=\"hm-cmpb\" data-cmp=\"" + Ui.Attr(p.Id) + "\" aria-pressed=\"false\" onclick=\"HomeJs.cmp(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 4v16M15 4v16\"/><path d=\"M4 9h5M15 9h5M4 15h5M15 15h5\"/></svg><span>Compare</span></button><div class=\"hm-pc-b\"><span class=\"hm-pcat\"><span>" + Ui.Enc(Ui.CategoryName(p.Category)) + "</span><em>" + Ui.Enc(Ui.SeriesName(p.Series)) + "</em></span><span class=\"hm-pname\"><a href=\"" + href + "\">" + Ui.Enc(p.Name) + "</a></span><span class=\"hm-pfit\">" + FitSummary(site, p) + "</span><span class=\"hm-pc-f\"><span class=\"hm-price\">" + Ui.Money(p.Price) + "<small>MSRP</small></span>" + Ui.Pill("hm", Ui.Stocks, p.Stock) + "</span></div></article>";
        }

        private async Task<View> Render(SiteData site)
        {
            DateTime today = Store.Today(site);
            return new View { Title = string.Empty, Desc = site.Settings.Tagline, Main = Page(site, "home", today), Nav = "home" };
        }

        private string DataPart(SiteData site, Block b, DateTime today)
        {
            switch (b.Source)
            {
                case "featured":
                    return Featured(site, b, today);
                case "families":
                    return Families(site, b);
                case "news":
                    return NewsPart(site, b);
                case "quoteband":
                    return QuoteBand(site, b);
                default:
                    return string.Empty;
            }
        }

        private static string Featured(SiteData site, Block b, DateTime today)
        {
            List<Part> list = site.Products.Where(p => Ui.Public(p) && p.Featured).OrderByDescending(p => p.Listed).Take(4).ToList();
            foreach (Part p in site.Products.Where(Ui.Public).OrderByDescending(p => p.Listed))
            {
                if (list.Count >= 4)
                {
                    break;
                }
                if (!list.Contains(p))
                {
                    list.Add(p);
                }
            }
            if (list.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder("<div class=\"hm-shead\"><div>" + Kick(site, b) + H2(site, b) + "</div><a class=\"hm-more\" href=\"Products\">All products &rarr;</a></div><div class=\"hm-cards hm-c4\">");
            foreach (Part p in list)
            {
                sb.Append(Card(site, p, today));
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Families(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"hm-fams\">");
            foreach ((string key, string name) in Ui.Categories)
            {
                int n = site.Products.Count(p => Ui.Public(p) && p.Category == key);
                sb.Append("<a class=\"hm-fam\" href=\"Products?cat=" + key + "\"><img src=\"images/products/thumbs/" + FamilyImage(site, key) + "\" alt=\"\" loading=\"lazy\" width=\"640\" height=\"480\"><span><b>" + Ui.Enc(name) + "</b><small>" + Ui.Enc(Ui.NameOf(Ui.CategoryBlurbs, key)) + "</small><em>" + Ui.Plural(n, "product", "products") + "</em></span></a>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string FamilyImage(SiteData site, string key)
        {
            Part? p = site.Products.FirstOrDefault(x => Ui.Public(x) && x.Category == key && x.Photos.Count > 0);
            return p == null ? "p01-1.webp" : Path.GetFileName(Ui.Cover(site, p));
        }

        private static string NewsPart(SiteData site, Block b)
        {
            List<Post> list = site.Posts.Where(p => p.Published).OrderByDescending(p => p.Date).Take(3).ToList();
            if (list.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder("<div class=\"hm-shead\"><div>" + Kick(site, b) + H2(site, b) + "</div><a class=\"hm-more\" href=\"News\">All news &rarr;</a></div><div class=\"hm-cards\">");
            foreach (Post p in list)
            {
                sb.Append("<a class=\"hm-nc\" href=\"News?post=" + Uri.EscapeDataString(p.Slug) + "\">" + Ui.Pic(p.Image, p.Title, string.Empty, true) + "<span class=\"hm-nc-b\"><small>" + Ui.Enc(Ui.NameOf(Ui.PostTags, p.Tag)) + " &middot; " + Ui.Day(p.Date) + "</small><b>" + Ui.Enc(p.Title) + "</b><span>" + Ui.Enc(p.Summary) + "</span></span></a>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string QuoteBand(SiteData site, Block b)
        {
            string tel = Regex.Replace(site.Settings.Phone, "[^0-9+]", string.Empty);
            return Sec(b, "<div class=\"hm-qband\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "hm-lead") + "</div><div class=\"hm-qside\"><a class=\"hm-qline\" href=\"tel:" + Ui.Attr(tel) + "\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6.5 3.5h3l1.5 4.5-2 1.5a11 11 0 0 0 5.5 5.5l1.5-2 4.5 1.5v3a2 2 0 0 1-2 2A16 16 0 0 1 4.5 5.5a2 2 0 0 1 2-2z\"/></svg><span><b>" + Ui.Enc(site.Settings.Phone) + "</b><small>Fitment desk</small></span></a><span class=\"hm-qline\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M12 7.5V12l3 2\"/></svg><span><b>" + TodayLine(site).Replace("<i></i>", string.Empty).Replace("<i class=\"hm-shut\"></i>", string.Empty) + "</b><small>" + Ui.Enc(site.Settings.TimeZone.Replace('_', ' ')) + "</small></span></span><a class=\"hm-btn hm-btn-accent\" href=\"Contact#quote\">" + Ui.Enc(b.Label != string.Empty ? b.Label : "Request a quote") + "</a></div></div>");
        }

        private static string Tok(SiteData site, string v)
        {
            return v.Replace("{name}", site.Settings.Name).Replace("{town}", site.Settings.Town);
        }

        private static string TokH(SiteData site, string v)
        {
            return v.Replace("{name}", Ui.Enc(site.Settings.Name)).Replace("{town}", Ui.Enc(site.Settings.Town));
        }

        private static string Lines(string v)
        {
            return Ui.Enc(v).Replace("\n", "<br>");
        }

        private static string Icon(string k)
        {
            return k switch
            {
                "gear" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"3.2\"/><path d=\"M12 2.8l1.6 2.6 3-0.7 0.8 3 2.9 1.1-1.1 2.9 1.9 2.4-2.4 1.9 0.3 3.1-3.1 0.3-1.3 2.8L12 20.6l-2.6 1.6-1.3-2.8-3.1-0.3 0.3-3.1-2.4-1.9 1.9-2.4L3.7 8.8l2.9-1.1 0.8-3 3 0.7z\"/></svg>",
                "wrench" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M14.5 6.5a4 4 0 0 0 5 5L9 22l-3-3L16.5 8.5a4 4 0 0 0-2-2z\"/><path d=\"M14.5 6.5L20 3l1 1-3.5 5.5\"/></svg>",
                "shield" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 3l7.5 3v5.5c0 4.5-3.2 8.2-7.5 9.5-4.3-1.3-7.5-5-7.5-9.5V6z\"/><path d=\"M8.5 12l2.5 2.5 4.5-5\"/></svg>",
                "truck" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M3 7h11v9H3zM14 10h4l3 3v3h-7z\"/><circle cx=\"7\" cy=\"17.5\" r=\"1.8\"/><circle cx=\"17.5\" cy=\"17.5\" r=\"1.8\"/></svg>",
                "factory" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M3 20V9l5 3V9l5 3V9l5 3V5h3v15z\"/><path d=\"M7 16h2M11 16h2M15 16h2\"/></svg>",
                "flask" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 3h6M10 3v6l-5.5 9a2 2 0 0 0 1.7 3h11.6a2 2 0 0 0 1.7-3L14 9V3\"/><path d=\"M7.5 15h9\"/></svg>",
                "bolt" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M13 2L5 13.5h6L10 22l9-11.5h-6z\"/></svg>",
                "ruler" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M3 16.5L16.5 3 21 7.5 7.5 21z\"/><path d=\"M7 13l1.5 1.5M10 10l1.5 1.5M13 7l1.5 1.5\"/></svg>",
                "check" => "<svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg>",
                "pin" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 21s-6.5-6-6.5-11a6.5 6.5 0 0 1 13 0c0 5-6.5 11-6.5 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.4\"/></svg>",
                "clock" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M12 7.5V12l3 2\"/></svg>",
                "phone" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6.5 3.5h3l1.5 4.5-2 1.5a11 11 0 0 0 5.5 5.5l1.5-2 4.5 1.5v3a2 2 0 0 1-2 2A16 16 0 0 1 4.5 5.5a2 2 0 0 1 2-2z\"/></svg>",
                "mail" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><rect x=\"3.5\" y=\"5.5\" width=\"17\" height=\"13\" rx=\"2\"/><path d=\"M4 7l8 6 8-6\"/></svg>",
                "doc" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 3h8l4 4v14H6z\"/><path d=\"M14 3v4h4M9 12h6M9 16h6\"/></svg>",
                "star" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 3.5l2.7 5.6 6.1 0.8-4.5 4.3 1.1 6.1L12 17.4l-5.4 2.9 1.1-6.1-4.5-4.3 6.1-0.8z\"/></svg>",
                "globe" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M3.5 12h17M12 3.5c3 3 3 14 0 17M12 3.5c-3 3-3 14 0 17\"/></svg>",
                _ => string.Empty
            };
        }

        private static string Mv(SiteData site, string src, string alt, bool bg, bool eager)
        {
            if (src == string.Empty)
            {
                return string.Empty;
            }
            MediaItem? m = site.Media.FirstOrDefault(x => x.Full == src);
            int w = m != null && m.Width > 0 ? m.Width : 1600;
            int h = m != null && m.Height > 0 ? m.Height : 1200;
            if (Ui.IsVideo(src))
            {
                return "<video src=\"" + Ui.Attr(src) + "\" poster=\"" + Ui.Attr(m?.Thumb ?? string.Empty) + "\" width=\"" + w + "\" height=\"" + h + "\" playsinline " + (bg ? "autoplay muted loop data-bg=\"1\" aria-hidden=\"true\"" : "controls preload=\"none\" aria-label=\"" + Ui.Attr(alt) + "\"") + "></video>";
            }
            return "<img src=\"" + Ui.Attr(src) + "\" alt=\"" + Ui.Attr(m != null && m.Alt != string.Empty ? m.Alt : alt) + "\" width=\"" + w + "\" height=\"" + h + "\"" + (eager ? string.Empty : " loading=\"lazy\"") + ">";
        }

        private static string Btn(SiteData site, string label, string link, bool main)
        {
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"hm-btn " + (main ? "hm-btn-accent" : "hm-btn-ghost") + "\" href=\"" + Ui.Attr(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Enc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"hm-hero-b\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<span class=\"hm-kick\">" + Ui.Enc(Tok(site, b.Kicker)) + "</span>";
        }

        private static string H2(SiteData site, Block b)
        {
            return b.Title == string.Empty ? string.Empty : "<h2 class=\"hm-h2\">" + Ui.Enc(Tok(site, b.Title)) + "</h2>";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"hm-rich " + cls + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead)
        {
            string x = Kick(site, b) + H2(site, b) + (lead ? Rich(site, b.Text, "hm-lead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"hm-bhead\">" + x + "</div>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"hm-sec hm-wrap\" id=\"" + Ui.Attr(b.Id) + "\">" + inner + "</section>";
        }

        private static string Finder(SiteData site, string year, string make, string model)
        {
            List<(string, string)> years = new List<(string, string)>();
            for (int y = site.Settings.YearTo; y >= site.Settings.YearFrom; y--)
            {
                years.Add((y.ToString(Ui.Inv), y.ToString(Ui.Inv)));
            }
            string makes = JsonSerializer.Serialize(site.Makes.Select(m => new { key = m.Key, name = m.Name, models = m.Models.Select(x => new { key = x.Key, name = x.Name }) }));
            return "<form class=\"hm-find\" action=\"Products\" method=\"get\" data-makes=\"" + Ui.Attr(makes) + "\"><label class=\"hm-field\"><span>Year</span><select name=\"year\"><option value=\"\">Any year</option>" + Ui.Opts(years, year) + "</select></label><label class=\"hm-field\"><span>Make</span><select name=\"make\"><option value=\"\">Any make</option>" + Ui.Opts(site.Makes.Select(m => (m.Key, m.Name)), make) + "</select></label><label class=\"hm-field\"><span>Model</span><select name=\"model\" data-cur=\"" + Ui.Attr(model) + "\" disabled><option value=\"\">Any model</option></select></label><button type=\"submit\" class=\"hm-btn\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"6.5\"/><path d=\"M16 16l4.5 4.5\"/></svg> Find parts that fit</button></form>";
        }

        private static string Stats(SiteData site, DateTime today)
        {
            return "<div class=\"hm-hstats\"><a href=\"Products\"><b>" + site.Products.Count(Ui.Public) + "</b><span>products in the catalog</span></a><a href=\"Dealers\"><b>" + site.Dealers.Count(d => d.Active) + "</b><span>dealers and installers</span></a><a href=\"About\"><b>" + Math.Max(1, today.Year - site.Settings.Founded) + "</b><span>years in " + Ui.Enc(site.Settings.Town.Split(',')[0]) + "</span></a></div>";
        }

        private static string Hero(SiteData site, Block b, DateTime today, bool h1, string title, string text)
        {
            bool find = b.Extra == "finder";
            string tag = h1 ? "h1" : "h2";
            string media = Mv(site, b.Media, Tok(site, b.Title), Ui.IsVideo(b.Media), h1);
            string t = title != string.Empty ? Ui.Enc(title) : Ui.Enc(Tok(site, b.Title));
            string lead = text != string.Empty ? "<p class=\"hm-lead\">" + Ui.Enc(text) + "</p>" : Rich(site, b.Text, "hm-lead");
            return "<section class=\"hm-bhero" + (find ? " hm-bhome" : string.Empty) + (media == string.Empty ? " hm-bplain" : string.Empty) + "\" id=\"" + Ui.Attr(b.Id) + "\"><div class=\"hm-wrap hm-bhero-in\"><div class=\"hm-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"hm-btitle\">" + t + "</" + tag + ">" + lead + Btns(site, b) + (find ? Finder(site, string.Empty, string.Empty, string.Empty) : string.Empty) + "</div>" + (media != string.Empty ? "<div class=\"hm-bhero-p\">" + media + (find ? Stats(site, today) : string.Empty) + "</div>" : string.Empty) + "</div></section>";
        }

        private static string Split(SiteData site, Block b, DateTime today)
        {
            string counts = b.Extra == "counts" ? "<div class=\"hm-nums\"><span><b>" + site.Products.Count(Ui.Public) + "</b>products</span><span><b>" + site.Dealers.Count(d => d.Active) + "</b>dealers</span><span><b>" + Math.Max(1, today.Year - site.Settings.Founded) + "</b>years</span></div>" : string.Empty;
            string btn = Btn(site, b.Label, b.Link, false);
            return Sec(b, "<div class=\"hm-bsplit" + (b.Flip ? " hm-flip" : string.Empty) + (b.Media == string.Empty ? " hm-nomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"hm-bsm\">" + Mv(site, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "hm-lead") + counts + (btn != string.Empty ? "<div class=\"hm-hero-b\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            if (b.Items.Count == 6)
            {
                n = 3;
            }
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"hm-bcards hm-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"hm-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Enc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"hm-bcard\" href=\"" + Ui.Attr(it.Link) + "\">" + inner + "<span class=\"hm-more\">" + Ui.Enc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"hm-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"hm-bsteps hm-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
            int i = 1;
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<li><span>" + i++ + "</span><b>" + Ui.Enc(Tok(site, it.Title)) + "</b><p>" + Lines(Tok(site, it.Text)) + "</p></li>");
            }
            sb.Append("</ol>");
            return Sec(b, sb.ToString());
        }

        private static string StatRow(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"hm-bstats hm-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div><b>" + Ui.Enc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string People(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"hm-bpeople hm-n" + Math.Clamp(b.Items.Count, 1, 4) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"hm-person\">" + (it.Media != string.Empty && !Ui.IsVideo(it.Media) ? Mv(site, it.Media, it.Title, false, false) : "<span class=\"hm-noface\" style=\"--h:" + Ui.Hue(it.Title) + "\">" + Ui.Enc(Ui.Initials(it.Title)) + "</span>") + "<b>" + Ui.Enc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Boxes(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"hm-bboxes hm-n" + Math.Clamp(b.Items.Count, 1, 3) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"hm-box\">" + (it.Title != string.Empty ? "<h2>" + Ui.Enc(Tok(site, it.Title)) + "</h2>" : string.Empty) + "<ul class=\"hm-bticks\">");
                foreach (string line in it.Text.Split('\n').Select(x => x.Trim()).Where(x => x != string.Empty))
                {
                    sb.Append("<li><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg><span>" + Ui.Enc(Tok(site, line)) + "</span></li>");
                }
                sb.Append("</ul></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Faq(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder("<div class=\"hm-bfaq\">" + Head(site, b, false) + "<div class=\"hm-faq\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details><summary>" + Ui.Enc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Quote(SiteData site, Block b)
        {
            return Sec(b, "<figure class=\"hm-bquote\">" + (b.Media != string.Empty && !Ui.IsVideo(b.Media) ? Mv(site, b.Media, b.Title, false, false) : string.Empty) + "<blockquote>" + Lines(Tok(site, b.Text)) + "</blockquote>" + (b.Title != string.Empty ? "<figcaption>" + Ui.Enc(Tok(site, b.Title)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Wide(SiteData site, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"hm-bmedia\">" + Mv(site, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Enc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"hm-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "hm-lead") + "</div>" + Btns(site, b) + "</div>");
        }

        private string Page(SiteData site, string key, DateTime today, string title = "", string text = "")
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            if (!site.Pages.TryGetValue(key, out PageDoc? doc))
            {
                return string.Empty;
            }
            foreach (Block b in doc.Blocks.Where(x => !x.Hidden))
            {
                string html = b.Type switch
                {
                    "hero" => Hero(site, b, today, first, first ? title : string.Empty, first ? text : string.Empty),
                    "text" => Sec(b, "<div class=\"hm-btext\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + "</div>"),
                    "split" => Split(site, b, today),
                    "cards" => Cards(site, b),
                    "steps" => Steps(site, b),
                    "stats" => StatRow(site, b),
                    "people" => People(site, b),
                    "boxes" => Boxes(site, b),
                    "faq" => Faq(site, b),
                    "quote" => Quote(site, b),
                    "media" => Wide(site, b),
                    "cta" => Cta(site, b),
                    "data" => DataPart(site, b, today),
                    _ => string.Empty
                };
                if (b.Type == "hero")
                {
                    first = false;
                }
                sb.Append(html);
            }
            return sb.ToString();
        }
    }
}
