using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutoParts.Models;

namespace AutoParts.Core
{
    public static class Ui
    {
        public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        public static readonly (string Key, string Name)[] Categories = { ("coilovers", "Coilovers"), ("struts", "Struts"), ("shocks", "Shocks"), ("arms", "Control arms"), ("swaybars", "Sway bars"), ("bushings", "Bushings"), ("leafsprings", "Leaf springs"), ("liftkits", "Lift & leveling kits"), ("air", "Air suspension") };

        public static readonly (string Key, string Name)[] CategoryBlurbs = { ("coilovers", "Height and damping adjustable, corner by corner."), ("struts", "Complete strut assemblies, ready to bolt in."), ("shocks", "Monotube and twin-tube dampers for every road."), ("arms", "Forged arms with sealed ball joints."), ("swaybars", "Hollow and solid bars with adjustable end links."), ("bushings", "Polyurethane and rubber, pressed or bolt-in."), ("leafsprings", "Progressive packs for work and towing."), ("liftkits", "Level the stance or clear bigger tires."), ("air", "Bellows, compressors and ride control.") };

        public static readonly (string Key, string Name)[] Series = { ("street", "Street"), ("track", "Track"), ("offroad", "Off-road"), ("hd", "Heavy duty") };

        public static readonly (string Key, string Name)[] Stocks = { ("in", "In stock"), ("low", "Low stock"), ("out", "Out of stock"), ("order", "Built to order") };

        public static readonly (string Key, string Name)[] ProductStatuses = { ("draft", "Draft"), ("live", "Live"), ("retired", "Retired") };

        public static readonly (string Key, string Name)[] Positions = { ("set", "Full set"), ("front", "Front"), ("rear", "Rear"), ("pair", "Pair") };

        public static readonly (string Key, string Name)[] Bodies = { ("sedan", "Sedan"), ("coupe", "Coupe"), ("hatch", "Hatchback"), ("wagon", "Wagon"), ("crossover", "Crossover"), ("suv", "SUV"), ("pickup", "Pickup"), ("van", "Van") };

        public static readonly (string Key, string Name)[] SpecUnits = { ("", "Text"), ("in", "in / mm"), ("lb", "lb / kg"), ("psi", "psi / bar"), ("lbin", "lb/in / N/mm"), ("ftlb", "ft·lb / N·m"), ("deg", "degrees") };

        public static readonly (string Key, string Name)[] DealerKinds = { ("retail", "Retail store"), ("installer", "Installer"), ("distributor", "Distributor"), ("online", "Online store") };

        public static readonly (string Key, string Name)[] Tiers = { ("authorized", "Authorized"), ("premier", "Premier") };

        public static readonly (string Key, string Name)[] DealerServices = { ("install", "Installation"), ("align", "Alignment"), ("corner", "Corner balancing"), ("fab", "Fabrication"), ("fleet", "Fleet service") };

        public static readonly (string Key, string Name)[] InquiryKinds = { ("quote", "Quote request"), ("question", "Product question"), ("dealer", "Dealer inquiry"), ("warranty", "Warranty claim"), ("contact", "Message") };

        public static readonly (string Key, string Name)[] QuoteStatuses = { ("new", "New"), ("quoted", "Quoted"), ("won", "Won"), ("lost", "Lost"), ("closed", "Closed") };

        public static readonly (string Key, string Name)[] MsgStatuses = { ("new", "New"), ("replied", "Replied"), ("closed", "Closed") };

        public static readonly (string Key, string Name)[] ContactTopics = { ("general", "General question"), ("order", "An order or delivery"), ("fitment", "Fitment help"), ("press", "Press and media"), ("other", "Something else") };

        public static readonly (string Key, string Name)[] PostTags = { ("launch", "Product launch"), ("show", "Trade show"), ("plant", "Plant news"), ("tech", "Tech note"), ("community", "Community") };

        public static readonly (string Key, string Name)[] Depts = { ("production", "Production"), ("engineering", "Engineering"), ("quality", "Quality"), ("sales", "Sales"), ("warehouse", "Warehouse"), ("office", "Office") };

        public static readonly (string Key, string Name)[] JobTypes = { ("full", "Full-time"), ("part", "Part-time"), ("contract", "Contract") };

        public static readonly (string Key, string Name)[] AppStatuses = { ("new", "New"), ("reviewing", "Reviewing"), ("interview", "Interview"), ("offer", "Offer"), ("hired", "Hired"), ("declined", "Declined") };

        public static readonly (string Key, string Name)[] UnitModes = { ("us", "US only (lb, in, psi)"), ("both", "US with metric beside"), ("metric", "Metric only (kg, mm, bar)") };

        public static readonly (string Key, string Name)[] States = { ("AL", "Alabama"), ("AK", "Alaska"), ("AZ", "Arizona"), ("AR", "Arkansas"), ("CA", "California"), ("CO", "Colorado"), ("CT", "Connecticut"), ("DE", "Delaware"), ("DC", "District of Columbia"), ("FL", "Florida"), ("GA", "Georgia"), ("HI", "Hawaii"), ("ID", "Idaho"), ("IL", "Illinois"), ("IN", "Indiana"), ("IA", "Iowa"), ("KS", "Kansas"), ("KY", "Kentucky"), ("LA", "Louisiana"), ("ME", "Maine"), ("MD", "Maryland"), ("MA", "Massachusetts"), ("MI", "Michigan"), ("MN", "Minnesota"), ("MS", "Mississippi"), ("MO", "Missouri"), ("MT", "Montana"), ("NE", "Nebraska"), ("NV", "Nevada"), ("NH", "New Hampshire"), ("NJ", "New Jersey"), ("NM", "New Mexico"), ("NY", "New York"), ("NC", "North Carolina"), ("ND", "North Dakota"), ("OH", "Ohio"), ("OK", "Oklahoma"), ("OR", "Oregon"), ("PA", "Pennsylvania"), ("RI", "Rhode Island"), ("SC", "South Carolina"), ("SD", "South Dakota"), ("TN", "Tennessee"), ("TX", "Texas"), ("UT", "Utah"), ("VT", "Vermont"), ("VA", "Virginia"), ("WA", "Washington"), ("WV", "West Virginia"), ("WI", "Wisconsin"), ("WY", "Wyoming") };

        public static string Enc(string? v)
        {
            return System.Net.WebUtility.HtmlEncode(v ?? string.Empty);
        }

        public static string Attr(string? v)
        {
            return System.Net.WebUtility.HtmlEncode(v ?? string.Empty);
        }

        public static string Js(string? v)
        {
            return (v ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "&quot;").Replace("<", "\\x3c").Replace("\n", " ").Replace("\r", string.Empty);
        }

        public static string Clip(string? value, int max)
        {
            string v = (value ?? string.Empty).Trim();
            return v.Length > max ? v.Substring(0, max) : v;
        }

        public static string Plural(int n, string one, string many)
        {
            return n.ToString("#,##0", Inv) + " " + (n == 1 ? one : many);
        }

        public static string Iso(DateTime d)
        {
            return d.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", Inv);
        }

        public static string Ago(DateTime d)
        {
            TimeSpan t = DateTime.UtcNow - d.ToUniversalTime();
            if (t.TotalMinutes < 1)
            {
                return "Just now";
            }
            if (t.TotalMinutes < 60)
            {
                int m = (int)t.TotalMinutes;
                return m + (m == 1 ? " min ago" : " mins ago");
            }
            if (t.TotalHours < 24)
            {
                int h = (int)t.TotalHours;
                return h + (h == 1 ? " hour ago" : " hours ago");
            }
            return d.ToUniversalTime().ToString("d MMM yyyy", Inv);
        }

        public static string Time(DateTime d, string mode = "ago")
        {
            DateTime u = DateTime.SpecifyKind(d, DateTimeKind.Utc);
            return "<time datetime=\"" + Iso(u) + "\" data-t=\"" + mode + "\">" + (mode == "ago" ? Ago(u) : u.ToString("d MMMM yyyy, HH:mm 'UTC'", Inv)) + "</time>";
        }

        public static string Initials(string name)
        {
            string[] parts = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "?";
            }
            string a = parts[0].Substring(0, 1);
            string b = parts.Length > 1 ? parts[^1].Substring(0, 1) : string.Empty;
            return (a + b).ToUpperInvariant();
        }

        public static string Hue(string id)
        {
            int h = 0;
            foreach (char c in id ?? string.Empty)
            {
                h = (h * 31 + c) % 360;
            }
            return h.ToString(Inv);
        }

        public static string Avatar(string p, string name)
        {
            string n = name == string.Empty ? "Admin" : name;
            return "<span class=\"" + p + "-av\" style=\"--h:" + Hue(n) + "\" aria-hidden=\"true\">" + Enc(Initials(n)) + "</span>";
        }

        public static string Who(string id)
        {
            return id == "web" ? "Website" : id == string.Empty ? "Nobody" : "Admin";
        }

        public static string Who(AdminUser u, string id)
        {
            return id == "web" ? "Website" : id == string.Empty ? "Nobody" : u.Name;
        }

        public static string Pager(string p, int page, int size, int total, string one, string many, string js)
        {
            if (total == 0)
            {
                return string.Empty;
            }
            int pages = Math.Max(1, (total + size - 1) / size);
            return "<div class=\"" + p + "-pager\"><button type=\"button\" class=\"" + p + "-btn " + p + "-btn-sm " + p + "-btn-ghost\" onclick=\"" + js + "(" + (page - 1) + ")\"" + (page <= 1 ? " disabled" : string.Empty) + ">&larr; Previous</button><span>Page " + page + " of " + pages + " &middot; " + Plural(total, one, many) + "</span><button type=\"button\" class=\"" + p + "-btn " + p + "-btn-sm " + p + "-btn-ghost\" onclick=\"" + js + "(" + (page + 1) + ")\"" + (page >= pages ? " disabled" : string.Empty) + ">Next &rarr;</button></div>";
        }

        public static int PageOf(string value, int size, int total)
        {
            int pages = Math.Max(1, (total + size - 1) / size);
            return Math.Clamp(int.TryParse(value, out int n) ? n : 1, 1, pages);
        }

        public static string Money(decimal v)
        {
            return (v < 0 ? "&minus;$" : "$") + Math.Abs(v).ToString(v == Math.Floor(v) ? "#,##0" : "#,##0.00", Inv);
        }

        public static string Short(decimal v)
        {
            if (v >= 1000000m)
            {
                return "$" + (v / 1000000m).ToString(v % 1000000m == 0 ? "0" : "0.0#", Inv) + "M";
            }
            if (v >= 10000m)
            {
                return "$" + Math.Round(v / 1000m).ToString("0", Inv) + "K";
            }
            return "$" + v.ToString("#,##0", Inv);
        }

        public static string Num(int v)
        {
            return v.ToString("#,##0", Inv);
        }

        public static string Day(DateTime d, bool year = true)
        {
            return d.ToString(year ? "MMM d, yyyy" : "MMM d", Inv);
        }

        public static string LongDay(DateTime d)
        {
            return d.ToString("dddd, MMMM d, yyyy", Inv);
        }

        public static string Ymd(DateTime d)
        {
            return d.ToString("yyyy-MM-dd", Inv);
        }

        public static DateTime? ParseDay(string? v)
        {
            return DateTime.TryParseExact((v ?? string.Empty).Trim(), "yyyy-MM-dd", Inv, DateTimeStyles.None, out DateTime d) ? DateTime.SpecifyKind(d.Date, DateTimeKind.Unspecified) : null;
        }

        public static string Hm(SiteData s, DateTime d)
        {
            return d.ToString(s.Settings.Clock24 ? "HH:mm" : "h:mm tt", Inv);
        }

        public static string HmText(SiteData s, string hm)
        {
            TimeSpan? t = Store.Hm(hm);
            return t == null ? string.Empty : Hm(s, DateTime.Today.Add(t.Value));
        }

        public static string Thumb(string full)
        {
            return full.Contains("/full/", StringComparison.Ordinal) ? full.Replace("/full/", "/thumbs/") : full;
        }

        public static string Pic(string src, string alt, string cls, bool thumb = false, bool eager = false)
        {
            if (src == string.Empty)
            {
                return string.Empty;
            }
            return "<img class=\"" + cls + "\" src=\"" + Attr(thumb ? Thumb(src) : src) + "\" alt=\"" + Attr(alt) + "\"" + (eager ? string.Empty : " loading=\"lazy\"") + " width=\"" + (thumb ? "640" : "1600") + "\" height=\"" + (thumb ? "480" : "1200") + "\">";
        }

        public static string CleanPhone(string? v)
        {
            return Regex.Replace(Clip(v, 30), "[^0-9+()\\-. ]", string.Empty).Trim();
        }

        public static string Digits(string? v)
        {
            return Regex.Replace(v ?? string.Empty, "[^0-9]", string.Empty);
        }

        public static bool Email(string v)
        {
            return Regex.IsMatch(v ?? string.Empty, "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$");
        }

        public static bool SafeColor(string v)
        {
            return Regex.IsMatch(v ?? string.Empty, "^#[0-9a-fA-F]{6}$");
        }

        public static string Slug(string v)
        {
            string s = Regex.Replace((v ?? string.Empty).ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            return s.Length > 60 ? s.Substring(0, 60).Trim('-') : s;
        }

        public static string Opts(IEnumerable<(string Value, string Text)> items, string cur)
        {
            StringBuilder sb = new StringBuilder();
            foreach ((string v, string t) in items)
            {
                sb.Append("<option value=\"" + Attr(v) + "\"" + (v == cur ? " selected" : string.Empty) + ">" + Enc(t) + "</option>");
            }
            return sb.ToString();
        }

        public static string DataList(string id, IEnumerable<string> values)
        {
            StringBuilder sb = new StringBuilder("<datalist id=\"" + id + "\">");
            foreach (string v in values.Where(x => x != string.Empty).Distinct().OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append("<option value=\"" + Attr(v) + "\"></option>");
            }
            sb.Append("</datalist>");
            return sb.ToString();
        }

        public static string NameOf((string Key, string Name)[] list, string key)
        {
            foreach ((string k, string n) in list)
            {
                if (k == key)
                {
                    return n;
                }
            }
            return key;
        }

        public static bool Has((string Key, string Name)[] list, string key)
        {
            return list.Any(x => x.Key == key);
        }

        public static string Pill(string p, (string Key, string Name)[] list, string key)
        {
            return "<span class=\"" + p + "-st " + p + "-st-" + key + "\">" + Enc(NameOf(list, key)) + "</span>";
        }

        public static string StateName(string code)
        {
            return NameOf(States, code);
        }

        public static bool Public(Part x)
        {
            return x.Status == "live";
        }

        public static Part? ProductOf(SiteData s, string id)
        {
            return s.Products.FirstOrDefault(x => x.Id == id);
        }

        public static string ProductName(SiteData s, string id)
        {
            return ProductOf(s, id)?.Name ?? string.Empty;
        }

        public static string CategoryName(string k)
        {
            return NameOf(Categories, k);
        }

        public static string SeriesName(string k)
        {
            return NameOf(Series, k);
        }

        public static Make? MakeOf(SiteData s, string key)
        {
            return s.Makes.FirstOrDefault(m => m.Key == key);
        }

        public static string MakeName(SiteData s, string key)
        {
            return MakeOf(s, key)?.Name ?? key;
        }

        public static string ModelName(SiteData s, string make, string model)
        {
            return MakeOf(s, make)?.Models.FirstOrDefault(m => m.Key == model)?.Name ?? model;
        }

        public static bool Fits(Part p, string make, string model, int year)
        {
            return p.Fitments.Any(f => (make == string.Empty || f.Make == make) && (model == string.Empty || f.Model == model) && (year == 0 || (year >= f.From && year <= f.To)));
        }

        public static string FitLine(SiteData s, Fitment f)
        {
            return MakeName(s, f.Make) + " " + ModelName(s, f.Make, f.Model) + " " + f.From.ToString(Inv) + "&ndash;" + f.To.ToString(Inv);
        }

        private static string Fmt(double v)
        {
            double a = Math.Abs(v);
            return v.ToString(a >= 100 ? "#,##0" : a >= 10 ? "0.#" : "0.##", Inv);
        }

        private static (double Factor, string Us, string Metric) UnitInfo(string unit)
        {
            return unit switch
            {
                "in" => (25.4, "in", "mm"),
                "lb" => (0.45359237, "lb", "kg"),
                "psi" => (0.0689476, "psi", "bar"),
                "lbin" => (0.175127, "lb/in", "N/mm"),
                "ftlb" => (1.35582, "ft·lb", "N·m"),
                "deg" => (1, "°", "°"),
                _ => (0, string.Empty, string.Empty)
            };
        }

        public static string Measure(SiteData s, string value, string unit)
        {
            (double factor, string us, string metric) = UnitInfo(unit);
            string v = (value ?? string.Empty).Trim();
            if (factor == 0 || v == string.Empty)
            {
                return Enc(v);
            }
            if (unit == "deg")
            {
                return Enc(v) + "°";
            }
            string mode = s.Settings.Units;
            string conv = Regex.Replace(v, "-?[0-9]+(\\.[0-9]+)?", m => Fmt(double.Parse(m.Value, Inv) * factor));
            if (mode == "metric")
            {
                return Enc(conv) + " " + metric;
            }
            if (mode == "us")
            {
                return Enc(v) + " " + us;
            }
            return Enc(v) + " " + us + " <small>(" + Enc(conv) + " " + metric + ")</small>";
        }

        public static string MeasurePlain(SiteData s, string value, string unit)
        {
            return Regex.Replace(Measure(s, value, unit), "<[^>]+>", string.Empty).Replace("&nbsp;", " ");
        }

        public static string Weight(SiteData s, Part p)
        {
            SpecRow? w = p.Specs.FirstOrDefault(x => x.Unit == "lb");
            return w == null ? string.Empty : Measure(s, w.Value, "lb");
        }

        public static List<string> Images(string folder)
        {
            string dir = Path.Combine(Store.Root, "images", folder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(dir))
            {
                return new List<string>();
            }
            return Directory.GetFiles(dir).Where(f => Regex.IsMatch(f, "\\.(webp|jpg|jpeg|png)$", RegexOptions.IgnoreCase)).Select(f => "images/" + folder + "/" + Path.GetFileName(f)).OrderBy(f => f, StringComparer.Ordinal).ToList();
        }

        public static bool IsVideo(string? src)
        {
            return Regex.IsMatch(src ?? string.Empty, "\\.(mp4|webm)$", RegexOptions.IgnoreCase);
        }

        public static bool IsDoc(string? src)
        {
            return Regex.IsMatch(src ?? string.Empty, "\\.pdf$", RegexOptions.IgnoreCase);
        }

        public static bool SafeVideo(string src)
        {
            return Regex.IsMatch(src ?? string.Empty, "^images/[A-Za-z0-9/_.-]+\\.(mp4|webm)$") && !src!.Contains("..") && File.Exists(Path.Combine(Store.Root, src.Replace('/', Path.DirectorySeparatorChar)));
        }

        public static bool SafeDoc(string src)
        {
            return Regex.IsMatch(src ?? string.Empty, "^images/[A-Za-z0-9/_.-]+\\.pdf$") && !src!.Contains("..") && File.Exists(Path.Combine(Store.Root, src.Replace('/', Path.DirectorySeparatorChar)));
        }

        public static bool SafeImage(string src)
        {
            return Regex.IsMatch(src ?? string.Empty, "^images/[A-Za-z0-9/_.-]+\\.(webp|jpg|jpeg|png)$") && !src!.Contains("..") && File.Exists(Path.Combine(Store.Root, src.Replace('/', Path.DirectorySeparatorChar)));
        }

        public static bool SafeMedia(string src)
        {
            return SafeImage(src) || SafeVideo(src) || SafeDoc(src);
        }

        public static string Poster(SiteData site, string src)
        {
            if (!IsVideo(src) && !IsDoc(src))
            {
                return Thumb(src);
            }
            return site.Media.FirstOrDefault(m => m.Full == src)?.Thumb ?? string.Empty;
        }

        public static List<string> Stills(Part p)
        {
            return p.Photos.Where(x => !IsVideo(x)).ToList();
        }

        public static string Cover(SiteData site, Part p)
        {
            string? still = p.Photos.FirstOrDefault(x => !IsVideo(x));
            return still ?? (p.Photos.Count > 0 ? Poster(site, p.Photos[0]) : string.Empty);
        }

        public static List<string> SplitLines(string? v, int max, int each)
        {
            return (v ?? string.Empty).Split('\n').Select(x => Clip(x, each)).Where(x => x != string.Empty).Take(max).ToList();
        }

        public static string Csv(string v)
        {
            string t = (v ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            if (Regex.IsMatch(t, "^[=+\\-@]"))
            {
                t = "'" + t;
            }
            return t.Contains(',') || t.Contains('"') || t.Contains('\'') ? "\"" + t.Replace("\"", "\"\"") + "\"" : t;
        }

        public static (DateTime? From, DateTime? To) Period(DateTime today, string period, string from, string to, bool future = false)
        {
            return period switch
            {
                "today" => (today, today),
                "7" => future ? (today, today.AddDays(6)) : (today.AddDays(-6), today),
                "30" => future ? (today, today.AddDays(29)) : (today.AddDays(-29), today),
                "range" => (ParseDay(from), ParseDay(to)),
                _ => (null, null)
            };
        }

        public static bool InPeriod(DateTime d, DateTime? from, DateTime? to)
        {
            return (from == null || d.Date >= from.Value) && (to == null || d.Date <= to.Value);
        }

        public static string PeriodBar(string p, string js, string cur, string from, string to, bool future = false)
        {
            (string k, string n)[] chips = { ("today", "Today"), ("7", future ? "Next 7 days" : "7 days"), ("30", future ? "Next 30 days" : "30 days"), ("all", "All") };
            StringBuilder sb = new StringBuilder("<div class=\"" + p + "-period\"><div class=\"" + p + "-chips\">");
            foreach ((string k, string n) in chips)
            {
                sb.Append("<button type=\"button\" class=\"" + p + "-chip" + (k == cur ? " " + p + "-act" : string.Empty) + "\" onclick=\"" + js + "('" + k + "')\">" + n + "</button>");
            }
            sb.Append("</div><label class=\"" + p + "-pf\"><span>From</span><input type=\"date\" id=\"" + p + "-from\" value=\"" + Attr(cur == "range" ? from : string.Empty) + "\" onchange=\"" + js + "('range')\"></label><label class=\"" + p + "-pf\"><span>To</span><input type=\"date\" id=\"" + p + "-to\" value=\"" + Attr(cur == "range" ? to : string.Empty) + "\" onchange=\"" + js + "('range')\"></label><input type=\"hidden\" id=\"" + p + "-per\" value=\"" + Attr(cur) + "\"></div>");
            return sb.ToString();
        }
    }
}
