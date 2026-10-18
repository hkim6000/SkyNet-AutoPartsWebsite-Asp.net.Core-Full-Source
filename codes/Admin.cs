using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class Admin : WebPage
    {
        private const string AreaKey = "";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Dashboard");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--db-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("db-menu", Auth.Menu("db", "Admin", site));
            response.SetElementContents("db-who", Auth.Who("db", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("db-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"db-tr db-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string Kpi(string label, string value, string sub, string href, bool hot = false)
        {
            string inner = "<span>" + label + "</span><b>" + value + "</b><small>" + sub + "</small>";
            return href != string.Empty ? "<a class=\"db-kpi" + (hot ? " db-hot" : string.Empty) + "\" href=\"" + href + "\">" + inner + "</a>" : "<div class=\"db-kpi\">" + inner + "</div>";
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            DateTime now = Store.Now(site);
            DateTime today = now.Date;
            DateTime month = new DateTime(today.Year, today.Month, 1);
            string hello = now.Hour < 12 ? "Good morning" : now.Hour < 18 ? "Good afternoon" : "Good evening";
            int newQuotes = site.Inquiries.Count(x => x.Kind == "quote" && x.Status == "new");
            int newOther = site.Inquiries.Count(x => x.Kind != "quote" && x.Status == "new");
            int newApps = site.Jobs.Sum(j => j.Apps.Count(a => a.Status == "new"));
            decimal wonMonth = site.Inquiries.Where(x => x.Kind == "quote" && x.Status == "won" && Store.Local(site, x.Created) >= month).Sum(x => x.Quoted);
            decimal openValue = site.Inquiries.Where(x => x.Kind == "quote" && x.Status == "quoted").Sum(x => x.Quoted);
            StringBuilder sb = new StringBuilder("<div class=\"db-ptop\"><div><h1>" + hello + ", " + Ui.Enc(c.U.Name.Split(' ')[0]) + "</h1><p>" + Ui.LongDay(today) + "</p></div><div class=\"db-acts\"><a class=\"db-btn db-btn-accent\" href=\"AdminProducts?id=new\">Add a product</a><a class=\"db-btn db-btn-ghost\" href=\"Home\" target=\"_blank\" rel=\"noopener\">View site</a></div></div><div class=\"db-kpis\">");
            sb.Append(Kpi("New quotes", Ui.Num(newQuotes), newQuotes == 1 ? "waiting for a price" : "waiting for prices", "AdminQuotes?kind=quote&status=new", newQuotes > 0));
            sb.Append(Kpi("Open quotes", Ui.Short(openValue), Ui.Plural(site.Inquiries.Count(x => x.Kind == "quote" && x.Status == "quoted"), "quote", "quotes") + " sent, not decided", "AdminQuotes?kind=quote&status=quoted"));
            sb.Append(Kpi("Won this month", Ui.Short(wonMonth), Ui.Plural(site.Inquiries.Count(x => x.Kind == "quote" && x.Status == "won" && Store.Local(site, x.Created) >= month), "order", "orders"), "AdminQuotes?kind=quote&status=won"));
            sb.Append(Kpi("Messages", Ui.Num(newOther), "questions, dealers, warranty, contact", "AdminQuotes?status=new", newOther > 0));
            sb.Append(Kpi("Live products", Ui.Num(site.Products.Count(Ui.Public)), Ui.Plural(site.Products.Count(p => p.Status == "draft"), "draft", "drafts") + " &middot; " + Ui.Plural(site.Products.Count(p => Ui.Public(p) && p.Stock == "out"), "out of stock", "out of stock"), "AdminProducts"));
            sb.Append(Kpi("Applications", Ui.Num(newApps), Ui.Plural(site.Jobs.Count(j => j.Open), "open position", "open positions"), "AdminCareers?tab=apps", newApps > 0));
            sb.Append("</div><div class=\"db-grid2\" style=\"margin-top:0\">");
            sb.Append("<section class=\"db-panel\"><div class=\"db-ph\"><h2>Newest quote requests</h2><a href=\"AdminQuotes?kind=quote\">All quotes</a></div>");
            List<Inquiry> quotes = site.Inquiries.Where(x => x.Kind == "quote").OrderByDescending(x => x.Created).Take(6).ToList();
            if (quotes.Count == 0)
            {
                sb.Append("<p class=\"db-none\">No quote requests yet.</p>");
            }
            foreach (Inquiry q in quotes)
            {
                decimal total = q.Items.Sum(i => (Ui.ProductOf(site, i.Product)?.Price ?? 0) * i.Qty);
                sb.Append("<a class=\"db-row\" href=\"AdminQuotes?id=" + Ui.Attr(q.Id) + "\"><div><b>" + Ui.Enc(q.Name) + (q.Company != string.Empty ? " &middot; " + Ui.Enc(q.Company) : string.Empty) + "</b><small>" + Ui.Enc(string.Join(", ", q.Items.Select(i => i.Qty + " × " + (Ui.ProductOf(site, i.Product)?.Sku ?? i.Product)))) + (q.Vehicle != string.Empty ? " &middot; " + Ui.Enc(q.Vehicle) : string.Empty) + "</small></div><span class=\"db-amt\">" + Ui.Money(q.Status == "new" ? total : q.Quoted) + "</span>" + Ui.Pill("db", Ui.QuoteStatuses, q.Status) + "</a>");
            }
            sb.Append("</section><section class=\"db-panel\"><h2>Needs attention</h2><div class=\"db-todo\">");
            int old = site.Inquiries.Count(x => x.Status == "new" && x.Created < DateTime.UtcNow.AddDays(-2));
            int outStock = site.Products.Count(p => Ui.Public(p) && p.Stock == "out");
            int lowStock = site.Products.Count(p => Ui.Public(p) && p.Stock == "low");
            int drafts = site.Products.Count(p => p.Status == "draft");
            int stale = site.Inquiries.Count(x => x.Kind == "quote" && x.Status == "quoted" && x.Created < DateTime.UtcNow.AddDays(-14));
            int warranty = site.Inquiries.Count(x => x.Kind == "warranty" && x.Status == "new");
            int nothing = 0;
            if (old > 0)
            {
                sb.Append("<a href=\"AdminQuotes?status=new\"><span>Unanswered for more than two days</span><b>" + old + "</b></a>");
                nothing++;
            }
            if (warranty > 0)
            {
                sb.Append("<a href=\"AdminQuotes?kind=warranty&status=new\"><span>New warranty claims</span><b>" + warranty + "</b></a>");
                nothing++;
            }
            if (stale > 0)
            {
                sb.Append("<a href=\"AdminQuotes?kind=quote&status=quoted\"><span>Quotes sent more than two weeks ago</span><b>" + stale + "</b></a>");
                nothing++;
            }
            if (outStock > 0)
            {
                sb.Append("<a href=\"AdminProducts?stock=out\"><span>Live products out of stock</span><b>" + outStock + "</b></a>");
                nothing++;
            }
            if (lowStock > 0)
            {
                sb.Append("<a href=\"AdminProducts?stock=low\"><span>Products low on stock</span><b>" + lowStock + "</b></a>");
                nothing++;
            }
            if (drafts > 0)
            {
                sb.Append("<a href=\"AdminProducts?status=draft\"><span>Draft products not on the website</span><b>" + drafts + "</b></a>");
                nothing++;
            }
            if (newApps > 0)
            {
                sb.Append("<a href=\"AdminCareers?tab=apps\"><span>New job applications</span><b>" + newApps + "</b></a>");
                nothing++;
            }
            if (nothing == 0)
            {
                sb.Append("<p class=\"db-none\">Nothing waiting. Nice.</p>");
            }
            sb.Append("</div></section>");
            sb.Append("<section class=\"db-panel\"><div class=\"db-ph\"><h2>Most viewed products</h2><a href=\"AdminProducts?sort=views\">All products</a></div>");
            foreach (Part p in site.Products.Where(Ui.Public).OrderByDescending(p => p.Views).Take(6))
            {
                sb.Append("<a class=\"db-row\" href=\"AdminProducts?id=" + Ui.Attr(p.Id) + "\">" + (p.Photos.Count > 0 ? "<img class=\"db-dth\" src=\"" + Ui.Attr(Ui.Thumb(Ui.Cover(site, p))) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<div><b>" + Ui.Enc(p.Name) + "</b><small>" + Ui.Enc(p.Sku) + " &middot; " + Ui.Num(p.Views) + " views &middot; " + Ui.Num(p.Compares) + " compares</small></div>" + Ui.Pill("db", Ui.Stocks, p.Stock) + "</a>");
            }
            sb.Append("</section><section class=\"db-panel\"><h2>Quote requests by category, last 90 days</h2><div class=\"db-bars\">");
            DateTime since = DateTime.UtcNow.AddDays(-90);
            var byCat = site.Inquiries.Where(x => x.Kind == "quote" && x.Created >= since).SelectMany(x => x.Items).Select(i => Ui.ProductOf(site, i.Product)?.Category ?? string.Empty).Where(k => k != string.Empty).GroupBy(k => k).Select(g => new { Key = g.Key, N = g.Count() }).OrderByDescending(g => g.N).ToList();
            int max = byCat.Count > 0 ? byCat.Max(g => g.N) : 1;
            if (byCat.Count == 0)
            {
                sb.Append("<p class=\"db-none\">No quote requests in the last 90 days.</p>");
            }
            foreach (var g in byCat)
            {
                sb.Append("<div class=\"db-bar2\"><span>" + Ui.Enc(Ui.CategoryName(g.Key)) + "</span><i style=\"width:" + Math.Max(4, g.N * 100 / max) + "%\"></i><b>" + g.N + "</b></div>");
            }
            sb.Append("</div></section></div>");
            return sb.ToString();
        }
    }
}
