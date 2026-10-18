using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminActivity : WebPage
    {
        private const string AreaKey = "activity";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Activity");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--lv-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("lv-menu", Auth.Menu("lv", "AdminActivity", site));
            response.SetElementContents("lv-who", Auth.Who("lv", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminActivityJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminActivityJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("lv-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminActivityJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"lv-tr lv-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private const int Size = 20;

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            StringBuilder sb = new StringBuilder("<div class=\"lv-ptop\"><div><h1>Activity log</h1><p>Everything the admin and the website do, including sign-in attempts the server refused.</p></div></div><div class=\"lv-bar\">");
            sb.Append("<label>Who<select id=\"lv-whoq\" onchange=\"AdminActivityJs.list(1)\"><option value=\"\">Everyone</option><option value=\"admin\">Admin</option><option value=\"web\">Website</option></select></label>");
            sb.Append("<label>Search<input type=\"search\" id=\"lv-q\" list=\"lv-alist\" placeholder=\"Action or item\" maxlength=\"60\" oninput=\"AdminActivityJs.search()\"></label>" + Ui.DataList("lv-alist", (await Store.LoadActivity()).Select(a => a.Action).Distinct()) + "<label>Outcome<select id=\"lv-aok\" onchange=\"AdminActivityJs.list(1)\">" + Ui.Opts(new[] { ("", "All outcomes"), ("1", "Done"), ("0", "Refused") }, string.Empty) + "</select></label></div><div class=\"lv-bar\" id=\"lv-perbar\">" + Ui.PeriodBar("lv", "AdminActivityJs.per", "today", string.Empty, string.Empty) + "</div><div class=\"lv-tbl lv-lvtbl\" id=\"lv-list\"></div><div id=\"lv-pager\"></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminActivityJs.expired();");
                return response;
            }
            string whoq = GetDataValue("whoq");
            string aq = Ui.Clip(GetDataValue("aq"), 60);
            string ok = GetDataValue("aok");
            string per = GetDataValue("per") == string.Empty ? "today" : Per(GetDataValue("per"));
            SiteData site = await Store.LoadSite();
            (DateTime? from, DateTime? to) = Ui.Period(Store.Today(site), per, GetDataValue("pfrom"), GetDataValue("pto"));
            IEnumerable<Activity> q = (await Store.LoadActivity()).Where(a => Ui.InPeriod(Store.Local(site, a.When), from, to));
            if (whoq == "web")
            {
                q = q.Where(a => a.User == "web");
            }
            else if (whoq == "admin")
            {
                q = q.Where(a => a.User != "web");
            }
            if (aq != string.Empty)
            {
                q = q.Where(a => a.Action.Contains(aq, StringComparison.OrdinalIgnoreCase) || a.Target.Contains(aq, StringComparison.OrdinalIgnoreCase));
            }
            if (ok == "1" || ok == "0")
            {
                q = q.Where(a => a.Ok == (ok == "1"));
            }
            List<Activity> list = q.ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, list.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("When", string.Empty), ("Who", string.Empty), ("Action", string.Empty), ("Item", string.Empty), ("Result", string.Empty) }));
            if (list.Count == 0)
            {
                sb.Append("<div class=\"lv-tr\"><span class=\"lv-none lv-wide\">Nothing in this period.</span></div>");
            }
            foreach (Activity a in list.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<div class=\"lv-tr" + (a.Ok ? string.Empty : " lv-bad") + "\"><span data-l=\"When\">" + Store.Local(site, a.When).ToString("MMM d", Ui.Inv) + ", " + Ui.Hm(site, Store.Local(site, a.When)) + "</span><span data-l=\"Who\"><b>" + Ui.Enc(a.User == "web" ? "Website" : a.User == Store.AdminId ? c.U.Name : a.User) + "</b></span><span data-l=\"Action\">" + Ui.Enc(a.Action) + "</span><span data-l=\"Item\" class=\"lv-wide\" title=\"" + Ui.Attr(a.Target) + "\">" + Ui.Enc(a.Target) + "</span><span data-l=\"Result\">" + (a.Ok ? "<span class=\"lv-yes\">Done</span>" : "<span class=\"lv-no\">Refused</span>") + "</span></div>");
            }
            response.SetElementContents("lv-list", sb.ToString());
            response.SetElementContents("lv-pager", Ui.Pager("lv", page, Size, list.Count, "entry", "entries", "AdminActivityJs.list"));
            response.SetElementContents("lv-perbar", Ui.PeriodBar("lv", "AdminActivityJs.per", per, GetDataValue("pfrom"), GetDataValue("pto")));
            return response;
        }
    }
}
