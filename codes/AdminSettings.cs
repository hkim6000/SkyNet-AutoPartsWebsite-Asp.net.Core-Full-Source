using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminSettings : WebPage
    {
        private const string AreaKey = "settings";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Settings");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--st-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("st-menu", Auth.Menu("st", "AdminSettings", site));
            response.SetElementContents("st-who", Auth.Who("st", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminSettingsJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminSettingsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("st-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminSettingsJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"st-tr st-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static readonly string[] Zones = { "America/New_York", "America/Chicago", "America/Denver", "America/Phoenix", "America/Los_Angeles", "America/Anchorage", "Pacific/Honolulu", "America/Toronto", "America/Vancouver", "Europe/London", "Europe/Paris", "Europe/Berlin", "Asia/Tokyo", "Asia/Seoul", "Australia/Sydney", "Pacific/Auckland", "UTC" };

        private static string Field(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"st-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"st-s" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private static string TextArea(string id, string label, string value, int max, int rows)
        {
            return "<label class=\"st-field\"><span>" + label + "</span><textarea id=\"st-s" + id + "\" rows=\"" + rows + "\" maxlength=\"" + max + "\">" + Ui.Enc(value) + "</textarea></label>";
        }

        private static string MakeRow(Make m)
        {
            string models = string.Join("\n", m.Models.Select(x => x.Name + ", " + Ui.NameOf(Ui.Bodies, x.Body) + ", " + x.From.ToString(Ui.Inv) + "-" + x.To.ToString(Ui.Inv)));
            return "<div class=\"st-vmrow\"><input type=\"text\" maxlength=\"30\" value=\"" + Ui.Attr(m.Name) + "\" placeholder=\"Make\" aria-label=\"Make\"><textarea rows=\"" + Math.Max(2, m.Models.Count) + "\" maxlength=\"2000\" placeholder=\"One model per line: Name, body, from-to\" aria-label=\"Models\">" + Ui.Enc(models) + "</textarea><button type=\"button\" class=\"st-ibtn\" aria-label=\"Remove make\" onclick=\"AdminSettingsJs.dropMake(this)\">&times;</button></div>";
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            Settings s = site.Settings;
            StringBuilder sb = new StringBuilder("<div class=\"st-ptop\"><div><h1>Settings</h1><p>Company details, units, warranty, vehicles for the fitment finder and office hours. Page text and pictures are in Pages.</p></div></div><nav class=\"st-setnav\"><a href=\"#s-company\">Company</a><a href=\"#s-catalog\">Catalog</a><a href=\"#s-vehicles\">Vehicles</a><a href=\"#s-hours\">Office hours</a></nav><form onsubmit=\"return AdminSettingsJs.save()\" novalidate>");
            sb.Append("<section class=\"st-panel\" id=\"s-company\"><h2>Company</h2><div class=\"st-form\"><div class=\"st-f3\">" + Field("name", "Name", s.Name, 40) + Field("town", "Town", s.Town, 40) + Field("tag", "Tagline", s.Tagline, 140) + "</div><div class=\"st-f3\">" + Field("addr", "Address", s.Address, 100) + Field("phone", "Phone", s.Phone, 30, "tel") + Field("email", "Email", s.Email, 80, "email") + "</div>" + Field("legal", "Legal line (footer)", s.Legal, 200) + "<div class=\"st-f4\"><label class=\"st-field\"><span>Accent color</span><input type=\"color\" id=\"st-saccent\" value=\"" + Ui.Attr(Ui.SafeColor(s.Accent) ? s.Accent : "#D9531E") + "\"></label><label class=\"st-field\"><span>Website theme</span><select id=\"st-stheme\">" + Ui.Opts(new[] { ("light", "Light"), ("dark", "Dark") }, s.Theme) + "</select></label>" + Field("founded", "Founded (year)", s.Founded.ToString(Ui.Inv), 4, "text", " inputmode=\"numeric\"") + "</div></div></section>");
            sb.Append("<section class=\"st-panel\" id=\"s-catalog\"><h2>Catalog</h2><div class=\"st-form\"><div class=\"st-f3\"><label class=\"st-field\"><span>Units on the website</span><select id=\"st-sunits\">" + Ui.Opts(Ui.UnitModes.Select(u => (u.Key, u.Name)), s.Units) + "</select></label>" + Field("wmonths", "Standard warranty (months)", s.WarrantyMonths.ToString(Ui.Inv), 3, "text", " inputmode=\"numeric\"") + Field("ship", "Shipping line (product pages)", s.ShipFrom, 160) + "</div><div class=\"st-f2\">" + TextArea("wtext", "Warranty text (Contact page, warranty tab)", s.WarrantyText, 600, 4) + TextArea("qtext", "Quote promise (quote forms)", s.QuoteText, 300, 4) + "</div>" + TextArea("certs", "Certifications and quality lines (one per line, Capabilities page)", s.Certifications, 800, 5) + "</div></section>");
            sb.Append("<section class=\"st-panel\" id=\"s-vehicles\"><h2>Vehicles</h2><p class=\"st-hint\" style=\"margin-bottom:12px\">The makes and models in the fitment finder and in product fitment lines. One model per line as <b>Name, body, from-to</b>, for example <b>Meridian, Sedan, 2014-2027</b>. Bodies: " + string.Join(", ", Ui.Bodies.Select(b => b.Name)) + ".</p><div class=\"st-f4\" style=\"margin-bottom:12px\">" + Field("yfrom", "Finder years from", s.YearFrom.ToString(Ui.Inv), 4, "text", " inputmode=\"numeric\"") + Field("yto", "Finder years to", s.YearTo.ToString(Ui.Inv), 4, "text", " inputmode=\"numeric\"") + "</div><div class=\"st-vm\" id=\"st-makes\">" + string.Concat(site.Makes.Select(MakeRow)) + "</div><template id=\"st-mtpl\">" + MakeRow(new Make()) + "</template><div style=\"margin-top:10px\"><button type=\"button\" class=\"st-btn st-btn-sm st-btn-ghost\" onclick=\"AdminSettingsJs.addMake()\">Add a make</button></div></section>");
            sb.Append("<section class=\"st-panel\" id=\"s-hours\"><h2>Office hours and time zone</h2><div id=\"st-hours\">");
            foreach (int d in new[] { 1, 2, 3, 4, 5, 6, 0 })
            {
                OpenDay o = s.Hours.FirstOrDefault(h => h.Day == d) ?? new OpenDay { Day = d };
                sb.Append("<div class=\"st-orow" + (o.Open ? string.Empty : " st-shut") + "\" data-day=\"" + d + "\"><b>" + Ui.DayNames[d] + "</b><label><input type=\"checkbox\"" + (o.Open ? " checked" : string.Empty) + " onchange=\"AdminSettingsJs.dayOn(this)\" aria-label=\"Open on " + Ui.DayNames[d] + "\"></label><input type=\"time\" step=\"900\" value=\"" + Ui.Attr(o.Start) + "\" aria-label=\"Opens\"><input type=\"time\" step=\"900\" value=\"" + Ui.Attr(o.End) + "\" aria-label=\"Closes\"></div>");
            }
            List<(string, string)> zones = Zones.Select(z => (z, z.Replace('_', ' '))).ToList();
            if (!Zones.Contains(s.TimeZone))
            {
                zones.Insert(0, (s.TimeZone, s.TimeZone));
            }
            sb.Append("</div><p class=\"st-hint\" style=\"margin:8px 0 12px\">Office hours drive the open or closed line on every page and the Contact page.</p><div class=\"st-f4\"><label class=\"st-field\"><span>Time zone (now " + Ui.Hm(site, Store.Now(site)) + ")</span><select id=\"st-stz\">" + Ui.Opts(zones, s.TimeZone) + "</select></label><label class=\"st-chk\" style=\"align-self:end;min-height:44px\"><input type=\"checkbox\" id=\"st-sh24\"" + (s.Clock24 ? " checked" : string.Empty) + "> 24-hour clock</label></div></section>");
            sb.Append("<div class=\"st-savebar\"><em class=\"st-err\" id=\"st-e-set\" role=\"alert\"></em><button type=\"submit\" class=\"st-btn st-btn-accent\">Save settings</button></div></form>");
            return sb.ToString();
        }

        private static int Int(string v, int min, int max, int cur)
        {
            return int.TryParse((v ?? string.Empty).Trim(), NumberStyles.Integer, Ui.Inv, out int n) ? Math.Clamp(n, min, max) : cur;
        }

        private static (List<Make>? Makes, string Error) ParseMakes(string raw, List<Make> old)
        {
            List<Make> list = new List<Make>();
            foreach (string block in raw.Split("~~", StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = block.Split("::");
                string name = Regex.Replace(Ui.Clip(parts[0], 30), "\\s+", " ").Trim();
                if (name.Length < 2)
                {
                    continue;
                }
                string key = Ui.Slug(name);
                Make? prev = old.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                Make m = new Make { Key = prev?.Key ?? key, Name = name };
                if (list.Any(x => x.Key == m.Key))
                {
                    return (null, "The make " + name + " is listed twice.");
                }
                foreach (string line in (parts.Length > 1 ? parts[1] : string.Empty).Split('|'))
                {
                    string[] f = line.Split(',').Select(x => x.Trim()).ToArray();
                    if (f.Length == 0 || f[0] == string.Empty)
                    {
                        continue;
                    }
                    string body = f.Length > 1 ? Ui.Bodies.FirstOrDefault(b => b.Name.Equals(f[1], StringComparison.OrdinalIgnoreCase) || b.Key.Equals(f[1], StringComparison.OrdinalIgnoreCase)).Key ?? string.Empty : string.Empty;
                    if (body == string.Empty)
                    {
                        return (null, name + " " + f[0] + ": use one of the body names, for example Sedan or Pickup.");
                    }
                    Match yr = Regex.Match(f.Length > 2 ? f[2] : string.Empty, "^(\\d{4})\\s*-\\s*(\\d{4})$");
                    if (!yr.Success)
                    {
                        return (null, name + " " + f[0] + ": give the years as from-to, for example 2014-2027.");
                    }
                    string mname = Ui.Clip(f[0], 30);
                    VehicleModel? pm = prev?.Models.FirstOrDefault(x => x.Name.Equals(mname, StringComparison.OrdinalIgnoreCase));
                    m.Models.Add(new VehicleModel { Key = pm?.Key ?? Ui.Slug(mname), Name = mname, Body = body, From = int.Parse(yr.Groups[1].Value, Ui.Inv), To = int.Parse(yr.Groups[2].Value, Ui.Inv) });
                }
                if (m.Models.Select(x => x.Key).Distinct().Count() != m.Models.Count)
                {
                    return (null, name + " has two models with the same name.");
                }
                list.Add(m);
            }
            return (list, string.Empty);
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminSettingsJs.expired();");
                return response;
            }
            string name = Regex.Replace(Ui.Clip(GetDataValue("sname"), 40), "\\s+", " ").Trim();
            string email = Ui.Clip(GetDataValue("semail"), 80).Trim();
            string tz = Ui.Clip(GetDataValue("stz"), 60);
            string err = name.Length < 2 ? "Enter the company name." : email != string.Empty && !Ui.Email(email) ? "Check the email address." : !Store.KnownZone(tz) ? "Choose a time zone." : string.Empty;
            List<OpenDay> hours = new List<OpenDay>();
            foreach (string row in GetDataValue("shours").Split('~'))
            {
                string[] x = row.Split('|');
                if (x.Length != 4 || !int.TryParse(x[0], out int d) || d < 0 || d > 6 || Store.Hm(x[2]) == null || Store.Hm(x[3]) == null)
                {
                    continue;
                }
                OpenDay o = new OpenDay { Day = d, Open = x[1] == "1", Start = x[2], End = x[3] };
                if (o.Open && string.CompareOrdinal(o.Start, o.End) >= 0 && err == string.Empty)
                {
                    err = Ui.DayNames[d] + ": closing time must be after opening time.";
                }
                hours.Add(o);
            }
            int yFrom = Int(GetDataValue("syfrom"), 1950, 2100, 2012);
            int yTo = Int(GetDataValue("syto"), 1950, 2100, 2027);
            if (err == string.Empty && yTo < yFrom)
            {
                err = "Finder years: the last year must not be before the first.";
            }
            SiteData site0 = await Store.LoadSite();
            (List<Make>? makes, string merr) = ParseMakes(GetDataValue("smakes"), site0.Makes);
            if (err == string.Empty && makes == null)
            {
                err = merr;
            }
            if (err != string.Empty)
            {
                response.SetElementContents("st-e-set", err);
                return response;
            }
            string accent = Ui.SafeColor(GetDataValue("saccent")) ? GetDataValue("saccent") : "#D9531E";
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Settings s = site.Settings;
                s.Name = name;
                s.Town = Ui.Clip(GetDataValue("stown"), 40).Trim();
                s.Tagline = Ui.Clip(GetDataValue("stag"), 140).Trim();
                s.Address = Ui.Clip(GetDataValue("saddr"), 100).Trim();
                s.Phone = Ui.CleanPhone(GetDataValue("sphone"));
                s.Email = email;
                s.Legal = Ui.Clip(GetDataValue("slegal"), 200).Trim();
                s.Accent = accent;
                s.Theme = GetDataValue("stheme") == "dark" ? "dark" : "light";
                s.Units = Ui.Has(Ui.UnitModes, GetDataValue("sunits")) ? GetDataValue("sunits") : "both";
                s.YearFrom = yFrom;
                s.YearTo = yTo;
                s.Founded = Int(GetDataValue("sfounded"), 1800, 2100, s.Founded);
                s.WarrantyMonths = Int(GetDataValue("swmonths"), 0, 240, s.WarrantyMonths);
                s.WarrantyText = Ui.Clip(GetDataValue("swtext"), 600).Trim();
                s.QuoteText = Ui.Clip(GetDataValue("sqtext"), 300).Trim();
                s.Certifications = string.Join("\n", Ui.SplitLines(GetDataValue("scerts"), 12, 120));
                s.ShipFrom = Ui.Clip(GetDataValue("sship"), 160).Trim();
                s.TimeZone = tz;
                s.Clock24 = GetDataValue("sh24") == "1";
                if (hours.Count == 7)
                {
                    s.Hours = hours.OrderBy(h => h.Day).ToList();
                }
                site.Makes = makes!;
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "settings", name, true);
            response.ExecuteScript("AdminSettingsJs.toast('Settings saved');document.getElementById('st-site').style.setProperty('--st-accent','" + accent + "');");
            return response;
        }
    }
}
