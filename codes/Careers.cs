using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class Careers : WebPage
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
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--cr-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_nav}", Nav("cr", v.Nav, false))
                .Replace("{plhd_drawer}", Nav("cr", v.Nav, true))
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
                return "<i class=\"cr-shut\"></i>Fitment desk opens today at " + Ui.HmText(site, d!.Start);
            }
            for (int i = 1; i <= 7; i++)
            {
                DateTime n = now.Date.AddDays(i);
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)n.DayOfWeek);
                if (o != null && o.Open)
                {
                    return "<i class=\"cr-shut\"></i>Closed now &middot; opens " + (i == 1 ? "tomorrow" : Ui.DayNames[(int)n.DayOfWeek]) + " at " + Ui.HmText(site, o.Start);
                }
            }
            return "<i class=\"cr-shut\"></i>Closed";
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
                response.SetElementContents("cr-e-nlmail", "Enter an email address like name@example.com.");
                return response;
            }
            bool added = await Store.AddSubscriber(mail);
            response.ExecuteScript("CareersJs.subscribed('" + (added ? "Thanks. Product news lands about once a month." : "You are already on the list.") + "');");
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
            return "<article class=\"cr-pc\" data-pid=\"" + Ui.Attr(p.Id) + "\"><a class=\"cr-pc-img\" href=\"" + href + "\" tabindex=\"-1\" aria-hidden=\"true\">" + Ui.Pic(Ui.Cover(site, p), string.Empty, string.Empty, true) + (text != string.Empty ? "<span class=\"cr-badge cr-b-" + cls + "\">" + text + "</span>" : string.Empty) + "</a><button type=\"button\" class=\"cr-cmpb\" data-cmp=\"" + Ui.Attr(p.Id) + "\" aria-pressed=\"false\" onclick=\"CareersJs.cmp(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 4v16M15 4v16\"/><path d=\"M4 9h5M15 9h5M4 15h5M15 15h5\"/></svg><span>Compare</span></button><div class=\"cr-pc-b\"><span class=\"cr-pcat\"><span>" + Ui.Enc(Ui.CategoryName(p.Category)) + "</span><em>" + Ui.Enc(Ui.SeriesName(p.Series)) + "</em></span><span class=\"cr-pname\"><a href=\"" + href + "\">" + Ui.Enc(p.Name) + "</a></span><span class=\"cr-pfit\">" + FitSummary(site, p) + "</span><span class=\"cr-pc-f\"><span class=\"cr-price\">" + Ui.Money(p.Price) + "<small>MSRP</small></span>" + Ui.Pill("cr", Ui.Stocks, p.Stock) + "</span></div></article>";
        }

        private const long MaxResume = 5L * 1024 * 1024;

        private async Task<View> Render(SiteData site)
        {
            DateTime today = Store.Today(site);
            string slug = Q("job", 80);
            if (slug != string.Empty)
            {
                Job? j = site.Jobs.FirstOrDefault(x => x.Slug == slug);
                if (j == null)
                {
                    return new View { Title = "Position not found", Main = "<section class=\"cr-phead cr-wrap\"><h1>We could not find that position</h1><p class=\"cr-lead\">It may have been filled.</p><p style=\"margin-top:20px\"><a class=\"cr-btn cr-btn-accent\" href=\"Careers\">Open positions</a></p></section>", Nav = "careers" };
                }
                return new View { Title = j.Title, Desc = j.Summary, Main = JobView(site, j), Nav = "careers" };
            }
            return new View { Title = "Careers", Desc = "Open positions at " + site.Settings.Name + " in " + site.Settings.Town + ".", Main = Page(site, "careers", today), Nav = "careers" };
        }

        private static string JobView(SiteData site, Job j)
        {
            StringBuilder sb = new StringBuilder("<div class=\"cr-wrap cr-crumb\" style=\"padding-top:22px\"><a href=\"Careers\">Careers</a><span>/</span><span>" + Ui.Enc(Ui.NameOf(Ui.Depts, j.Dept)) + "</span></div><div class=\"cr-wrap cr-jtop\"><div><span class=\"cr-kick\">" + Ui.Enc(Ui.NameOf(Ui.Depts, j.Dept)) + " &middot; " + Ui.Enc(Ui.NameOf(Ui.JobTypes, j.Type)) + "</span><h1>" + Ui.Enc(j.Title) + "</h1><p class=\"cr-lead\">" + Ui.Enc(j.Summary) + "</p><div class=\"cr-jfacts\"><span><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M12 7.5V12l3 2\"/></svg> " + Ui.Enc(j.Shift) + "</span><span><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 21s-6.5-6-6.5-11a6.5 6.5 0 0 1 13 0c0 5-6.5 11-6.5 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.4\"/></svg> " + Ui.Enc(site.Settings.Town) + "</span>" + (j.Pay != string.Empty ? "<span><b>" + Ui.Enc(j.Pay) + "</b></span>" : string.Empty) + "</div></div>" + (j.Open ? "<a class=\"cr-btn cr-btn-accent\" href=\"#apply\">Apply now</a>" : "<span class=\"cr-st cr-st-closed\">This position is closed</span>") + "</div>");
            sb.Append("<div class=\"cr-wrap cr-jbody\"><div class=\"cr-rich cr-jtext\">" + Blocks.Sanitize(j.Body, 20000) + "<p class=\"cr-hint\" style=\"margin-top:18px\">Posted " + Ui.Day(j.Posted) + ". " + Ui.Enc(site.Settings.Name) + " is an equal opportunity employer.</p></div>");
            if (j.Open)
            {
                sb.Append("<aside class=\"cr-box cr-jform\" id=\"apply\"><h2>Apply for this position</h2><form class=\"cr-form\" id=\"cr-jf\" onsubmit=\"return CareersJs.apply()\" novalidate><label class=\"cr-field\"><span>Your name</span><input type=\"text\" id=\"cr-jname\" maxlength=\"80\" autocomplete=\"name\"></label><em class=\"cr-err\" id=\"cr-e-jname\" role=\"alert\"></em><label class=\"cr-field\"><span>Email</span><input type=\"email\" id=\"cr-jmail\" maxlength=\"120\" autocomplete=\"email\"></label><em class=\"cr-err\" id=\"cr-e-jmail\" role=\"alert\"></em><label class=\"cr-field\"><span>Phone</span><input type=\"tel\" id=\"cr-jphone\" maxlength=\"30\" autocomplete=\"tel\"></label><em class=\"cr-err\" id=\"cr-e-jphone\" role=\"alert\"></em><label class=\"cr-field\"><span>Résumé <small>(PDF or Word, up to 5 MB)</small></span><input type=\"file\" id=\"cr-jfile\" accept=\".pdf,.doc,.docx,application/pdf\" onchange=\"CareersJs.picked(this)\"></label><em class=\"cr-err\" id=\"cr-e-jfile\" role=\"alert\"></em><label class=\"cr-field\"><span>A few words about you <small>(optional)</small></span><textarea id=\"cr-jcover\" rows=\"5\" maxlength=\"2000\" placeholder=\"What you have done and why this role\"></textarea></label><label class=\"cr-hp\" aria-hidden=\"true\">Website<input type=\"text\" id=\"cr-jweb\" tabindex=\"-1\" autocomplete=\"off\"></label><input type=\"hidden\" id=\"cr-jid\" value=\"" + Ui.Attr(j.Id) + "\"><button type=\"submit\" class=\"cr-btn cr-btn-accent cr-btn-wide\" id=\"cr-jsend\">Send application</button><p class=\"cr-hint\">We answer every application within two weeks.</p></form><div id=\"cr-jdone\"></div></aside>");
            }
            sb.Append("</div>");
            List<Job> more = site.Jobs.Where(x => x.Open && x.Id != j.Id).OrderByDescending(x => x.Posted).Take(3).ToList();
            if (more.Count > 0)
            {
                sb.Append("<section class=\"cr-sec cr-wrap\"><div class=\"cr-shead\"><div><span class=\"cr-kick\">Also open</span><h2 class=\"cr-h2\">Other positions</h2></div><a class=\"cr-more\" href=\"Careers\">All positions &rarr;</a></div><div class=\"cr-jobs\">" + string.Concat(more.Select(JobRow)) + "</div></section>");
            }
            return sb.ToString();
        }

        private static string JobRow(Job j)
        {
            return "<a class=\"cr-job\" href=\"Careers?job=" + Uri.EscapeDataString(j.Slug) + "\"><span class=\"cr-jmain\"><b>" + Ui.Enc(j.Title) + "</b><span>" + Ui.Enc(j.Summary) + "</span></span><span class=\"cr-jmeta\"><span>" + Ui.Enc(Ui.NameOf(Ui.Depts, j.Dept)) + "</span><span>" + Ui.Enc(Ui.NameOf(Ui.JobTypes, j.Type)) + "</span>" + (j.Pay != string.Empty ? "<b>" + Ui.Enc(j.Pay) + "</b>" : string.Empty) + "</span><span class=\"cr-more\">View &rarr;</span></a>";
        }

        private string DataPart(SiteData site, Block b, DateTime today)
        {
            if (b.Source != "joblist")
            {
                return string.Empty;
            }
            string dept = Ui.Has(Ui.Depts, Q("dept", 20)) ? Q("dept", 20) : string.Empty;
            return "<section class=\"cr-sec cr-wrap\" id=\"jobs\"><div class=\"cr-bhead\">" + H2(site, b) + Rich(site, b.Text, "cr-lead") + "</div><div class=\"cr-chips\" id=\"cr-depts\">" + Chips(site, dept) + "</div><div id=\"cr-list\">" + List(site, dept) + "</div></section>";
        }

        private static string Chips(SiteData site, string cur)
        {
            StringBuilder sb = new StringBuilder("<button type=\"button\" class=\"cr-chip" + (cur == string.Empty ? " cr-act" : string.Empty) + "\" onclick=\"CareersJs.dept('')\">All departments</button>");
            foreach ((string k, string n) in Ui.Depts)
            {
                int cnt = site.Jobs.Count(j => j.Open && j.Dept == k);
                if (cnt > 0)
                {
                    sb.Append("<button type=\"button\" class=\"cr-chip" + (cur == k ? " cr-act" : string.Empty) + "\" onclick=\"CareersJs.dept('" + k + "')\">" + Ui.Enc(n) + " <em>" + cnt + "</em></button>");
                }
            }
            return sb.ToString();
        }

        private static string List(SiteData site, string dept)
        {
            List<Job> all = site.Jobs.Where(j => j.Open && (dept == string.Empty || j.Dept == dept)).OrderByDescending(j => j.Posted).ToList();
            if (all.Count == 0)
            {
                return "<p class=\"cr-none\">No open positions in this department right now. Send a general application through the <a href=\"Contact\">contact form</a>.</p>";
            }
            return "<div class=\"cr-jobs\" style=\"margin-top:18px\">" + string.Concat(all.Select(JobRow)) + "</div>";
        }

        public async Task<ApiResponse> Filter()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            string dept = Ui.Has(Ui.Depts, GetDataValue("dept")) ? GetDataValue("dept") : string.Empty;
            response.SetElementContents("cr-depts", Chips(site, dept));
            response.SetElementContents("cr-list", List(site, dept));
            return response;
        }

        private static string ResumeExt(byte[] b)
        {
            if (b.Length > 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46)
            {
                return "pdf";
            }
            if (b.Length > 4 && b[0] == 0x50 && b[1] == 0x4B && b[2] == 0x03 && b[3] == 0x04)
            {
                return "docx";
            }
            if (b.Length > 8 && b[0] == 0xD0 && b[1] == 0xCF && b[2] == 0x11 && b[3] == 0xE0)
            {
                return "doc";
            }
            return string.Empty;
        }

        public async Task<ApiResponse> Apply()
        {
            ApiResponse response = new ApiResponse();
            string name = Regex.Replace(Ui.Clip(GetDataValue("jname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("jmail"), 120).Trim();
            string phone = Ui.CleanPhone(GetDataValue("jphone"));
            string data = GetDataValue("rfile");
            if (name.Length < 2)
            {
                response.ExecuteScript("CareersJs.fieldErr('jname','Please enter your name.');CareersJs.applied(false);");
                return response;
            }
            if (!Ui.Email(mail))
            {
                response.ExecuteScript("CareersJs.fieldErr('jmail','Enter an email address like name@example.com.');CareersJs.applied(false);");
                return response;
            }
            if (Ui.Digits(phone).Length < 7)
            {
                response.ExecuteScript("CareersJs.fieldErr('jphone','Enter a phone number we can reach you on.');CareersJs.applied(false);");
                return response;
            }
            byte[]? bytes = null;
            int comma = data.IndexOf(',');
            if (comma > 0 && data.Length - comma < MaxResume * 1.4)
            {
                try
                {
                    bytes = Convert.FromBase64String(data.Substring(comma + 1));
                }
                catch (FormatException)
                {
                    bytes = null;
                }
            }
            string ext = bytes == null ? string.Empty : ResumeExt(bytes);
            if (bytes == null || ext == string.Empty || bytes.Length > MaxResume)
            {
                response.ExecuteScript("CareersJs.fieldErr('jfile','Attach your résumé as a PDF or Word file under 5 MB.');CareersJs.applied(false);");
                return response;
            }
            if (Ui.Clip(GetDataValue("jweb"), 50) != string.Empty)
            {
                response.ExecuteScript("CareersJs.applied(true,'Thanks. We answer every application within two weeks.');");
                return response;
            }
            string jobTitle = string.Empty;
            string appId = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Job? j = site.Jobs.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("jid"), 20) && x.Open);
                if (j == null)
                {
                    response.ExecuteScript("CareersJs.applied(false);CareersJs.toast('This position has just closed.');");
                    return response;
                }
                if (j.Apps.Any(a => a.Email.Equals(mail, StringComparison.OrdinalIgnoreCase)))
                {
                    response.ExecuteScript("CareersJs.applied(true,'" + Ui.Js("We already have your application for this position, " + name.Split(' ')[0] + ". We will be in touch.") + "');");
                    return response;
                }
                appId = Store.NextId(site.Jobs.SelectMany(x => x.Apps).Select(a => a.Id), "A-", 0);
                string file = appId.ToLowerInvariant() + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "." + ext;
                await File.WriteAllBytesAsync(Path.Combine(Store.FilesDir("resumes"), file), bytes);
                string orig = Regex.Replace(Ui.Clip(GetDataValue("rname"), 80), "[^A-Za-z0-9._ -]", string.Empty).Trim();
                j.Apps.Add(new JobApp { Id = appId, Name = name, Email = mail, Phone = phone, Cover = Ui.Clip(GetDataValue("jcover"), 2000), Resume = "data/files/resumes/" + file, ResumeName = orig != string.Empty ? orig : "resume." + ext, Created = DateTime.UtcNow });
                jobTitle = j.Title;
                await Store.SaveSite(site);
            }
            await Store.Log("web", "application", appId + " · " + name + " · " + jobTitle, true);
            response.ExecuteScript("CareersJs.applied(true,'" + Ui.Js("Thanks, " + name.Split(' ')[0] + ". Your application for " + jobTitle + " is in. We answer every application within two weeks.") + "');");
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
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"cr-btn " + (main ? "cr-btn-accent" : "cr-btn-ghost") + "\" href=\"" + Ui.Attr(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Enc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"cr-hero-b\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<span class=\"cr-kick\">" + Ui.Enc(Tok(site, b.Kicker)) + "</span>";
        }

        private static string H2(SiteData site, Block b)
        {
            return b.Title == string.Empty ? string.Empty : "<h2 class=\"cr-h2\">" + Ui.Enc(Tok(site, b.Title)) + "</h2>";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"cr-rich " + cls + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead)
        {
            string x = Kick(site, b) + H2(site, b) + (lead ? Rich(site, b.Text, "cr-lead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"cr-bhead\">" + x + "</div>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"cr-sec cr-wrap\" id=\"" + Ui.Attr(b.Id) + "\">" + inner + "</section>";
        }

        private static string Finder(SiteData site, string year, string make, string model)
        {
            List<(string, string)> years = new List<(string, string)>();
            for (int y = site.Settings.YearTo; y >= site.Settings.YearFrom; y--)
            {
                years.Add((y.ToString(Ui.Inv), y.ToString(Ui.Inv)));
            }
            string makes = JsonSerializer.Serialize(site.Makes.Select(m => new { key = m.Key, name = m.Name, models = m.Models.Select(x => new { key = x.Key, name = x.Name }) }));
            return "<form class=\"cr-find\" action=\"Products\" method=\"get\" data-makes=\"" + Ui.Attr(makes) + "\"><label class=\"cr-field\"><span>Year</span><select name=\"year\"><option value=\"\">Any year</option>" + Ui.Opts(years, year) + "</select></label><label class=\"cr-field\"><span>Make</span><select name=\"make\"><option value=\"\">Any make</option>" + Ui.Opts(site.Makes.Select(m => (m.Key, m.Name)), make) + "</select></label><label class=\"cr-field\"><span>Model</span><select name=\"model\" data-cur=\"" + Ui.Attr(model) + "\" disabled><option value=\"\">Any model</option></select></label><button type=\"submit\" class=\"cr-btn\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"6.5\"/><path d=\"M16 16l4.5 4.5\"/></svg> Find parts that fit</button></form>";
        }

        private static string Stats(SiteData site, DateTime today)
        {
            return "<div class=\"cr-hstats\"><a href=\"Products\"><b>" + site.Products.Count(Ui.Public) + "</b><span>products in the catalog</span></a><a href=\"Dealers\"><b>" + site.Dealers.Count(d => d.Active) + "</b><span>dealers and installers</span></a><a href=\"About\"><b>" + Math.Max(1, today.Year - site.Settings.Founded) + "</b><span>years in " + Ui.Enc(site.Settings.Town.Split(',')[0]) + "</span></a></div>";
        }

        private static string Hero(SiteData site, Block b, DateTime today, bool h1, string title, string text)
        {
            bool find = b.Extra == "finder";
            string tag = h1 ? "h1" : "h2";
            string media = Mv(site, b.Media, Tok(site, b.Title), Ui.IsVideo(b.Media), h1);
            string t = title != string.Empty ? Ui.Enc(title) : Ui.Enc(Tok(site, b.Title));
            string lead = text != string.Empty ? "<p class=\"cr-lead\">" + Ui.Enc(text) + "</p>" : Rich(site, b.Text, "cr-lead");
            return "<section class=\"cr-bhero" + (find ? " cr-bhome" : string.Empty) + (media == string.Empty ? " cr-bplain" : string.Empty) + "\" id=\"" + Ui.Attr(b.Id) + "\"><div class=\"cr-wrap cr-bhero-in\"><div class=\"cr-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"cr-btitle\">" + t + "</" + tag + ">" + lead + Btns(site, b) + (find ? Finder(site, string.Empty, string.Empty, string.Empty) : string.Empty) + "</div>" + (media != string.Empty ? "<div class=\"cr-bhero-p\">" + media + (find ? Stats(site, today) : string.Empty) + "</div>" : string.Empty) + "</div></section>";
        }

        private static string Split(SiteData site, Block b, DateTime today)
        {
            string counts = b.Extra == "counts" ? "<div class=\"cr-nums\"><span><b>" + site.Products.Count(Ui.Public) + "</b>products</span><span><b>" + site.Dealers.Count(d => d.Active) + "</b>dealers</span><span><b>" + Math.Max(1, today.Year - site.Settings.Founded) + "</b>years</span></div>" : string.Empty;
            string btn = Btn(site, b.Label, b.Link, false);
            return Sec(b, "<div class=\"cr-bsplit" + (b.Flip ? " cr-flip" : string.Empty) + (b.Media == string.Empty ? " cr-nomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"cr-bsm\">" + Mv(site, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "cr-lead") + counts + (btn != string.Empty ? "<div class=\"cr-hero-b\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            if (b.Items.Count == 6)
            {
                n = 3;
            }
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"cr-bcards cr-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"cr-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Enc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"cr-bcard\" href=\"" + Ui.Attr(it.Link) + "\">" + inner + "<span class=\"cr-more\">" + Ui.Enc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"cr-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"cr-bsteps cr-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
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
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"cr-bstats cr-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div><b>" + Ui.Enc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string People(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"cr-bpeople cr-n" + Math.Clamp(b.Items.Count, 1, 4) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"cr-person\">" + (it.Media != string.Empty && !Ui.IsVideo(it.Media) ? Mv(site, it.Media, it.Title, false, false) : "<span class=\"cr-noface\" style=\"--h:" + Ui.Hue(it.Title) + "\">" + Ui.Enc(Ui.Initials(it.Title)) + "</span>") + "<b>" + Ui.Enc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Boxes(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"cr-bboxes cr-n" + Math.Clamp(b.Items.Count, 1, 3) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"cr-box\">" + (it.Title != string.Empty ? "<h2>" + Ui.Enc(Tok(site, it.Title)) + "</h2>" : string.Empty) + "<ul class=\"cr-bticks\">");
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
            StringBuilder sb = new StringBuilder("<div class=\"cr-bfaq\">" + Head(site, b, false) + "<div class=\"cr-faq\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details><summary>" + Ui.Enc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Quote(SiteData site, Block b)
        {
            return Sec(b, "<figure class=\"cr-bquote\">" + (b.Media != string.Empty && !Ui.IsVideo(b.Media) ? Mv(site, b.Media, b.Title, false, false) : string.Empty) + "<blockquote>" + Lines(Tok(site, b.Text)) + "</blockquote>" + (b.Title != string.Empty ? "<figcaption>" + Ui.Enc(Tok(site, b.Title)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Wide(SiteData site, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"cr-bmedia\">" + Mv(site, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Enc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"cr-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "cr-lead") + "</div>" + Btns(site, b) + "</div>");
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
                    "text" => Sec(b, "<div class=\"cr-btext\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + "</div>"),
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
