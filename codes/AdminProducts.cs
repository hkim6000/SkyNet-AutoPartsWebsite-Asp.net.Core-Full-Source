using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminProducts : WebPage
    {
        private const string AreaKey = "products";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Products");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--pz-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("pz-menu", Auth.Menu("pz", "AdminProducts", site));
            response.SetElementContents("pz-who", Auth.Who("pz", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminProductsJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminProductsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("pz-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminProductsJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"pz-tr pz-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string PickItem(MediaItem m)
        {
            bool vid = m.Kind == "video";
            bool doc = m.Kind == "doc";
            return "<button type=\"button\" class=\"pz-pki\" data-src=\"" + Ui.Attr(m.Full) + "\" data-thumb=\"" + Ui.Attr(m.Thumb) + "\" data-kind=\"" + m.Kind + "\" data-title=\"" + Ui.Attr(m.Title) + "\" onclick=\"AdminProductsJs.pkToggle(this)\" title=\"" + Ui.Attr(m.Title + (m.Tags.Count > 0 ? " · " + string.Join(", ", m.Tags) : string.Empty)) + "\"><img src=\"" + Ui.Attr(m.Thumb) + "\" alt=\"\" loading=\"lazy\">" + (vid ? "<b class=\"pz-vbadge\">" + (m.Seconds / 60) + ":" + (m.Seconds % 60).ToString("00", Ui.Inv) + "</b>" : doc ? "<b class=\"pz-dbadge\">PDF</b>" : string.Empty) + "<span>" + Ui.Enc(m.Title) + "</span><i>&#10003;</i></button>";
        }

        public async Task<ApiResponse> MediaPick()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminProductsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            string allow = GetDataValue("pallow") is "photo" or "video" or "doc" ? GetDataValue("pallow") : string.Empty;
            string kind = allow != string.Empty ? allow : GetDataValue("pkind") is "photo" or "video" or "doc" ? GetDataValue("pkind") : string.Empty;
            string term = Ui.Clip(GetDataValue("pterm"), 60).Trim();
            string tag = Ui.Clip(GetDataValue("ptag"), 40);
            List<string> ctx = Ui.Clip(GetDataValue("pctx"), 80).ToLowerInvariant().Split(',').Select(x => x.Trim()).Where(x => x != string.Empty).ToList();
            int page = int.TryParse(GetDataValue("ppg"), out int pv) ? Math.Clamp(pv, 1, 200) : 1;
            const int size = 30;
            List<MediaItem> all = site.Media.Where(m => (kind == string.Empty ? m.Kind != "doc" : m.Kind == kind) && (tag == string.Empty || m.Tags.Contains(tag)) && (term == string.Empty || (m.Title + " " + m.Alt + " " + string.Join(" ", m.Tags)).Contains(term, StringComparison.OrdinalIgnoreCase))).OrderByDescending(m => m.Tags.Count(t => ctx.Contains(t))).ThenByDescending(m => m.When).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (MediaItem m in all.Skip((page - 1) * size).Take(size))
            {
                sb.Append(PickItem(m));
            }
            if (all.Count == 0)
            {
                sb.Append("<p class=\"pz-none\" style=\"grid-column:1/-1\">Nothing matches. Upload a photo, or add videos and documents in Media.</p>");
            }
            string tags = page == 1 ? JsonSerializer.Serialize(site.Media.SelectMany(m => m.Tags).Distinct().OrderBy(t => t).ToList()) : "null";
            response.ExecuteScript("AdminProductsJs.pkShown(" + JsonSerializer.Serialize(sb.ToString()) + "," + (all.Count > page * size ? "true" : "false") + "," + tags + "," + (page > 1 ? "true" : "false") + ");");
            return response;
        }

        private static string PickExt(byte[] b)
        {
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            {
                return "jpg";
            }
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            {
                return "png";
            }
            if (b.Length > 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            {
                return "webp";
            }
            return string.Empty;
        }

        private static byte[]? PickData(string data, int max)
        {
            Match m = Regex.Match(data ?? string.Empty, "^data:image/(jpeg|png|webp);base64,([A-Za-z0-9+/=]+)$");
            if (!m.Success)
            {
                return null;
            }
            try
            {
                byte[] b = Convert.FromBase64String(m.Groups[2].Value);
                return b.Length <= max && PickExt(b) != string.Empty ? b : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public async Task<ApiResponse> MediaUp()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminProductsJs.expired();");
                return response;
            }
            byte[]? big = PickData(GetDataValue("full"), 8 * 1024 * 1024);
            byte[]? small = PickData(GetDataValue("thumb"), 3 * 1024 * 1024);
            if (big == null || small == null || PickExt(big) != PickExt(small))
            {
                response.ExecuteScript("AdminProductsJs.pkUploaded(false,'That file is not a JPG, PNG or WebP photo, or it is too large.');");
                return response;
            }
            string ext = PickExt(big);
            string id = Guid.NewGuid().ToString("N").Substring(0, 16);
            string dir = Path.Combine(Store.Root, "images", "uploads");
            Directory.CreateDirectory(Path.Combine(dir, "full"));
            Directory.CreateDirectory(Path.Combine(dir, "thumbs"));
            await File.WriteAllBytesAsync(Path.Combine(dir, "full", id + "." + ext), big);
            await File.WriteAllBytesAsync(Path.Combine(dir, "thumbs", id + "." + ext), small);
            string title = Regex.Replace(Ui.Clip(GetDataValue("pname"), 80), "\\s+", " ").Trim();
            List<string> ctx = Ui.Clip(GetDataValue("pctx"), 80).ToLowerInvariant().Split(',').Select(x => Regex.Replace(x, "[^a-z0-9 -]", string.Empty).Trim()).Where(x => x != string.Empty).Distinct().Take(4).ToList();
            int w = int.TryParse(GetDataValue("pw"), out int wv) ? Math.Clamp(wv, 0, 20000) : 0;
            int h = int.TryParse(GetDataValue("ph"), out int hv) ? Math.Clamp(hv, 0, 20000) : 0;
            double r = Math.Min(1.0, 1600.0 / Math.Max(1, Math.Max(w, h)));
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                site.Media.Add(new MediaItem { Id = id, Kind = "photo", Title = title.Length >= 2 ? title : "Photo " + id.Substring(0, 6), Tags = ctx, Full = "images/uploads/full/" + id + "." + ext, Thumb = "images/uploads/thumbs/" + id + "." + ext, Width = (int)Math.Round(w * r), Height = (int)Math.Round(h * r), Bytes = big.Length, By = Store.AdminId, When = DateTime.UtcNow });
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "upload", "photo · " + title, true);
            response.ExecuteScript("AdminProductsJs.pkUploaded(true,'');");
            return response;
        }

        private const int Size = 20;

        private static readonly (string Key, string Name)[] SortOpts = { ("new", "Newest first"), ("name", "Name, A to Z"), ("sku", "Part number"), ("price", "Price, high to low"), ("views", "Most viewed"), ("quotes", "Most quoted") };

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            string[] a = (arg ?? string.Empty).Split('|');
            string st = Ui.Has(Ui.ProductStatuses, a[0]) ? a[0] : string.Empty;
            string so = a.Length > 1 && SortOpts.Any(s => s.Key == a[1]) ? a[1] : "new";
            string stock = a.Length > 2 && Ui.Has(Ui.Stocks, a[2]) ? a[2] : string.Empty;
            StringBuilder sb = new StringBuilder("<div class=\"pz-ptop\"><div><h1>Products</h1><p>The catalog. Only Live products show on the website; drafts and retired products stay here.</p></div><div class=\"pz-acts\"><button type=\"button\" class=\"pz-btn pz-btn-accent\" onclick=\"AdminProductsJs.edit('')\">Add a product</button></div></div>");
            sb.Append("<div class=\"pz-bar\"><label>Search<input type=\"search\" id=\"pz-q\" list=\"pz-qlist\" placeholder=\"Name, part number or vehicle\" maxlength=\"40\" oninput=\"AdminProductsJs.search()\"></label>" + Ui.DataList("pz-qlist", site.Products.Select(p => p.Sku).Concat(site.Makes.Select(m => m.Name))));
            sb.Append("<label>Category<select id=\"pz-fcat\" onchange=\"AdminProductsJs.list(1)\"><option value=\"\">All categories</option>" + Ui.Opts(Ui.Categories.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label>");
            sb.Append("<label>Series<select id=\"pz-fser\" onchange=\"AdminProductsJs.list(1)\"><option value=\"\">All series</option>" + Ui.Opts(Ui.Series.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label>");
            sb.Append("<label>Status<select id=\"pz-fstat\" onchange=\"AdminProductsJs.list(1)\"><option value=\"\">All but retired</option>" + Ui.Opts(Ui.ProductStatuses.Select(s => (s.Key, s.Name)), st) + "<option value=\"all\"" + (st == "all" ? " selected" : string.Empty) + ">Everything</option></select></label>");
            sb.Append("<label>Stock<select id=\"pz-fstock\" onchange=\"AdminProductsJs.list(1)\"><option value=\"\">Any stock</option>" + Ui.Opts(Ui.Stocks.Select(s => (s.Key, s.Name)), stock) + "</select></label>");
            sb.Append("<label>Sort<select id=\"pz-fsort\" onchange=\"AdminProductsJs.list(1)\">" + Ui.Opts(SortOpts.Select(s => (s.Key, s.Name)), so) + "</select></label></div><div class=\"pz-tbl pz-pztbl\" id=\"pz-list\"></div><div id=\"pz-pager\"></div>");
            return sb.ToString();
        }

        private static int QuoteCount(SiteData site, string id)
        {
            return site.Inquiries.Count(q => q.Kind == "quote" && q.Items.Any(i => i.Product == id));
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminProductsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            string term = Ui.Clip(GetDataValue("pq"), 40);
            string cat = GetDataValue("pcat");
            string ser = GetDataValue("pser");
            string st = GetDataValue("pstat");
            string stock = GetDataValue("pstock");
            IEnumerable<Part> q = site.Products.Where(p => (st == "all" || (st == string.Empty ? p.Status != "retired" : p.Status == st)) && (cat == string.Empty || p.Category == cat) && (ser == string.Empty || p.Series == ser) && (stock == string.Empty || p.Stock == stock));
            if (term != string.Empty)
            {
                q = q.Where(p => (p.Name + " " + p.Sku + " " + p.Id + " " + string.Join(" ", p.Fitments.Select(f => Ui.MakeName(site, f.Make) + " " + Ui.ModelName(site, f.Make, f.Model)))).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            q = GetDataValue("psort") switch
            {
                "name" => q.OrderBy(p => p.Name),
                "sku" => q.OrderBy(p => p.Sku),
                "price" => q.OrderByDescending(p => p.Price),
                "views" => q.OrderByDescending(p => p.Views),
                "quotes" => q.OrderByDescending(p => QuoteCount(site, p.Id)),
                _ => q.OrderByDescending(p => p.Created)
            };
            List<Part> all = q.ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { (string.Empty, string.Empty), ("Product", string.Empty), ("Category", string.Empty), ("Price", "pz-num"), ("Stock", string.Empty), ("Status", string.Empty), ("Views", "pz-num"), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"pz-tr\"><span class=\"pz-none pz-wide\">No products match.</span></div>");
            }
            foreach (Part p in all.Skip((page - 1) * Size).Take(Size))
            {
                string img = p.Photos.Count > 0 ? "<img class=\"pz-thumb\" src=\"" + Ui.Attr(Ui.Thumb(Ui.Cover(site, p))) + "\" alt=\"\" loading=\"lazy\">" : "<span class=\"pz-hint\">No photo</span>";
                string flags = (p.Featured ? "<span class=\"pz-flag pz-feat\">Featured</span>" : string.Empty) + (p.Docs.Count > 0 ? "<span class=\"pz-flag\">" + p.Docs.Count + " doc" + (p.Docs.Count == 1 ? string.Empty : "s") + "</span>" : string.Empty);
                sb.Append("<div class=\"pz-tr\"><span>" + img + "</span><span class=\"pz-wide\"><b>" + Ui.Enc(p.Name) + "</b><small>" + Ui.Enc(p.Sku) + " &middot; " + (p.Fitments.Count > 0 ? Ui.FitLine(site, p.Fitments[0]) + (p.Fitments.Count > 1 ? " +" + (p.Fitments.Count - 1) : string.Empty) : "No fitment") + "</small></span><span data-l=\"Category\">" + Ui.Enc(Ui.CategoryName(p.Category)) + "<small>" + Ui.Enc(Ui.SeriesName(p.Series)) + "</small></span><span data-l=\"Price\" class=\"pz-num\">" + Ui.Money(p.Price) + "</span><span data-l=\"Stock\">" + Ui.Pill("pz", Ui.Stocks, p.Stock) + "</span><span data-l=\"Status\">" + Ui.Pill("pz", Ui.ProductStatuses, p.Status) + (flags != string.Empty ? "<span class=\"pz-flags\">" + flags + "</span>" : string.Empty) + "</span><span data-l=\"Views\" class=\"pz-num\">" + Ui.Num(p.Views) + "<small>" + Ui.Plural(QuoteCount(site, p.Id), "quote", "quotes") + "</small></span><span><button type=\"button\" class=\"pz-btn pz-btn-sm pz-btn-ghost\" onclick=\"AdminProductsJs.edit('" + Ui.Attr(p.Id) + "')\">Edit</button></span></div>");
            }
            response.SetElementContents("pz-list", sb.ToString());
            response.SetElementContents("pz-pager", Ui.Pager("pz", page, Size, all.Count, "product", "products", "AdminProductsJs.list"));
            return response;
        }

        private static string F(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"pz-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"pz-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private static string S(string id, string label, IEnumerable<(string, string)> opts, string cur, string extra = "")
        {
            return "<label class=\"pz-field\"><span>" + label + "</span><select id=\"pz-" + id + "\"" + extra + ">" + Ui.Opts(opts, cur) + "</select></label>";
        }

        private static string SpecRowHtml(SpecRow s)
        {
            return "<div class=\"pz-srow\"><input type=\"text\" maxlength=\"40\" value=\"" + Ui.Attr(s.Name) + "\" placeholder=\"Spring rate, front\" aria-label=\"Name\"><input type=\"text\" maxlength=\"80\" value=\"" + Ui.Attr(s.Value) + "\" placeholder=\"Value, in US units\" aria-label=\"Value\"><select aria-label=\"Unit\">" + Ui.Opts(Ui.SpecUnits.Select(u => (u.Key, u.Name)), s.Unit) + "</select><button type=\"button\" class=\"pz-ibtn\" aria-label=\"Move up\" onclick=\"AdminProductsJs.specMove(this,-1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 15l6-6 6 6\"/></svg></button><button type=\"button\" class=\"pz-ibtn\" aria-label=\"Remove\" onclick=\"AdminProductsJs.dropRow(this)\">&times;</button></div>";
        }

        private static string FitRowHtml(SiteData site, Fitment f)
        {
            return "<div class=\"pz-frow\" data-model=\"" + Ui.Attr(f.Model) + "\"><select data-f=\"make\" aria-label=\"Make\" onchange=\"AdminProductsJs.models(this,'')\"><option value=\"\">Make</option>" + Ui.Opts(site.Makes.Select(m => (m.Key, m.Name)), f.Make) + "</select><select data-f=\"model\" aria-label=\"Model\"><option value=\"\">Model</option></select><input type=\"text\" inputmode=\"numeric\" maxlength=\"4\" value=\"" + (f.From > 0 ? f.From.ToString(Ui.Inv) : string.Empty) + "\" placeholder=\"From\" aria-label=\"From year\"><input type=\"text\" inputmode=\"numeric\" maxlength=\"4\" value=\"" + (f.To > 0 ? f.To.ToString(Ui.Inv) : string.Empty) + "\" placeholder=\"To\" aria-label=\"To year\"><select aria-label=\"Position\">" + Ui.Opts(Ui.Positions.Select(p => (p.Key, p.Name)), f.Position) + "</select><input type=\"text\" maxlength=\"60\" value=\"" + Ui.Attr(f.Note) + "\" placeholder=\"Note (optional)\" aria-label=\"Note\"><button type=\"button\" class=\"pz-ibtn\" aria-label=\"Remove\" onclick=\"AdminProductsJs.dropRow(this)\">&times;</button></div>";
        }

        public async Task<ApiResponse> Form()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminProductsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            Part? found = site.Products.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("pid"), 20));
            Part d = found ?? new Part { Status = "draft", Stock = "in", WarrantyMonths = site.Settings.WarrantyMonths, Created = DateTime.UtcNow };
            string makes = JsonSerializer.Serialize(site.Makes.Select(m => new { key = m.Key, name = m.Name, models = m.Models.Select(x => new { key = x.Key, name = x.Name }) }));
            var docs = d.Docs.Select(x => new { src = x, thumb = Ui.Poster(site, x), title = site.Media.FirstOrDefault(m => m.Full == x)?.Title ?? Path.GetFileName(x) });
            StringBuilder sb = new StringBuilder("<h2>" + (found == null ? "Add a product" : Ui.Enc(d.Name) + " <small style=\"font-family:var(--pz-text);font-size:14px;color:var(--pz-ink2)\">" + Ui.Enc(d.Sku) + "</small>") + "</h2><div id=\"pz-pdata\" data-id=\"" + Ui.Attr(found?.Id ?? string.Empty) + "\" data-photos=\"" + Ui.Attr(JsonSerializer.Serialize(d.Photos)) + "\" data-thumbs=\"" + Ui.Attr(JsonSerializer.Serialize(d.Photos.Distinct().ToDictionary(p => p, p => Ui.Poster(site, p)))) + "\" data-docs=\"" + Ui.Attr(JsonSerializer.Serialize(docs)) + "\" data-makes=\"" + Ui.Attr(makes) + "\" hidden></div>");
            sb.Append("<div class=\"pz-tabs\" role=\"tablist\"><button type=\"button\" class=\"pz-tab pz-act\" data-p=\"det\" onclick=\"AdminProductsJs.pane('det')\">Details</button><button type=\"button\" class=\"pz-tab\" data-p=\"txt\" onclick=\"AdminProductsJs.pane('txt')\">Text</button><button type=\"button\" class=\"pz-tab\" data-p=\"spc\" onclick=\"AdminProductsJs.pane('spc')\">Specs<em>" + d.Specs.Count + "</em></button><button type=\"button\" class=\"pz-tab\" data-p=\"fit\" onclick=\"AdminProductsJs.pane('fit')\">Fitment<em>" + d.Fitments.Count + "</em></button><button type=\"button\" class=\"pz-tab\" data-p=\"med\" onclick=\"AdminProductsJs.pane('med')\">Media<em id=\"pz-pcount\">" + (d.Photos.Count + d.Docs.Count) + "</em></button>" + (d.History.Count > 0 ? "<button type=\"button\" class=\"pz-tab\" data-p=\"his\" onclick=\"AdminProductsJs.pane('his')\">History</button>" : string.Empty) + "</div>");
            sb.Append("<fieldset class=\"pz-form\" id=\"pz-pform\" style=\"border:0;padding:0;margin:0;min-width:0\"><div class=\"pz-panes\">");
            sb.Append("<div data-p=\"det\" class=\"pz-act\"><div class=\"pz-f3\">" + F("pname", "Name", d.Name, 90) + F("psku", "Part number", d.Sku, 30, "text", " placeholder=\"IV-CO-1010\" style=\"text-transform:uppercase\"") + S("pcat", "Category", Ui.Categories.Select(x => (x.Key, x.Name)), d.Category) + "</div>");
            sb.Append("<div class=\"pz-f4\">" + S("pser", "Series", Ui.Series.Select(x => (x.Key, x.Name)), d.Series) + F("pprice", "Price, MSRP ($)", d.Price > 0 ? d.Price.ToString("0.##", Ui.Inv) : string.Empty, 9, "text", " inputmode=\"decimal\"") + S("pstock", "Stock", Ui.Stocks.Select(x => (x.Key, x.Name)), d.Stock) + F("plead", "Lead time (days)", d.LeadDays.ToString(Ui.Inv), 3, "text", " inputmode=\"numeric\"") + "</div>");
            sb.Append("<div class=\"pz-f4\">" + F("pwar", "Warranty (months)", d.WarrantyMonths.ToString(Ui.Inv), 3, "text", " inputmode=\"numeric\"") + F("pinst", "Install time (minutes)", d.InstallMinutes.ToString(Ui.Inv), 4, "text", " inputmode=\"numeric\"") + S("pstatus", "Status", Ui.ProductStatuses.Select(x => (x.Key, x.Name)), d.Status) + "<label class=\"pz-chk\" style=\"align-self:end;min-height:44px\"><input type=\"checkbox\" id=\"pz-pfeatured\"" + (d.Featured ? " checked" : string.Empty) + "> Featured on the home page</label></div>");
            sb.Append(F("psum", "One-line summary (cards and search results)", d.Summary, 160) + "<p class=\"pz-hint\">Live products appear on the website and in the fitment finder. A product needs at least one photo to go live.</p></div>");
            sb.Append("<div data-p=\"txt\"><label class=\"pz-field\"><span>Overview (leave an empty line between paragraphs)</span><textarea id=\"pz-ptext\" rows=\"8\" maxlength=\"4000\">" + Ui.Enc(d.Text) + "</textarea></label><div class=\"pz-f2\"><label class=\"pz-field\"><span>Features (one per line, up to 8)</span><textarea id=\"pz-pfeat2\" rows=\"5\" maxlength=\"800\">" + Ui.Enc(string.Join("\n", d.Features)) + "</textarea></label><label class=\"pz-field\"><span>In the box (one per line, up to 10)</span><textarea id=\"pz-pbox\" rows=\"5\" maxlength=\"800\">" + Ui.Enc(string.Join("\n", d.Box)) + "</textarea></label></div>" + F("ptorque", "Torque and install notes", d.Torque, 200) + "</div>");
            sb.Append("<div data-p=\"spc\"><p class=\"pz-hint\">Enter values in US units; the website converts to metric when Settings say so. Pick Text for anything that is not a measurement.</p><div id=\"pz-specs\" style=\"display:grid;gap:8px\">");
            foreach (SpecRow s in d.Specs)
            {
                sb.Append(SpecRowHtml(s));
            }
            sb.Append("</div><template id=\"pz-stpl\">" + SpecRowHtml(new SpecRow()) + "</template><div><button type=\"button\" class=\"pz-btn pz-btn-sm pz-btn-ghost\" onclick=\"AdminProductsJs.addSpec()\">Add a spec</button></div></div>");
            sb.Append("<div data-p=\"fit\"><p class=\"pz-hint\">Each line is one application. Makes and models come from Settings.</p><div id=\"pz-fits\" style=\"display:grid;gap:8px\">");
            foreach (Fitment f in d.Fitments)
            {
                sb.Append(FitRowHtml(site, f));
            }
            sb.Append("</div><template id=\"pz-ftpl\">" + FitRowHtml(site, new Fitment { From = site.Settings.YearFrom, To = site.Settings.YearTo }) + "</template><div><button type=\"button\" class=\"pz-btn pz-btn-sm pz-btn-ghost\" onclick=\"AdminProductsJs.addFit()\">Add an application</button></div></div>");
            sb.Append("<div data-p=\"med\"><p class=\"pz-hint\">The first photo is the cover on cards. Videos show in the gallery with a play button.</p><div class=\"pz-photos\" id=\"pz-photos\"></div><div><button type=\"button\" class=\"pz-btn pz-btn-sm\" onclick=\"AdminProductsJs.addMedia()\">Add photos or videos</button></div><h3>Documents</h3><div class=\"pz-docl\" id=\"pz-docs\"></div><div><button type=\"button\" class=\"pz-btn pz-btn-sm pz-btn-ghost\" onclick=\"AdminProductsJs.addDoc()\">Attach a PDF from Media</button></div><p class=\"pz-hint\">Upload new PDFs in <a href=\"AdminMedia\">Media</a>.</p></div>");
            if (d.History.Count > 0)
            {
                sb.Append("<div data-p=\"his\"><div class=\"pz-hist\">");
                foreach (ProductEvent h in d.History.OrderByDescending(h => h.When))
                {
                    sb.Append("<div><span>" + Store.Local(site, h.When).ToString("MMM d, yyyy", Ui.Inv) + " &middot; " + Ui.Enc(h.Event) + "</span><span class=\"pz-hint\">" + Ui.Enc(Ui.Who(c.U, h.By)) + "</span></div>");
                }
                sb.Append("</div><p class=\"pz-hint\">" + Ui.Num(d.Views) + " page views &middot; " + Ui.Num(d.Compares) + " compares &middot; " + Ui.Plural(QuoteCount(site, d.Id), "quote request", "quote requests") + ".</p></div>");
            }
            sb.Append("</div></fieldset><em class=\"pz-err\" id=\"pz-e-save\" role=\"alert\"></em><div class=\"pz-mfoot\">");
            if (found != null)
            {
                sb.Append("<button type=\"button\" class=\"pz-btn pz-btn-danger pz-left\" onclick=\"AdminProductsJs.remove('" + Ui.Attr(found.Id) + "','" + Ui.Js(found.Name) + "')\">Delete</button>");
                if (Ui.Public(found))
                {
                    sb.Append("<a class=\"pz-btn pz-btn-ghost\" href=\"Product?id=" + Uri.EscapeDataString(found.Id) + "\" target=\"_blank\" rel=\"noopener\">Open on the website</a>");
                }
            }
            sb.Append("<button type=\"button\" class=\"pz-btn pz-btn-ghost\" onclick=\"AdminProductsJs.closeModal()\">Cancel</button><button type=\"button\" class=\"pz-btn pz-btn-accent\" onclick=\"AdminProductsJs.save()\">" + (found == null ? "Add product" : "Save") + "</button></div>");
            response.SetElementContents("pz-mbody", sb.ToString());
            response.ExecuteScript("AdminProductsJs.opened();");
            return response;
        }

        private static int Int(string v, int min, int max)
        {
            return int.TryParse((v ?? string.Empty).Trim(), NumberStyles.Integer, Ui.Inv, out int n) ? Math.Clamp(n, min, max) : 0;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminProductsJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("pid"), 20);
            string name = Regex.Replace(Ui.Clip(GetDataValue("pname"), 90), "\\s+", " ").Trim();
            string sku = Regex.Replace(Ui.Clip(GetDataValue("psku"), 30).ToUpperInvariant(), "[^A-Z0-9-]", string.Empty);
            decimal price = decimal.TryParse(Regex.Replace(GetDataValue("pprice"), "[$,\\s]", string.Empty), NumberStyles.Number, Ui.Inv, out decimal pv) ? Math.Clamp(Math.Round(pv, 2), 0, 1000000) : 0;
            List<string> photos = GetDataValue("pphotos").Split('|').Where(p => p != string.Empty && (Ui.SafeImage(p) || Ui.SafeVideo(p))).Distinct().Take(12).ToList();
            List<string> docs = GetDataValue("pdocs").Split('|').Where(p => p != string.Empty && Ui.SafeDoc(p)).Distinct().Take(8).ToList();
            string want = Ui.Has(Ui.ProductStatuses, GetDataValue("pstatus")) ? GetDataValue("pstatus") : "draft";
            List<SpecRow> specs = new List<SpecRow>();
            foreach (string row in GetDataValue("pspecs").Split('~'))
            {
                string[] x = row.Split('|');
                if (x.Length == 3 && x[0].Trim() != string.Empty)
                {
                    specs.Add(new SpecRow { Name = Ui.Clip(x[0], 40), Value = Ui.Clip(x[1], 80), Unit = Ui.Has(Ui.SpecUnits, x[2]) ? x[2] : string.Empty });
                }
            }
            string err = string.Empty;
            string tab = "det";
            if (name.Length < 3)
            {
                err = "Enter the product name.";
            }
            else if (sku.Length < 3)
            {
                err = "Enter a part number, letters, numbers and dashes.";
            }
            else if (price <= 0)
            {
                err = "Enter the price.";
            }
            else if (want == "live" && !photos.Any(p => !Ui.IsVideo(p)))
            {
                err = "Add at least one photo before the product goes live.";
                tab = "med";
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminProductsJs.fail('" + Ui.Js(err) + "','" + tab + "');");
                return response;
            }
            bool isNew = false;
            string saved = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                if (site.Products.Any(p => p.Id != id && p.Sku == sku))
                {
                    response.ExecuteScript("AdminProductsJs.fail('Another product already has that part number.','det');");
                    return response;
                }
                List<Fitment> fits = new List<Fitment>();
                foreach (string row in GetDataValue("pfits").Split('~'))
                {
                    string[] x = row.Split('|');
                    if (x.Length != 6)
                    {
                        continue;
                    }
                    Make? mk = Ui.MakeOf(site, x[0]);
                    VehicleModel? md = mk?.Models.FirstOrDefault(m => m.Key == x[1]);
                    int from = Int(x[2], 1950, 2100);
                    int to = Int(x[3], 1950, 2100);
                    if (mk == null || md == null)
                    {
                        continue;
                    }
                    if (from == 0 || to == 0 || to < from)
                    {
                        response.ExecuteScript("AdminProductsJs.fail('" + Ui.Js("Check the years for " + mk.Name + " " + md.Name + ".") + "','fit');");
                        return response;
                    }
                    fits.Add(new Fitment { Make = mk.Key, Model = md.Key, From = from, To = to, Position = Ui.Has(Ui.Positions, x[4]) ? x[4] : "set", Note = Ui.Clip(x[5], 60) });
                }
                Part? d = site.Products.FirstOrDefault(x => x.Id == id);
                DateTime today = Store.Today(site);
                if (d == null)
                {
                    isNew = true;
                    d = new Part { Id = Store.NextId(site.Products.Select(x => x.Id), "P-", 1000), Status = "draft", Created = DateTime.UtcNow, History = new List<ProductEvent> { new ProductEvent { When = DateTime.UtcNow, Event = "Created", By = Store.AdminId } } };
                    site.Products.Add(d);
                }
                string old = d.Status;
                d.Name = name;
                d.Sku = sku;
                d.Category = Ui.Has(Ui.Categories, GetDataValue("pcat")) ? GetDataValue("pcat") : d.Category;
                d.Series = Ui.Has(Ui.Series, GetDataValue("pser")) ? GetDataValue("pser") : d.Series;
                d.Summary = Ui.Clip(GetDataValue("psum"), 160).Trim();
                d.Price = price;
                d.Stock = Ui.Has(Ui.Stocks, GetDataValue("pstock")) ? GetDataValue("pstock") : d.Stock;
                d.LeadDays = Int(GetDataValue("plead"), 0, 365);
                d.WarrantyMonths = Int(GetDataValue("pwar"), 0, 240);
                d.InstallMinutes = Int(GetDataValue("pinst"), 0, 3000);
                d.Featured = GetDataValue("pfeatured") == "1";
                d.Text = Ui.Clip(GetDataValue("ptext"), 4000).Replace("\r", string.Empty).Trim();
                d.Features = Ui.SplitLines(GetDataValue("pfeat2"), 8, 80);
                d.Box = Ui.SplitLines(GetDataValue("pbox"), 10, 80);
                d.Torque = Ui.Clip(GetDataValue("ptorque"), 200).Trim();
                d.Specs = specs.Take(20).ToList();
                d.Fitments = fits.Take(20).ToList();
                d.Photos = photos;
                d.Docs = docs;
                if (want != old)
                {
                    d.Status = want;
                    d.History.Add(new ProductEvent { When = DateTime.UtcNow, Event = Ui.NameOf(Ui.ProductStatuses, want), By = Store.AdminId });
                    if (want == "live" && d.Listed == null)
                    {
                        d.Listed = today;
                    }
                }
                if (d.Status != "live")
                {
                    d.Featured = false;
                }
                saved = d.Id;
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, isNew ? "add product" : "product", sku + " · " + name, true);
            response.ExecuteScript("AdminProductsJs.saved('" + Ui.Js(saved) + "'," + (isNew ? "true" : "false") + ");");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminProductsJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("pid"), 20);
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Part? d = site.Products.FirstOrDefault(x => x.Id == id);
                if (d == null)
                {
                    return response;
                }
                name = d.Sku + " · " + d.Name;
                site.Products.Remove(d);
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "delete product", name, true);
            response.ExecuteScript("AdminProductsJs.deleted();");
            return response;
        }
    }
}
