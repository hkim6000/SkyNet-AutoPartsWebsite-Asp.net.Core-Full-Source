using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class Contact : WebPage
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
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--ct-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_nav}", Nav("ct", v.Nav, false))
                .Replace("{plhd_drawer}", Nav("ct", v.Nav, true))
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
                return "<i class=\"ct-shut\"></i>Fitment desk opens today at " + Ui.HmText(site, d!.Start);
            }
            for (int i = 1; i <= 7; i++)
            {
                DateTime n = now.Date.AddDays(i);
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)n.DayOfWeek);
                if (o != null && o.Open)
                {
                    return "<i class=\"ct-shut\"></i>Closed now &middot; opens " + (i == 1 ? "tomorrow" : Ui.DayNames[(int)n.DayOfWeek]) + " at " + Ui.HmText(site, o.Start);
                }
            }
            return "<i class=\"ct-shut\"></i>Closed";
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
                response.SetElementContents("ct-e-nlmail", "Enter an email address like name@example.com.");
                return response;
            }
            bool added = await Store.AddSubscriber(mail);
            response.ExecuteScript("ContactJs.subscribed('" + (added ? "Thanks. Product news lands about once a month." : "You are already on the list.") + "');");
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
            return "<article class=\"ct-pc\" data-pid=\"" + Ui.Attr(p.Id) + "\"><a class=\"ct-pc-img\" href=\"" + href + "\" tabindex=\"-1\" aria-hidden=\"true\">" + Ui.Pic(Ui.Cover(site, p), string.Empty, string.Empty, true) + (text != string.Empty ? "<span class=\"ct-badge ct-b-" + cls + "\">" + text + "</span>" : string.Empty) + "</a><button type=\"button\" class=\"ct-cmpb\" data-cmp=\"" + Ui.Attr(p.Id) + "\" aria-pressed=\"false\" onclick=\"ContactJs.cmp(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 4v16M15 4v16\"/><path d=\"M4 9h5M15 9h5M4 15h5M15 15h5\"/></svg><span>Compare</span></button><div class=\"ct-pc-b\"><span class=\"ct-pcat\"><span>" + Ui.Enc(Ui.CategoryName(p.Category)) + "</span><em>" + Ui.Enc(Ui.SeriesName(p.Series)) + "</em></span><span class=\"ct-pname\"><a href=\"" + href + "\">" + Ui.Enc(p.Name) + "</a></span><span class=\"ct-pfit\">" + FitSummary(site, p) + "</span><span class=\"ct-pc-f\"><span class=\"ct-price\">" + Ui.Money(p.Price) + "<small>MSRP</small></span>" + Ui.Pill("ct", Ui.Stocks, p.Stock) + "</span></div></article>";
        }

        private async Task<View> Render(SiteData site)
        {
            DateTime today = Store.Today(site);
            return new View { Title = "Contact", Desc = "Request a quote, send a message or start a warranty claim with " + site.Settings.Name + ".", Main = Page(site, "contact", today), Nav = "contact" };
        }

        private string DataPart(SiteData site, Block b, DateTime today)
        {
            if (b.Source != "contact")
            {
                return string.Empty;
            }
            string tel = Regex.Replace(site.Settings.Phone, "[^0-9+]", string.Empty);
            string sku = Q("sku", 30);
            string topic = Ui.Has(Ui.ContactTopics, Q("topic", 20)) ? Q("topic", 20) : "general";
            StringBuilder sb = new StringBuilder("<div class=\"ct-cwrap\"><div class=\"ct-cinfo\"><div class=\"ct-box\"><h2>" + Ui.Enc(site.Settings.Name) + "</h2><p class=\"ct-cl\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 21s-6.5-6-6.5-11a6.5 6.5 0 0 1 13 0c0 5-6.5 11-6.5 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.4\"/></svg> <span>" + Ui.Enc(site.Settings.Address) + "</span></p><p class=\"ct-cl\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6.5 3.5h3l1.5 4.5-2 1.5a11 11 0 0 0 5.5 5.5l1.5-2 4.5 1.5v3a2 2 0 0 1-2 2A16 16 0 0 1 4.5 5.5a2 2 0 0 1 2-2z\"/></svg> <a href=\"tel:" + Ui.Attr(tel) + "\">" + Ui.Enc(site.Settings.Phone) + "</a></p><p class=\"ct-cl\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><rect x=\"3.5\" y=\"5.5\" width=\"17\" height=\"13\" rx=\"2\"/><path d=\"M4 7l8 6 8-6\"/></svg> <a href=\"mailto:" + Ui.Attr(site.Settings.Email) + "\">" + Ui.Enc(site.Settings.Email) + "</a></p><h3 style=\"margin-top:18px\">Office hours</h3><div class=\"ct-hours\">" + HoursRows(site, "ct") + "</div><p class=\"ct-hint\">" + TodayLine(site).Replace("<i></i>", string.Empty).Replace("<i class=\"ct-shut\"></i>", string.Empty) + " &middot; " + Ui.Enc(site.Settings.TimeZone.Replace('_', ' ')) + "</p></div>" + (b.Title != string.Empty || b.Text != string.Empty ? "<div class=\"ct-info-box ct-cnote\">" + (b.Kicker != string.Empty ? "<b>" + Ui.Enc(Tok(site, b.Kicker)) + "</b>" : string.Empty) + Rich(site, b.Text, string.Empty) + "</div>" : string.Empty) + "</div>");
            sb.Append("<div class=\"ct-cforms\" id=\"ct-cforms\"><div class=\"ct-tabs\" role=\"tablist\"><button type=\"button\" class=\"ct-tab ct-act\" data-p=\"quote\" role=\"tab\" aria-selected=\"true\" onclick=\"ContactJs.tab('quote')\">Request a quote</button><button type=\"button\" class=\"ct-tab\" data-p=\"msg\" role=\"tab\" aria-selected=\"false\" onclick=\"ContactJs.tab('msg')\">Send a message</button><button type=\"button\" class=\"ct-tab\" data-p=\"war\" role=\"tab\" aria-selected=\"false\" onclick=\"ContactJs.tab('war')\">Warranty claim</button></div><div class=\"ct-panes\">");
            sb.Append("<div data-p=\"quote\" class=\"ct-act\" id=\"quote\"><p class=\"ct-hint\" style=\"margin-bottom:12px\">" + Ui.Enc(site.Settings.QuoteText) + "</p><form class=\"ct-form\" id=\"ct-qf\" onsubmit=\"return ContactJs.quoteSend()\" novalidate><span class=\"ct-flabel\">Parts</span><div id=\"ct-lines\">" + Line(site, sku, 1) + "</div><button type=\"button\" class=\"ct-addl\" onclick=\"ContactJs.addLine()\">+ Add another part</button><template id=\"ct-ltpl\">" + Line(site, string.Empty, 1) + "</template><em class=\"ct-err\" id=\"ct-e-lines\" role=\"alert\"></em><div class=\"ct-row2\"><label class=\"ct-field\"><span>Your name</span><input type=\"text\" id=\"ct-qname\" maxlength=\"80\" autocomplete=\"name\"></label><label class=\"ct-field\"><span>Company <small>(optional, write Fleet for fleet pricing)</small></span><input type=\"text\" id=\"ct-qcomp\" maxlength=\"80\" autocomplete=\"organization\"></label></div><em class=\"ct-err\" id=\"ct-e-qname\" role=\"alert\"></em><div class=\"ct-row2\"><label class=\"ct-field\"><span>Email</span><input type=\"email\" id=\"ct-qmail\" maxlength=\"120\" autocomplete=\"email\"></label><label class=\"ct-field\"><span>Phone <small>(optional)</small></span><input type=\"tel\" id=\"ct-qphone\" maxlength=\"30\" autocomplete=\"tel\"></label></div><em class=\"ct-err\" id=\"ct-e-qmail\" role=\"alert\"></em><div class=\"ct-row2\"><label class=\"ct-field\"><span>Your vehicle <small>(optional)</small></span><input type=\"text\" id=\"ct-qveh\" maxlength=\"80\" placeholder=\"Year, make and model\"></label><label class=\"ct-field\"><span>State</span><select id=\"ct-qstate\"><option value=\"\">Choose</option>" + Ui.Opts(Ui.States.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label></div><label class=\"ct-field\"><span>Anything else <small>(optional)</small></span><textarea id=\"ct-qmsg\" rows=\"3\" maxlength=\"1000\" placeholder=\"Ship to a dealer, a deadline, a part that is not in the list\"></textarea></label><label class=\"ct-hp\" aria-hidden=\"true\">Website<input type=\"text\" id=\"ct-qweb\" tabindex=\"-1\" autocomplete=\"off\"></label><div><button type=\"submit\" class=\"ct-btn ct-btn-accent\" id=\"ct-qsend\">Send quote request</button></div></form><div id=\"ct-qdone\"></div></div>");
            sb.Append("<div data-p=\"msg\"><form class=\"ct-form\" id=\"ct-mf\" onsubmit=\"return ContactJs.msgSend()\" novalidate><div class=\"ct-row2\"><label class=\"ct-field\"><span>Topic</span><select id=\"ct-mtopic\">" + Ui.Opts(Ui.ContactTopics.Select(t => (t.Key, t.Name)), topic) + "</select></label><label class=\"ct-field\"><span>Your name</span><input type=\"text\" id=\"ct-mname\" maxlength=\"80\" autocomplete=\"name\"></label></div><em class=\"ct-err\" id=\"ct-e-mname\" role=\"alert\"></em><div class=\"ct-row2\"><label class=\"ct-field\"><span>Email</span><input type=\"email\" id=\"ct-mmail\" maxlength=\"120\" autocomplete=\"email\"></label><label class=\"ct-field\"><span>Phone <small>(optional)</small></span><input type=\"tel\" id=\"ct-mphone\" maxlength=\"30\" autocomplete=\"tel\"></label></div><em class=\"ct-err\" id=\"ct-e-mmail\" role=\"alert\"></em><label class=\"ct-field\"><span>Message</span><textarea id=\"ct-mmsg\" rows=\"6\" maxlength=\"2000\"></textarea></label><em class=\"ct-err\" id=\"ct-e-mmsg\" role=\"alert\"></em><label class=\"ct-hp\" aria-hidden=\"true\">Website<input type=\"text\" id=\"ct-mweb\" tabindex=\"-1\" autocomplete=\"off\"></label><div><button type=\"submit\" class=\"ct-btn ct-btn-accent\" id=\"ct-msend\">Send message</button></div></form><div id=\"ct-mdone\"></div></div>");
            sb.Append("<div data-p=\"war\"><p class=\"ct-hint\" style=\"margin-bottom:12px\">" + Ui.Enc(site.Settings.WarrantyText) + "</p><form class=\"ct-form\" id=\"ct-wf\" onsubmit=\"return ContactJs.warSend()\" novalidate><div class=\"ct-row3\"><label class=\"ct-field\"><span>Part number</span><input type=\"text\" id=\"ct-wsku\" maxlength=\"30\" list=\"ct-skus\" placeholder=\"IV-CO-1010\" value=\"" + Ui.Attr(sku) + "\"></label>" + Ui.DataList("ct-skus", site.Products.Where(Ui.Public).Select(p => p.Sku)) + "<label class=\"ct-field\"><span>Purchase date</span><input type=\"date\" id=\"ct-wdate\"></label><label class=\"ct-field\"><span>Bought from</span><input type=\"text\" id=\"ct-wdealer\" maxlength=\"80\" list=\"ct-dealers\" placeholder=\"Dealer or direct\"></label>" + Ui.DataList("ct-dealers", site.Dealers.Where(d => d.Active).Select(d => d.Name)) + "</div><em class=\"ct-err\" id=\"ct-e-wsku\" role=\"alert\"></em><div class=\"ct-row2\"><label class=\"ct-field\"><span>Your name</span><input type=\"text\" id=\"ct-wname\" maxlength=\"80\" autocomplete=\"name\"></label><label class=\"ct-field\"><span>Email</span><input type=\"email\" id=\"ct-wmail\" maxlength=\"120\" autocomplete=\"email\"></label></div><em class=\"ct-err\" id=\"ct-e-wname\" role=\"alert\"></em><em class=\"ct-err\" id=\"ct-e-wmail\" role=\"alert\"></em><div class=\"ct-row2\"><label class=\"ct-field\"><span>Phone <small>(optional)</small></span><input type=\"tel\" id=\"ct-wphone\" maxlength=\"30\" autocomplete=\"tel\"></label><label class=\"ct-field\"><span>Vehicle</span><input type=\"text\" id=\"ct-wveh\" maxlength=\"80\" placeholder=\"Year, make and model\"></label></div><label class=\"ct-field\"><span>What happened</span><textarea id=\"ct-wmsg\" rows=\"5\" maxlength=\"2000\" placeholder=\"When it started, mileage since installation, what you have checked\"></textarea></label><em class=\"ct-err\" id=\"ct-e-wmsg\" role=\"alert\"></em><label class=\"ct-hp\" aria-hidden=\"true\">Website<input type=\"text\" id=\"ct-wweb\" tabindex=\"-1\" autocomplete=\"off\"></label><div><button type=\"submit\" class=\"ct-btn ct-btn-accent\" id=\"ct-wsend\">Start the claim</button></div><p class=\"ct-hint\">We answer within two business days. Photos can follow by email once you have a claim number.</p></form><div id=\"ct-wdone\"></div></div>");
            sb.Append("</div></div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Line(SiteData site, string sku, int qty)
        {
            string cur = site.Products.FirstOrDefault(p => Ui.Public(p) && p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase))?.Id ?? string.Empty;
            StringBuilder sb = new StringBuilder("<div class=\"ct-line\"><label class=\"ct-field\"><span class=\"ct-sr\">Part</span><select data-l=\"product\"><option value=\"\">Choose a product</option>");
            foreach ((string k, string n) in Ui.Categories)
            {
                List<Part> list = site.Products.Where(p => Ui.Public(p) && p.Category == k).OrderBy(p => p.Name).ToList();
                if (list.Count == 0)
                {
                    continue;
                }
                sb.Append("<optgroup label=\"" + Ui.Attr(n) + "\">" + Ui.Opts(list.Select(p => (p.Id, p.Sku + " · " + p.Name)), cur) + "</optgroup>");
            }
            sb.Append("</select></label><label class=\"ct-field ct-lq\"><span class=\"ct-sr\">Quantity</span><input type=\"number\" data-l=\"qty\" min=\"1\" max=\"999\" value=\"" + qty + "\" aria-label=\"Quantity\"></label><button type=\"button\" class=\"ct-ibtn\" aria-label=\"Remove line\" onclick=\"ContactJs.dropLine(this)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3\"/></svg></button></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> QuoteSend()
        {
            ApiResponse response = new ApiResponse();
            string name = Regex.Replace(Ui.Clip(GetDataValue("qname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("qmail"), 120).Trim();
            List<QuoteLine> items = new List<QuoteLine>();
            foreach (string part in GetDataValue("qitems").Split('|'))
            {
                string[] kv = part.Split(':');
                if (kv.Length == 2 && kv[0] != string.Empty && int.TryParse(kv[1], out int qn))
                {
                    items.Add(new QuoteLine { Product = Ui.Clip(kv[0], 20), Qty = Math.Clamp(qn, 1, 999) });
                }
            }
            if (items.Count == 0)
            {
                response.ExecuteScript("ContactJs.fieldErr('lines','Choose at least one product.');ContactJs.quoted(false);");
                return response;
            }
            if (name.Length < 2)
            {
                response.ExecuteScript("ContactJs.fieldErr('qname','Please enter your name.');ContactJs.quoted(false);");
                return response;
            }
            if (!Ui.Email(mail))
            {
                response.ExecuteScript("ContactJs.fieldErr('qmail','Enter an email address like name@example.com.');ContactJs.quoted(false);");
                return response;
            }
            string id = string.Empty;
            if (Ui.Clip(GetDataValue("qweb"), 50) == string.Empty)
            {
                using (await Store.Hold())
                {
                    SiteData site = await Store.LoadSite();
                    items = items.Where(i => site.Products.Any(p => p.Id == i.Product && Ui.Public(p))).Take(12).ToList();
                    if (items.Count == 0)
                    {
                        response.ExecuteScript("ContactJs.fieldErr('lines','Choose at least one product.');ContactJs.quoted(false);");
                        return response;
                    }
                    Inquiry q = new Inquiry { Id = Store.NextId(site.Inquiries.Select(x => x.Id), "Q-", 5000), Kind = "quote", Name = name, Company = Ui.Clip(GetDataValue("qcomp"), 80), Email = mail, Phone = Ui.CleanPhone(GetDataValue("qphone")), State = Ui.Has(Ui.States, GetDataValue("qstate")) ? GetDataValue("qstate") : string.Empty, Vehicle = Ui.Clip(GetDataValue("qveh"), 80), Items = items, Message = Ui.Clip(GetDataValue("qmsg"), 1000), Created = DateTime.UtcNow };
                    site.Inquiries.Add(q);
                    id = q.Id;
                    await Store.SaveSite(site);
                }
                await Store.Log("web", "quote", id + " · " + name, true);
            }
            response.ExecuteScript("ContactJs.quoted(true,'" + Ui.Js("Thanks, " + name.Split(' ')[0] + ". Your request number is " + id + ". The fitment desk answers within one business day.") + "');");
            return response;
        }

        public async Task<ApiResponse> MsgSend()
        {
            ApiResponse response = new ApiResponse();
            string name = Regex.Replace(Ui.Clip(GetDataValue("mname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("mmail"), 120).Trim();
            string msg = Ui.Clip(GetDataValue("mmsg"), 2000).Trim();
            if (name.Length < 2)
            {
                response.ExecuteScript("ContactJs.fieldErr('mname','Please enter your name.');ContactJs.messaged(false);");
                return response;
            }
            if (!Ui.Email(mail))
            {
                response.ExecuteScript("ContactJs.fieldErr('mmail','Enter an email address like name@example.com.');ContactJs.messaged(false);");
                return response;
            }
            if (msg.Length < 5)
            {
                response.ExecuteScript("ContactJs.fieldErr('mmsg','Please write your message.');ContactJs.messaged(false);");
                return response;
            }
            if (Ui.Clip(GetDataValue("mweb"), 50) == string.Empty)
            {
                using (await Store.Hold())
                {
                    SiteData site = await Store.LoadSite();
                    if (!site.Inquiries.Any(x => x.Email.Equals(mail, StringComparison.OrdinalIgnoreCase) && x.Message == msg))
                    {
                        site.Inquiries.Add(new Inquiry { Id = Store.NextId(site.Inquiries.Select(x => x.Id), "Q-", 5000), Kind = "contact", Name = name, Email = mail, Phone = Ui.CleanPhone(GetDataValue("mphone")), Topic = Ui.Has(Ui.ContactTopics, GetDataValue("mtopic")) ? GetDataValue("mtopic") : "general", Message = msg, Created = DateTime.UtcNow });
                        await Store.SaveSite(site);
                    }
                }
                await Store.Log("web", "message", name, true);
            }
            response.ExecuteScript("ContactJs.messaged(true,'" + Ui.Js("Thanks, " + name.Split(' ')[0] + ". We answer messages within one business day.") + "');");
            return response;
        }

        public async Task<ApiResponse> WarSend()
        {
            ApiResponse response = new ApiResponse();
            string sku = Ui.Clip(GetDataValue("wsku"), 30).ToUpperInvariant();
            string name = Regex.Replace(Ui.Clip(GetDataValue("wname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("wmail"), 120).Trim();
            string msg = Ui.Clip(GetDataValue("wmsg"), 2000).Trim();
            DateTime? bought = Ui.ParseDay(GetDataValue("wdate"));
            if (sku.Length < 3)
            {
                response.ExecuteScript("ContactJs.fieldErr('wsku','Enter the part number from the box or the serial tag.');ContactJs.claimed(false);");
                return response;
            }
            if (name.Length < 2)
            {
                response.ExecuteScript("ContactJs.fieldErr('wname','Please enter your name.');ContactJs.claimed(false);");
                return response;
            }
            if (!Ui.Email(mail))
            {
                response.ExecuteScript("ContactJs.fieldErr('wmail','Enter an email address like name@example.com.');ContactJs.claimed(false);");
                return response;
            }
            if (msg.Length < 10)
            {
                response.ExecuteScript("ContactJs.fieldErr('wmsg','Describe what happened so we can help.');ContactJs.claimed(false);");
                return response;
            }
            string id = string.Empty;
            if (Ui.Clip(GetDataValue("wweb"), 50) == string.Empty)
            {
                using (await Store.Hold())
                {
                    SiteData site = await Store.LoadSite();
                    Inquiry q = new Inquiry { Id = Store.NextId(site.Inquiries.Select(x => x.Id), "Q-", 5000), Kind = "warranty", Sku = sku, Purchased = bought, DealerName = Ui.Clip(GetDataValue("wdealer"), 80), Name = name, Email = mail, Phone = Ui.CleanPhone(GetDataValue("wphone")), Vehicle = Ui.Clip(GetDataValue("wveh"), 80), Message = msg, Created = DateTime.UtcNow };
                    site.Inquiries.Add(q);
                    id = q.Id;
                    await Store.SaveSite(site);
                }
                await Store.Log("web", "warranty", id + " · " + sku, true);
            }
            response.ExecuteScript("ContactJs.claimed(true,'" + Ui.Js("Thanks, " + name.Split(' ')[0] + ". Your claim number is " + id + ". Reply to our email with photos if you have them.") + "');");
            return response;
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
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"ct-btn " + (main ? "ct-btn-accent" : "ct-btn-ghost") + "\" href=\"" + Ui.Attr(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Enc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"ct-hero-b\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<span class=\"ct-kick\">" + Ui.Enc(Tok(site, b.Kicker)) + "</span>";
        }

        private static string H2(SiteData site, Block b)
        {
            return b.Title == string.Empty ? string.Empty : "<h2 class=\"ct-h2\">" + Ui.Enc(Tok(site, b.Title)) + "</h2>";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"ct-rich " + cls + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead)
        {
            string x = Kick(site, b) + H2(site, b) + (lead ? Rich(site, b.Text, "ct-lead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"ct-bhead\">" + x + "</div>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"ct-sec ct-wrap\" id=\"" + Ui.Attr(b.Id) + "\">" + inner + "</section>";
        }

        private static string Finder(SiteData site, string year, string make, string model)
        {
            List<(string, string)> years = new List<(string, string)>();
            for (int y = site.Settings.YearTo; y >= site.Settings.YearFrom; y--)
            {
                years.Add((y.ToString(Ui.Inv), y.ToString(Ui.Inv)));
            }
            string makes = JsonSerializer.Serialize(site.Makes.Select(m => new { key = m.Key, name = m.Name, models = m.Models.Select(x => new { key = x.Key, name = x.Name }) }));
            return "<form class=\"ct-find\" action=\"Products\" method=\"get\" data-makes=\"" + Ui.Attr(makes) + "\"><label class=\"ct-field\"><span>Year</span><select name=\"year\"><option value=\"\">Any year</option>" + Ui.Opts(years, year) + "</select></label><label class=\"ct-field\"><span>Make</span><select name=\"make\"><option value=\"\">Any make</option>" + Ui.Opts(site.Makes.Select(m => (m.Key, m.Name)), make) + "</select></label><label class=\"ct-field\"><span>Model</span><select name=\"model\" data-cur=\"" + Ui.Attr(model) + "\" disabled><option value=\"\">Any model</option></select></label><button type=\"submit\" class=\"ct-btn\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"6.5\"/><path d=\"M16 16l4.5 4.5\"/></svg> Find parts that fit</button></form>";
        }

        private static string Stats(SiteData site, DateTime today)
        {
            return "<div class=\"ct-hstats\"><a href=\"Products\"><b>" + site.Products.Count(Ui.Public) + "</b><span>products in the catalog</span></a><a href=\"Dealers\"><b>" + site.Dealers.Count(d => d.Active) + "</b><span>dealers and installers</span></a><a href=\"About\"><b>" + Math.Max(1, today.Year - site.Settings.Founded) + "</b><span>years in " + Ui.Enc(site.Settings.Town.Split(',')[0]) + "</span></a></div>";
        }

        private static string Hero(SiteData site, Block b, DateTime today, bool h1, string title, string text)
        {
            bool find = b.Extra == "finder";
            string tag = h1 ? "h1" : "h2";
            string media = Mv(site, b.Media, Tok(site, b.Title), Ui.IsVideo(b.Media), h1);
            string t = title != string.Empty ? Ui.Enc(title) : Ui.Enc(Tok(site, b.Title));
            string lead = text != string.Empty ? "<p class=\"ct-lead\">" + Ui.Enc(text) + "</p>" : Rich(site, b.Text, "ct-lead");
            return "<section class=\"ct-bhero" + (find ? " ct-bhome" : string.Empty) + (media == string.Empty ? " ct-bplain" : string.Empty) + "\" id=\"" + Ui.Attr(b.Id) + "\"><div class=\"ct-wrap ct-bhero-in\"><div class=\"ct-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"ct-btitle\">" + t + "</" + tag + ">" + lead + Btns(site, b) + (find ? Finder(site, string.Empty, string.Empty, string.Empty) : string.Empty) + "</div>" + (media != string.Empty ? "<div class=\"ct-bhero-p\">" + media + (find ? Stats(site, today) : string.Empty) + "</div>" : string.Empty) + "</div></section>";
        }

        private static string Split(SiteData site, Block b, DateTime today)
        {
            string counts = b.Extra == "counts" ? "<div class=\"ct-nums\"><span><b>" + site.Products.Count(Ui.Public) + "</b>products</span><span><b>" + site.Dealers.Count(d => d.Active) + "</b>dealers</span><span><b>" + Math.Max(1, today.Year - site.Settings.Founded) + "</b>years</span></div>" : string.Empty;
            string btn = Btn(site, b.Label, b.Link, false);
            return Sec(b, "<div class=\"ct-bsplit" + (b.Flip ? " ct-flip" : string.Empty) + (b.Media == string.Empty ? " ct-nomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"ct-bsm\">" + Mv(site, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "ct-lead") + counts + (btn != string.Empty ? "<div class=\"ct-hero-b\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            if (b.Items.Count == 6)
            {
                n = 3;
            }
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"ct-bcards ct-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"ct-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Enc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"ct-bcard\" href=\"" + Ui.Attr(it.Link) + "\">" + inner + "<span class=\"ct-more\">" + Ui.Enc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"ct-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"ct-bsteps ct-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
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
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"ct-bstats ct-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div><b>" + Ui.Enc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string People(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"ct-bpeople ct-n" + Math.Clamp(b.Items.Count, 1, 4) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"ct-person\">" + (it.Media != string.Empty && !Ui.IsVideo(it.Media) ? Mv(site, it.Media, it.Title, false, false) : "<span class=\"ct-noface\" style=\"--h:" + Ui.Hue(it.Title) + "\">" + Ui.Enc(Ui.Initials(it.Title)) + "</span>") + "<b>" + Ui.Enc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Boxes(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"ct-bboxes ct-n" + Math.Clamp(b.Items.Count, 1, 3) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"ct-box\">" + (it.Title != string.Empty ? "<h2>" + Ui.Enc(Tok(site, it.Title)) + "</h2>" : string.Empty) + "<ul class=\"ct-bticks\">");
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
            StringBuilder sb = new StringBuilder("<div class=\"ct-bfaq\">" + Head(site, b, false) + "<div class=\"ct-faq\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details><summary>" + Ui.Enc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Quote(SiteData site, Block b)
        {
            return Sec(b, "<figure class=\"ct-bquote\">" + (b.Media != string.Empty && !Ui.IsVideo(b.Media) ? Mv(site, b.Media, b.Title, false, false) : string.Empty) + "<blockquote>" + Lines(Tok(site, b.Text)) + "</blockquote>" + (b.Title != string.Empty ? "<figcaption>" + Ui.Enc(Tok(site, b.Title)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Wide(SiteData site, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"ct-bmedia\">" + Mv(site, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Enc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"ct-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "ct-lead") + "</div>" + Btns(site, b) + "</div>");
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
                    "text" => Sec(b, "<div class=\"ct-btext\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + "</div>"),
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
