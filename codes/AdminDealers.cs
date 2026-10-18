using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminDealers : WebPage
    {
        private const string AreaKey = "dealers";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Dealers");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--dl-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("dl-menu", Auth.Menu("dl", "AdminDealers", site));
            response.SetElementContents("dl-who", Auth.Who("dl", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminDealersJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDealersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("dl-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminDealersJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"dl-tr dl-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private const int Size = 20;

        private static string StateChips(SiteData site, string cur)
        {
            StringBuilder sb = new StringBuilder("<div class=\"dl-stsum\"><button type=\"button\" class=\"" + (cur == string.Empty ? "dl-act" : string.Empty) + "\" onclick=\"AdminDealersJs.setState('')\">All<em>" + site.Dealers.Count + "</em></button>");
            foreach (var g in site.Dealers.GroupBy(d => d.State).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
            {
                sb.Append("<button type=\"button\" class=\"" + (cur == g.Key ? "dl-act" : string.Empty) + "\" onclick=\"AdminDealersJs.setState('" + Ui.Attr(g.Key) + "')\">" + Ui.Enc(g.Key) + "<em>" + g.Count() + "</em></button>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            StringBuilder sb = new StringBuilder("<div class=\"dl-ptop\"><div><h1>Dealers</h1><p>Shops, installers, distributors and online stores on the Dealers page. Only active dealers show on the website.</p></div><div class=\"dl-acts\"><button type=\"button\" class=\"dl-btn dl-btn-accent\" onclick=\"AdminDealersJs.edit('')\">Add a dealer</button></div></div><div id=\"dl-states\">" + StateChips(site, string.Empty) + "</div>");
            sb.Append("<div class=\"dl-bar\"><label>Search<input type=\"search\" id=\"dl-q\" list=\"dl-qlist\" placeholder=\"Name or city\" maxlength=\"40\" oninput=\"AdminDealersJs.search()\"></label>" + Ui.DataList("dl-qlist", site.Dealers.Select(d => d.Name).Concat(site.Dealers.Select(d => d.City))) + "<label>Type<select id=\"dl-fkind\" onchange=\"AdminDealersJs.list(1)\"><option value=\"\">Any type</option>" + Ui.Opts(Ui.DealerKinds.Select(k => (k.Key, k.Name)), string.Empty) + "</select></label><label>Shown<select id=\"dl-fact\" onchange=\"AdminDealersJs.list(1)\"><option value=\"\">Active and off</option><option value=\"1\">Active</option><option value=\"0\">Switched off</option></select></label></div><div class=\"dl-tbl dl-dltbl\" id=\"dl-list\"></div><div id=\"dl-pager\"></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDealersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            string term = Ui.Clip(GetDataValue("dq"), 40);
            string st = GetDataValue("dstate");
            string kind = GetDataValue("dkind");
            string act = GetDataValue("dact");
            List<Dealer> all = site.Dealers.Where(d => (st == string.Empty || d.State == st) && (kind == string.Empty || d.Kind == kind) && (act == string.Empty || d.Active == (act == "1")) && (term == string.Empty || (d.Name + " " + d.City).Contains(term, StringComparison.OrdinalIgnoreCase))).OrderBy(d => d.State).ThenByDescending(d => d.Tier == "premier").ThenBy(d => d.Name).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("Dealer", string.Empty), ("Type", string.Empty), ("Where", string.Empty), ("Tier", string.Empty), ("Shown", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"dl-tr\"><span class=\"dl-none dl-wide\">No dealers match.</span></div>");
            }
            foreach (Dealer d in all.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<div class=\"dl-tr\"><span class=\"dl-wide\"><b>" + Ui.Enc(d.Name) + "</b><small>" + Ui.Enc(d.Phone) + (d.Email != string.Empty ? " &middot; " + Ui.Enc(d.Email) : string.Empty) + "</small></span><span data-l=\"Type\">" + Ui.Enc(Ui.NameOf(Ui.DealerKinds, d.Kind)) + "</span><span data-l=\"Where\">" + Ui.Enc(d.City) + ", " + Ui.Enc(d.State) + "<small>" + Ui.Enc(string.Join(", ", d.Services.Select(s => Ui.NameOf(Ui.DealerServices, s)))) + "</small></span><span data-l=\"Tier\">" + Ui.Pill("dl", Ui.Tiers, d.Tier) + "</span><span data-l=\"Shown\"><span class=\"dl-st dl-st-" + (d.Active ? "live\">Active" : "off\">Off") + "</span></span><span><button type=\"button\" class=\"dl-btn dl-btn-sm dl-btn-ghost\" onclick=\"AdminDealersJs.edit('" + Ui.Attr(d.Id) + "')\">Edit</button></span></div>");
            }
            response.SetElementContents("dl-list", sb.ToString());
            response.SetElementContents("dl-pager", Ui.Pager("dl", page, Size, all.Count, "dealer", "dealers", "AdminDealersJs.list"));
            response.SetElementContents("dl-states", StateChips(site, st));
            return response;
        }

        private static string F(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"dl-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"dl-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private static string S(string id, string label, IEnumerable<(string, string)> opts, string cur)
        {
            return "<label class=\"dl-field\"><span>" + label + "</span><select id=\"dl-" + id + "\">" + Ui.Opts(opts, cur) + "</select></label>";
        }

        public async Task<ApiResponse> Form()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDealersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Dealer? found = site.Dealers.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("did"), 20));
            Dealer d = found ?? new Dealer { Name = Ui.Clip(GetDataValue("pname"), 80), State = Ui.Has(Ui.States, GetDataValue("pstate")) ? GetDataValue("pstate") : string.Empty, Kind = Ui.Has(Ui.DealerKinds, GetDataValue("pkind")) ? GetDataValue("pkind") : "installer", Email = Ui.Clip(GetDataValue("pemail"), 80), Phone = Ui.CleanPhone(GetDataValue("pphone")), Since = Store.Today(site), Hours = "Mon to Fri 8 to 6, Sat 9 to 2" };
            StringBuilder sb = new StringBuilder("<h2>" + (found == null ? "Add a dealer" : Ui.Enc(d.Name)) + "</h2><div id=\"dl-ddata\" data-id=\"" + Ui.Attr(found?.Id ?? string.Empty) + "\" hidden></div><div class=\"dl-form\">");
            sb.Append("<div class=\"dl-f3\">" + F("dname", "Name", d.Name, 80) + S("dkind", "Type", Ui.DealerKinds.Select(k => (k.Key, k.Name)), d.Kind) + S("dtier", "Tier", Ui.Tiers.Select(k => (k.Key, k.Name)), d.Tier) + "</div>");
            sb.Append("<div class=\"dl-f4\">" + F("daddr", "Street address", d.Address, 100) + F("dcity", "City", d.City, 40) + S("dst", "State", new[] { ("", "Choose") }.Concat(Ui.States.Select(s => (s.Key, s.Name))), d.State) + F("dzip", "ZIP", d.Zip, 10) + "</div>");
            sb.Append("<div class=\"dl-f3\">" + F("dphone", "Phone", d.Phone, 30, "tel") + F("demail", "Email", d.Email, 80, "email") + F("dweb", "Website (https://)", d.Web, 200, "url") + "</div>");
            sb.Append("<div class=\"dl-f2\">" + F("dhours", "Hours", d.Hours, 80) + F("dsince", "Dealer since", Ui.Ymd(d.Since == default ? Store.Today(site) : d.Since), 10, "date") + "</div>");
            sb.Append("<div><span class=\"dl-hint\">Services</span><div class=\"dl-checks\" id=\"dl-svcs\" style=\"margin-top:6px\">" + string.Concat(Ui.DealerServices.Select(s => "<label class=\"dl-chk\"><input type=\"checkbox\" value=\"" + s.Key + "\"" + (d.Services.Contains(s.Key) ? " checked" : string.Empty) + "> " + Ui.Enc(s.Name) + "</label>")) + "</div></div>");
            sb.Append("<label class=\"dl-field\"><span>Notes (only you see these)</span><textarea id=\"dl-dnotes\" rows=\"2\" maxlength=\"400\">" + Ui.Enc(d.Notes) + "</textarea></label><label class=\"dl-chk\"><input type=\"checkbox\" id=\"dl-dactive\"" + (d.Active ? " checked" : string.Empty) + "> Show on the website</label></div><em class=\"dl-err\" id=\"dl-e-save\" role=\"alert\"></em><div class=\"dl-mfoot\">");
            if (found != null)
            {
                sb.Append("<button type=\"button\" class=\"dl-btn dl-btn-danger dl-left\" onclick=\"AdminDealersJs.remove('" + Ui.Attr(found.Id) + "','" + Ui.Js(found.Name) + "')\">Delete</button>");
            }
            sb.Append("<button type=\"button\" class=\"dl-btn dl-btn-ghost\" onclick=\"AdminDealersJs.closeModal()\">Cancel</button><button type=\"button\" class=\"dl-btn dl-btn-accent\" onclick=\"AdminDealersJs.save()\">" + (found == null ? "Add dealer" : "Save") + "</button></div>");
            response.SetElementContents("dl-mbody", sb.ToString());
            response.ExecuteScript("AdminDealersJs.opened();");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDealersJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("did"), 20);
            string name = Regex.Replace(Ui.Clip(GetDataValue("dname"), 80), "\\s+", " ").Trim();
            string st = GetDataValue("dst");
            string kind = Ui.Has(Ui.DealerKinds, GetDataValue("dkind")) ? GetDataValue("dkind") : "installer";
            string email = Ui.Clip(GetDataValue("demail"), 80).Trim();
            string web = Ui.Clip(GetDataValue("dweb"), 200).Trim();
            string err = name.Length < 2 ? "Enter the dealer's name." : !Ui.Has(Ui.States, st) ? "Choose the state." : kind != "online" && Ui.Clip(GetDataValue("dcity"), 40) == string.Empty ? "Enter the city." : email != string.Empty && !Ui.Email(email) ? "Check the email address." : web != string.Empty && !Regex.IsMatch(web, "^https?://[^\\s<>\"']+$", RegexOptions.IgnoreCase) ? "The website must start with https://." : string.Empty;
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminDealersJs.fail('" + Ui.Js(err) + "');");
                return response;
            }
            bool isNew = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Dealer? d = site.Dealers.FirstOrDefault(x => x.Id == id);
                if (d == null)
                {
                    isNew = true;
                    d = new Dealer { Id = Store.NextId(site.Dealers.Select(x => x.Id), "DL-", 0) };
                    site.Dealers.Add(d);
                }
                d.Name = name;
                d.Kind = kind;
                d.Tier = Ui.Has(Ui.Tiers, GetDataValue("dtier")) ? GetDataValue("dtier") : "authorized";
                d.Address = Ui.Clip(GetDataValue("daddr"), 100).Trim();
                d.City = Ui.Clip(GetDataValue("dcity"), 40).Trim();
                d.State = st;
                d.Zip = Regex.Replace(Ui.Clip(GetDataValue("dzip"), 10), "[^0-9-]", string.Empty);
                d.Phone = Ui.CleanPhone(GetDataValue("dphone"));
                d.Email = email;
                d.Web = web;
                d.Hours = Ui.Clip(GetDataValue("dhours"), 80).Trim();
                d.Since = Ui.ParseDay(GetDataValue("dsince")) ?? d.Since;
                d.Services = GetDataValue("dsvc").Split(',').Where(s => Ui.Has(Ui.DealerServices, s)).Distinct().ToList();
                d.Notes = Ui.Clip(GetDataValue("dnotes"), 400).Trim();
                d.Active = GetDataValue("dactive") == "1";
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, isNew ? "add dealer" : "dealer", name, true);
            response.ExecuteScript("AdminDealersJs.saved(" + (isNew ? "true" : "false") + ");");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDealersJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("did"), 20);
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Dealer? d = site.Dealers.FirstOrDefault(x => x.Id == id);
                if (d == null)
                {
                    return response;
                }
                name = d.Name;
                site.Dealers.Remove(d);
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "delete dealer", name, true);
            response.ExecuteScript("AdminDealersJs.deleted();");
            return response;
        }
    }
}
