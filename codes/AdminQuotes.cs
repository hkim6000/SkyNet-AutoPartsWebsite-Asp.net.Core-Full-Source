using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminQuotes : WebPage
    {
        private const string AreaKey = "quotes";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Quotes & inquiries");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--qt-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("qt-menu", Auth.Menu("qt", "AdminQuotes", site));
            response.SetElementContents("qt-who", Auth.Who("qt", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminQuotesJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminQuotesJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("qt-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminQuotesJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"qt-tr qt-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private const int Size = 20;

        private static (string Key, string Name)[] StatusesFor(string kind)
        {
            return kind == "quote" ? Ui.QuoteStatuses : Ui.MsgStatuses;
        }

        private static decimal CatalogTotal(SiteData site, Inquiry q)
        {
            return q.Items.Sum(i => (Ui.ProductOf(site, i.Product)?.Price ?? 0) * i.Qty);
        }

        private static string Summary(SiteData site, Inquiry q)
        {
            return q.Kind switch
            {
                "quote" => string.Join(", ", q.Items.Select(i => i.Qty + " × " + (Ui.ProductOf(site, i.Product)?.Sku ?? i.Product))) + (q.Vehicle != string.Empty ? " · " + q.Vehicle : string.Empty),
                "question" => (q.Sku != string.Empty ? q.Sku + " · " : string.Empty) + q.Message,
                "warranty" => q.Sku + (q.DealerName != string.Empty ? " · " + q.DealerName : string.Empty) + " · " + q.Message,
                "dealer" => Ui.NameOf(Ui.DealerKinds, q.Topic) + " · " + q.Message,
                _ => Ui.NameOf(Ui.ContactTopics, q.Topic) + " · " + q.Message
            };
        }

        private static string Chips(SiteData site, string cur)
        {
            StringBuilder sb = new StringBuilder("<div class=\"qt-chips qt-kchips\"><button type=\"button\" class=\"qt-chip" + (cur == string.Empty ? " qt-act" : string.Empty) + "\" onclick=\"AdminQuotesJs.setKind('')\">All<em>" + site.Inquiries.Count(x => x.Status == "new") + " new</em></button>");
            foreach ((string k, string n) in Ui.InquiryKinds)
            {
                int cnt = site.Inquiries.Count(x => x.Kind == k && x.Status == "new");
                sb.Append("<button type=\"button\" class=\"qt-chip" + (cur == k ? " qt-act" : string.Empty) + "\" onclick=\"AdminQuotesJs.setKind('" + k + "')\">" + Ui.Enc(n.EndsWith("y") ? n.Substring(0, n.Length - 1) + "ies" : n + "s") + (cnt > 0 ? "<em>" + cnt + " new</em>" : string.Empty) + "</button>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            string kind = Ui.Has(Ui.InquiryKinds, arg) ? arg : string.Empty;
            List<(string, string)> statuses = Ui.QuoteStatuses.Concat(Ui.MsgStatuses).Select(s => (s.Key, s.Name)).Distinct().ToList();
            StringBuilder sb = new StringBuilder("<div class=\"qt-ptop\"><div><h1>Quotes &amp; inquiries</h1><p>Quote requests, product questions, dealer inquiries, warranty claims and messages from the website.</p></div><div class=\"qt-acts\"><button type=\"button\" class=\"qt-btn qt-btn-ghost\" onclick=\"AdminQuotesJs.csv()\">Export CSV</button></div></div><div id=\"qt-kinds\">" + Chips(site, kind) + "</div>");
            sb.Append("<div class=\"qt-bar\"><label>Search<input type=\"search\" id=\"qt-q\" list=\"qt-qlist\" placeholder=\"Name, company, email, part number or ID\" maxlength=\"60\" oninput=\"AdminQuotesJs.search()\"></label>" + Ui.DataList("qt-qlist", site.Inquiries.Select(x => x.Name).Concat(site.Inquiries.Select(x => x.Company))) + "<label>Status<select id=\"qt-fstat\" onchange=\"AdminQuotesJs.list(1)\"><option value=\"\">Open (not closed, won or lost)</option>" + Ui.Opts(statuses, string.Empty) + "<option value=\"all\">Everything</option></select></label></div><div class=\"qt-bar\" id=\"qt-perbar\">" + Ui.PeriodBar("qt", "AdminQuotesJs.per", "all", string.Empty, string.Empty) + "</div><div class=\"qt-tbl qt-qttbl\" id=\"qt-list\"></div><div id=\"qt-pager\"></div>");
            return sb.ToString();
        }

        private List<Inquiry> Filtered(SiteData site)
        {
            string kind = Ui.Has(Ui.InquiryKinds, GetDataValue("qkind")) ? GetDataValue("qkind") : string.Empty;
            string term = Ui.Clip(GetDataValue("qq"), 60);
            string st = GetDataValue("qstat");
            string per = Per(GetDataValue("per") == string.Empty ? "all" : GetDataValue("per"));
            (DateTime? from, DateTime? to) = Ui.Period(Store.Today(site), per, GetDataValue("pfrom"), GetDataValue("pto"));
            IEnumerable<Inquiry> q = site.Inquiries.Where(x => (kind == string.Empty || x.Kind == kind) && (st == "all" || (st == string.Empty ? x.Status is "new" or "quoted" or "replied" : x.Status == st)) && Ui.InPeriod(Store.Local(site, x.Created), from, to));
            if (term != string.Empty)
            {
                q = q.Where(x => (x.Id + " " + x.Name + " " + x.Company + " " + x.Email + " " + x.Sku + " " + x.Vehicle + " " + string.Join(" ", x.Items.Select(i => Ui.ProductOf(site, i.Product)?.Sku ?? string.Empty))).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            return q.OrderBy(x => x.Status == "new" ? 0 : 1).ThenByDescending(x => x.Created).ToList();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminQuotesJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            List<Inquiry> all = Filtered(site);
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("When", string.Empty), ("Kind", string.Empty), ("From", string.Empty), ("What", string.Empty), ("Value", "qt-num"), ("Status", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"qt-tr\"><span class=\"qt-none qt-wide\">Nothing matches.</span></div>");
            }
            foreach (Inquiry q in all.Skip((page - 1) * Size).Take(Size))
            {
                decimal v = q.Kind == "quote" ? (q.Quoted > 0 ? q.Quoted : CatalogTotal(site, q)) : 0;
                sb.Append("<div class=\"qt-tr\"><span data-l=\"When\">" + Store.Local(site, q.Created).ToString("MMM d", Ui.Inv) + "<small>" + Ui.Hm(site, Store.Local(site, q.Created)) + "</small></span><span data-l=\"Kind\">" + Ui.Enc(Ui.NameOf(Ui.InquiryKinds, q.Kind)) + "</span><span data-l=\"From\" class=\"qt-wide\"><b>" + Ui.Enc(q.Name) + "</b><small>" + Ui.Enc(q.Company != string.Empty ? q.Company : q.Email) + "</small></span><span data-l=\"What\" class=\"qt-wide\" title=\"" + Ui.Attr(Summary(site, q)) + "\">" + Ui.Enc(Summary(site, q)) + "</span><span data-l=\"Value\" class=\"qt-num\">" + (v > 0 ? Ui.Money(v) : "&ndash;") + "</span><span data-l=\"Status\">" + Ui.Pill("qt", StatusesFor(q.Kind), q.Status) + "</span><span><button type=\"button\" class=\"qt-btn qt-btn-sm qt-btn-ghost\" onclick=\"AdminQuotesJs.open('" + Ui.Attr(q.Id) + "')\">Open</button></span></div>");
            }
            response.SetElementContents("qt-list", sb.ToString());
            response.SetElementContents("qt-pager", Ui.Pager("qt", page, Size, all.Count, "inquiry", "inquiries", "AdminQuotesJs.list"));
            response.SetElementContents("qt-kinds", Chips(site, Ui.Has(Ui.InquiryKinds, GetDataValue("qkind")) ? GetDataValue("qkind") : string.Empty));
            response.SetElementContents("qt-perbar", Ui.PeriodBar("qt", "AdminQuotesJs.per", Per(GetDataValue("per") == string.Empty ? "all" : GetDataValue("per")), GetDataValue("pfrom"), GetDataValue("pto")));
            return response;
        }

        private static string Dd(string dt, string dd)
        {
            return dd == string.Empty ? string.Empty : "<dt>" + dt + "</dt><dd>" + dd + "</dd>";
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminQuotesJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Inquiry? q = site.Inquiries.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("qid"), 20));
            if (q == null)
            {
                response.ExecuteScript("AdminQuotesJs.toast('That inquiry is no longer here.');");
                return response;
            }
            string kindName = Ui.NameOf(Ui.InquiryKinds, q.Kind);
            string subject = Uri.EscapeDataString(kindName + " " + q.Id + (q.Sku != string.Empty ? " · " + q.Sku : string.Empty) + " · " + site.Settings.Name);
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(kindName) + " <small style=\"font-family:var(--qt-text);font-size:14px;color:var(--qt-ink2)\">" + Ui.Enc(q.Id) + "</small></h2><div class=\"qt-f2\"><div><h3>From</h3><dl class=\"qt-dl\"><dt>Name</dt><dd>" + Ui.Enc(q.Name) + "</dd>" + Dd("Company", Ui.Enc(q.Company)) + "<dt>Email</dt><dd><a href=\"mailto:" + Ui.Attr(q.Email) + "?subject=" + subject + "\">" + Ui.Enc(q.Email) + "</a></dd>" + Dd("Phone", Ui.Enc(q.Phone)) + Dd("State", Ui.Enc(Ui.StateName(q.State))) + Dd("Vehicle", Ui.Enc(q.Vehicle)) + "<dt>Received</dt><dd>" + Ui.Time(q.Created, "full") + "</dd></dl></div><div><h3>Status</h3><div class=\"qt-stbtns\">");
            foreach ((string k, string n) in StatusesFor(q.Kind))
            {
                sb.Append("<button type=\"button\" class=\"qt-btn qt-btn-sm " + (q.Status == k ? "qt-btn-accent" : "qt-btn-ghost") + "\" onclick=\"AdminQuotesJs.status('" + k + "')\"" + (q.Status == k ? " disabled" : string.Empty) + ">" + Ui.Enc(n) + "</button>");
            }
            sb.Append("</div>" + (q.Kind == "quote" && q.Quoted > 0 ? "<p style=\"margin-top:10px\"><span class=\"qt-hint\">Quoted</span> <b id=\"qt-qamt\" data-v=\"" + q.Quoted.ToString("0.##", Ui.Inv) + "\">" + Ui.Money(q.Quoted) + "</b></p>" : "<span id=\"qt-qamt\" data-v=\"\" hidden></span>") + "<div style=\"margin-top:12px\"><a class=\"qt-btn qt-btn-sm\" href=\"mailto:" + Ui.Attr(q.Email) + "?subject=" + subject + "\">Reply by email</a></div></div></div>");
            if (q.Kind == "quote")
            {
                sb.Append("<h3>Parts</h3><div class=\"qt-items\"><div class=\"qt-irow\"><span>Product</span><span class=\"qt-num\">Qty</span><span class=\"qt-num\">Each</span><span class=\"qt-num\">Line</span></div>");
                foreach (QuoteLine i in q.Items)
                {
                    Part? p = Ui.ProductOf(site, i.Product);
                    sb.Append("<div class=\"qt-irow\"><span>" + (p != null ? "<b><a href=\"AdminProducts?id=" + Ui.Attr(p.Id) + "\">" + Ui.Enc(p.Name) + "</a></b><small>" + Ui.Enc(p.Sku) + " &middot; " + Ui.Pill("qt", Ui.Stocks, p.Stock) + "</small>" : "<b>" + Ui.Enc(i.Product) + "</b><small>No longer in the catalog</small>") + "</span><span class=\"qt-num\">" + i.Qty + "</span><span class=\"qt-num\">" + (p != null ? Ui.Money(p.Price) : "&ndash;") + "</span><span class=\"qt-num\">" + (p != null ? Ui.Money(p.Price * i.Qty) : "&ndash;") + "</span></div>");
                }
                sb.Append("</div><div class=\"qt-total\"><span>Catalog total</span><span>" + Ui.Money(CatalogTotal(site, q)) + "</span></div>");
            }
            if (q.Kind == "warranty")
            {
                Part? wp = site.Products.FirstOrDefault(p => p.Sku.Equals(q.Sku, StringComparison.OrdinalIgnoreCase));
                int months = wp != null && wp.WarrantyMonths > 0 ? wp.WarrantyMonths : site.Settings.WarrantyMonths;
                bool inside = q.Purchased != null && q.Purchased.Value.AddMonths(months) >= Store.Today(site);
                sb.Append("<h3>Claim</h3><dl class=\"qt-dl\"><dt>Part number</dt><dd>" + (wp != null ? "<a href=\"AdminProducts?id=" + Ui.Attr(wp.Id) + "\">" + Ui.Enc(q.Sku) + "</a> &middot; " + Ui.Enc(wp.Name) : Ui.Enc(q.Sku) + " <span class=\"qt-hint\">(not in the catalog)</span>") + "</dd>" + Dd("Purchased", q.Purchased != null ? Ui.Day(q.Purchased.Value) + " &middot; " + (inside ? "<span class=\"qt-st qt-st-live\">Inside the " + months + "-month warranty</span>" : "<span class=\"qt-st qt-st-out\">Outside the " + months + "-month warranty</span>") : string.Empty) + Dd("Bought from", Ui.Enc(q.DealerName)) + "</dl>");
            }
            if (q.Kind == "question" && q.Sku != string.Empty)
            {
                Part? qp = site.Products.FirstOrDefault(p => p.Sku == q.Sku);
                sb.Append("<h3>About</h3><p>" + (qp != null ? "<a href=\"AdminProducts?id=" + Ui.Attr(qp.Id) + "\">" + Ui.Enc(qp.Sku) + " &middot; " + Ui.Enc(qp.Name) + "</a>" : Ui.Enc(q.Sku)) + "</p>");
            }
            if (q.Kind == "dealer")
            {
                sb.Append("<h3>Shop</h3><dl class=\"qt-dl\"><dt>Type</dt><dd>" + Ui.Enc(Ui.NameOf(Ui.DealerKinds, q.Topic)) + "</dd></dl><p class=\"qt-hint\">When the shop signs up, add it in <a href=\"AdminDealers?new=1&name=" + Uri.EscapeDataString(q.Company) + "&state=" + Ui.Attr(q.State) + "&kind=" + Ui.Attr(q.Topic) + "&email=" + Uri.EscapeDataString(q.Email) + "&phone=" + Uri.EscapeDataString(q.Phone) + "\">Dealers</a>.</p>");
            }
            if (q.Kind == "contact")
            {
                sb.Append("<h3>Topic</h3><p>" + Ui.Enc(Ui.NameOf(Ui.ContactTopics, q.Topic)) + "</p>");
            }
            if (q.Message != string.Empty)
            {
                sb.Append("<h3>Message</h3><div class=\"qt-msg\">" + Ui.Enc(q.Message) + "</div>");
            }
            sb.Append("<h3>Notes</h3><div class=\"qt-notes\" id=\"qt-notes\">");
            if (q.Notes.Count == 0)
            {
                sb.Append("<p class=\"qt-hint\">No notes yet.</p>");
            }
            foreach (Note n in q.Notes.OrderByDescending(n => n.When))
            {
                sb.Append("<div class=\"qt-note\"><small>" + Ui.Time(n.When, "full") + " &middot; " + Ui.Enc(Ui.Who(c.U, n.By)) + "</small>" + Ui.Enc(n.Text) + "</div>");
            }
            sb.Append("</div><div class=\"qt-form\" style=\"margin-top:10px\"><label class=\"qt-field\"><span>Add a note</span><textarea id=\"qt-ntext\" rows=\"2\" maxlength=\"600\" placeholder=\"Called back, left a message\"></textarea></label><div><button type=\"button\" class=\"qt-btn qt-btn-sm qt-btn-ghost\" onclick=\"AdminQuotesJs.note()\">Add note</button></div></div>");
            sb.Append("<div class=\"qt-mfoot\"><button type=\"button\" class=\"qt-btn qt-btn-danger qt-left\" onclick=\"AdminQuotesJs.remove()\">Delete</button><button type=\"button\" class=\"qt-btn qt-btn-ghost\" onclick=\"AdminQuotesJs.closeModal()\">Close</button></div>");
            response.SetElementContents("qt-mbody", sb.ToString());
            response.ExecuteScript("AdminQuotesJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> Status()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminQuotesJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("qid"), 20);
            string st = GetDataValue("qst");
            decimal amt = decimal.TryParse(Regex.Replace(GetDataValue("qamt"), "[$,\\s]", string.Empty), NumberStyles.Number, Ui.Inv, out decimal av) ? Math.Clamp(Math.Round(av, 2), 0, 10000000) : 0;
            string what = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Inquiry? q = site.Inquiries.FirstOrDefault(x => x.Id == id);
                if (q == null || !Ui.Has(StatusesFor(q.Kind), st))
                {
                    return response;
                }
                string old = q.Status;
                q.Status = st;
                if (q.Kind == "quote" && st == "quoted")
                {
                    q.Quoted = amt > 0 ? amt : q.Quoted > 0 ? q.Quoted : CatalogTotal(site, q);
                }
                q.Notes.Add(new Note { When = DateTime.UtcNow, By = Store.AdminId, Text = Ui.NameOf(StatusesFor(q.Kind), old) + " → " + Ui.NameOf(StatusesFor(q.Kind), st) + (q.Kind == "quote" && st == "quoted" ? " · " + Ui.Money(q.Quoted).Replace("&minus;", "-") : string.Empty) });
                what = q.Id + " · " + Ui.NameOf(StatusesFor(q.Kind), st);
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "inquiry status", what, true);
            response.ExecuteScript("AdminQuotesJs.reopen();AdminQuotesJs.toast('Saved.');");
            return response;
        }

        public async Task<ApiResponse> Note()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminQuotesJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("qid"), 20);
            string text = Ui.Clip(GetDataValue("ntext"), 600).Trim();
            if (text == string.Empty)
            {
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Inquiry? q = site.Inquiries.FirstOrDefault(x => x.Id == id);
                if (q == null)
                {
                    return response;
                }
                q.Notes.Add(new Note { When = DateTime.UtcNow, By = Store.AdminId, Text = text });
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "inquiry note", id, true);
            response.ExecuteScript("AdminQuotesJs.reopen();");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminQuotesJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("qid"), 20);
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                int n = site.Inquiries.RemoveAll(x => x.Id == id);
                if (n == 0)
                {
                    return response;
                }
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "delete inquiry", id, true);
            response.ExecuteScript("AdminQuotesJs.closeModal();AdminQuotesJs.list();AdminQuotesJs.toast('Deleted.');");
            return response;
        }

        public async Task<ApiResponse> Csv()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminQuotesJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            StringBuilder sb = new StringBuilder("Id,Kind,Received,Name,Company,Email,Phone,State,Vehicle,Items,Part number,Message,Status,Quoted,Catalog total\n");
            foreach (Inquiry q in Filtered(site))
            {
                string[] cells = { q.Id, Ui.NameOf(Ui.InquiryKinds, q.Kind), Store.Local(site, q.Created).ToString("yyyy-MM-dd HH:mm", Ui.Inv), q.Name, q.Company, q.Email, q.Phone, q.State, q.Vehicle, string.Join("; ", q.Items.Select(i => i.Qty + " x " + (Ui.ProductOf(site, i.Product)?.Sku ?? i.Product))), q.Sku, q.Message, Ui.NameOf(StatusesFor(q.Kind), q.Status), q.Quoted > 0 ? q.Quoted.ToString("0.00", Ui.Inv) : string.Empty, q.Kind == "quote" ? CatalogTotal(site, q).ToString("0.00", Ui.Inv) : string.Empty };
                sb.Append(string.Join(",", cells.Select(Ui.Csv)) + "\n");
            }
            await Store.Log(Store.AdminId, "export", "inquiries csv", true);
            response.ExecuteScript("AdminQuotesJs.csvReady('" + Convert.ToBase64String(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray()) + "','inquiries-" + Store.Today(site).ToString("yyyy-MM-dd", Ui.Inv) + ".csv');");
            return response;
        }
    }
}
