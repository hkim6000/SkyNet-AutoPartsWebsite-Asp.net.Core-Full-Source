using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminCareers : WebPage
    {
        private const string AreaKey = "careers";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Careers");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--cz-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("cz-menu", Auth.Menu("cz", "AdminCareers", site));
            response.SetElementContents("cz-who", Auth.Who("cz", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminCareersJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("cz-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminCareersJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"cz-tr cz-th\">");
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
            int newApps = site.Jobs.Sum(j => j.Apps.Count(a => a.Status == "new"));
            StringBuilder sb = new StringBuilder("<div class=\"cz-ptop\"><div><h1>Careers</h1><p>Open positions on the Careers page and the applications that come in.</p></div><div class=\"cz-acts\"><button type=\"button\" class=\"cz-btn cz-btn-accent\" onclick=\"AdminCareersJs.edit('')\">Add a position</button></div></div>");
            sb.Append("<div class=\"cz-tabs\" role=\"tablist\"><button type=\"button\" class=\"cz-tab cz-act\" data-p=\"jobs\" onclick=\"AdminCareersJs.show('jobs')\">Positions<em>" + site.Jobs.Count(j => j.Open) + " open</em></button><button type=\"button\" class=\"cz-tab\" data-p=\"apps\" onclick=\"AdminCareersJs.show('apps')\">Applications" + (newApps > 0 ? "<em>" + newApps + " new</em>" : string.Empty) + "</button></div>");
            sb.Append("<div id=\"cz-jobsp\"><div class=\"cz-bar\"><label>Search<input type=\"search\" id=\"cz-q\" placeholder=\"Title\" maxlength=\"40\" oninput=\"AdminCareersJs.search()\"></label><label>Department<select id=\"cz-fdept\" onchange=\"AdminCareersJs.list(1)\"><option value=\"\">Any department</option>" + Ui.Opts(Ui.Depts.Select(d => (d.Key, d.Name)), string.Empty) + "</select></label><label>Shown<select id=\"cz-fopen\" onchange=\"AdminCareersJs.list(1)\"><option value=\"\">Open and closed</option><option value=\"1\">Open</option><option value=\"0\">Closed</option></select></label></div><div class=\"cz-tbl cz-cztbl\" id=\"cz-list\"></div><div id=\"cz-pager\"></div></div>");
            sb.Append("<div id=\"cz-appsp\" hidden><div class=\"cz-bar\"><label>Search<input type=\"search\" id=\"cz-aq\" placeholder=\"Name or email\" maxlength=\"40\" oninput=\"AdminCareersJs.search()\"></label><label>Position<select id=\"cz-fjob\" onchange=\"AdminCareersJs.list(1)\"><option value=\"\">Any position</option>" + Ui.Opts(site.Jobs.OrderByDescending(j => j.Posted).Select(j => (j.Id, j.Title)), string.Empty) + "</select></label><label>Status<select id=\"cz-fastat\" onchange=\"AdminCareersJs.list(1)\"><option value=\"\">Active (not hired or declined)</option>" + Ui.Opts(Ui.AppStatuses.Select(s => (s.Key, s.Name)), string.Empty) + "<option value=\"all\">Everything</option></select></label></div><div class=\"cz-tbl cz-aptbl\" id=\"cz-alist\"></div><div id=\"cz-apager\"></div></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            string term = Ui.Clip(GetDataValue("jq"), 40);
            string dept = GetDataValue("jdept");
            string op = GetDataValue("jopen");
            List<Job> all = site.Jobs.Where(j => (dept == string.Empty || j.Dept == dept) && (op == string.Empty || j.Open == (op == "1")) && (term == string.Empty || j.Title.Contains(term, StringComparison.OrdinalIgnoreCase))).OrderByDescending(j => j.Open).ThenByDescending(j => j.Posted).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("Position", string.Empty), ("Department", string.Empty), ("Type", string.Empty), ("Posted", string.Empty), ("Applications", "cz-num"), ("Shown", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"cz-tr\"><span class=\"cz-none cz-wide\">No positions match.</span></div>");
            }
            foreach (Job j in all.Skip((page - 1) * Size).Take(Size))
            {
                int nw = j.Apps.Count(a => a.Status == "new");
                sb.Append("<div class=\"cz-tr\"><span class=\"cz-wide\"><b>" + Ui.Enc(j.Title) + "</b><small>" + Ui.Enc(j.Shift) + (j.Pay != string.Empty ? " &middot; " + Ui.Enc(j.Pay) : string.Empty) + "</small></span><span data-l=\"Department\">" + Ui.Enc(Ui.NameOf(Ui.Depts, j.Dept)) + "</span><span data-l=\"Type\">" + Ui.Enc(Ui.NameOf(Ui.JobTypes, j.Type)) + "</span><span data-l=\"Posted\">" + Ui.Day(j.Posted) + "</span><span data-l=\"Applications\" class=\"cz-num\">" + j.Apps.Count + (nw > 0 ? "<small>" + nw + " new</small>" : string.Empty) + "</span><span data-l=\"Shown\"><span class=\"cz-st cz-st-" + (j.Open ? "open\">Open" : "closed\">Closed") + "</span></span><span><button type=\"button\" class=\"cz-btn cz-btn-sm cz-btn-ghost\" onclick=\"AdminCareersJs.edit('" + Ui.Attr(j.Id) + "')\">Edit</button></span></div>");
            }
            response.SetElementContents("cz-list", sb.ToString());
            response.SetElementContents("cz-pager", Ui.Pager("cz", page, Size, all.Count, "position", "positions", "AdminCareersJs.list"));
            return response;
        }

        public async Task<ApiResponse> Apps()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            string term = Ui.Clip(GetDataValue("aq"), 40);
            string job = GetDataValue("ajob");
            string st = GetDataValue("astat");
            var all = site.Jobs.SelectMany(j => j.Apps.Select(a => new { J = j, A = a })).Where(x => (job == string.Empty || x.J.Id == job) && (st == "all" || (st == string.Empty ? x.A.Status is not ("hired" or "declined") : x.A.Status == st)) && (term == string.Empty || (x.A.Name + " " + x.A.Email).Contains(term, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x.A.Status == "new" ? 0 : 1).ThenByDescending(x => x.A.Created).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("When", string.Empty), ("Applicant", string.Empty), ("Position", string.Empty), ("Status", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"cz-tr\"><span class=\"cz-none cz-wide\">No applications match.</span></div>");
            }
            foreach (var x in all.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<div class=\"cz-tr\"><span data-l=\"When\">" + Store.Local(site, x.A.Created).ToString("MMM d", Ui.Inv) + "</span><span data-l=\"Applicant\" class=\"cz-wide\"><b>" + Ui.Enc(x.A.Name) + "</b><small>" + Ui.Enc(x.A.Email) + "</small></span><span data-l=\"Position\" class=\"cz-wide\">" + Ui.Enc(x.J.Title) + "</span><span data-l=\"Status\">" + Ui.Pill("cz", Ui.AppStatuses, x.A.Status) + "</span><span><button type=\"button\" class=\"cz-btn cz-btn-sm cz-btn-ghost\" onclick=\"AdminCareersJs.open('" + Ui.Attr(x.A.Id) + "')\">Open</button></span></div>");
            }
            response.SetElementContents("cz-alist", sb.ToString());
            response.SetElementContents("cz-apager", Ui.Pager("cz", page, Size, all.Count, "application", "applications", "AdminCareersJs.list"));
            return response;
        }

        private static string F(string id, string label, string value, int max, string type = "text")
        {
            return "<label class=\"cz-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"cz-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"></label>";
        }

        public async Task<ApiResponse> Form()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Job? found = site.Jobs.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("jid"), 20));
            Job j = found ?? new Job { Posted = Store.Today(site), Open = true, Shift = "Day shift" };
            StringBuilder sb = new StringBuilder("<h2>" + (found == null ? "Add a position" : Ui.Enc(j.Title)) + "</h2><div id=\"cz-jdata\" data-id=\"" + Ui.Attr(found?.Id ?? string.Empty) + "\" hidden></div><div class=\"cz-form\">");
            sb.Append("<div class=\"cz-f2\">" + F("jtitle", "Title", j.Title, 80) + F("jslug", "Web address (slug)", j.Slug, 60) + "</div><div class=\"cz-f4\"><label class=\"cz-field\"><span>Department</span><select id=\"cz-jdept\">" + Ui.Opts(Ui.Depts.Select(d => (d.Key, d.Name)), j.Dept) + "</select></label><label class=\"cz-field\"><span>Type</span><select id=\"cz-jtype\">" + Ui.Opts(Ui.JobTypes.Select(d => (d.Key, d.Name)), j.Type) + "</select></label>" + F("jshift", "Shift or hours", j.Shift, 80) + F("jpay", "Pay (shown on the website)", j.Pay, 60) + "</div>");
            sb.Append(F("jsum", "One-line summary", j.Summary, 160) + "<div><span class=\"cz-hint\">Description</span><div class=\"cz-rt\"><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminCareersJs.fmt('bold')\"><b>B</b></button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminCareersJs.fmt('italic')\"><i>I</i></button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminCareersJs.fmt('insertUnorderedList')\">&bull; List</button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminCareersJs.fmt('clear')\">Clear</button></div><div class=\"cz-ed\" id=\"cz-jbody\" contenteditable=\"true\">" + Blocks.Sanitize(j.Body, 20000) + "</div></div>");
            sb.Append("<div class=\"cz-f3\">" + F("jposted", "Posted", Ui.Ymd(j.Posted), 10, "date") + "<label class=\"cz-chk\" style=\"align-self:end;min-height:44px\"><input type=\"checkbox\" id=\"cz-jopen\"" + (j.Open ? " checked" : string.Empty) + "> Open: shown on the website and taking applications</label></div></div><em class=\"cz-err\" id=\"cz-e-save\" role=\"alert\"></em><div class=\"cz-mfoot\">");
            if (found != null)
            {
                sb.Append("<button type=\"button\" class=\"cz-btn cz-btn-danger cz-left\" onclick=\"AdminCareersJs.remove('" + Ui.Attr(found.Id) + "','" + Ui.Js(found.Title) + "')\">Delete</button><a class=\"cz-btn cz-btn-ghost\" href=\"Careers?job=" + Uri.EscapeDataString(found.Slug) + "\" target=\"_blank\" rel=\"noopener\">Open on the website</a>");
            }
            sb.Append("<button type=\"button\" class=\"cz-btn cz-btn-ghost\" onclick=\"AdminCareersJs.closeModal()\">Cancel</button><button type=\"button\" class=\"cz-btn cz-btn-accent\" onclick=\"AdminCareersJs.save()\">" + (found == null ? "Add position" : "Save") + "</button></div>");
            response.SetElementContents("cz-mbody", sb.ToString());
            response.ExecuteScript("AdminCareersJs.opened();");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("jid"), 20);
            string title = Regex.Replace(Ui.Clip(GetDataValue("jtitle"), 80), "\\s+", " ").Trim();
            string slug = Ui.Slug(GetDataValue("jslug") != string.Empty ? GetDataValue("jslug") : title);
            string body = Blocks.Sanitize(GetDataValue("jbody"), 20000);
            DateTime? posted = Ui.ParseDay(GetDataValue("jposted"));
            string err = title.Length < 3 ? "Enter the title." : slug.Length < 3 ? "The web address needs a few letters." : body == string.Empty ? "Write the description." : posted == null ? "Choose the posted date." : string.Empty;
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminCareersJs.fail('" + Ui.Js(err) + "');");
                return response;
            }
            bool isNew = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                if (site.Jobs.Any(x => x.Id != id && x.Slug == slug))
                {
                    response.ExecuteScript("AdminCareersJs.fail('Another position already uses that web address.');");
                    return response;
                }
                Job? j = site.Jobs.FirstOrDefault(x => x.Id == id);
                if (j == null)
                {
                    isNew = true;
                    j = new Job { Id = Store.NextId(site.Jobs.Select(x => x.Id), "J-", 0) };
                    site.Jobs.Add(j);
                }
                j.Title = title;
                j.Slug = slug;
                j.Dept = Ui.Has(Ui.Depts, GetDataValue("jdept2")) ? GetDataValue("jdept2") : j.Dept;
                j.Type = Ui.Has(Ui.JobTypes, GetDataValue("jtype")) ? GetDataValue("jtype") : j.Type;
                j.Shift = Ui.Clip(GetDataValue("jshift"), 80).Trim();
                j.Pay = Ui.Clip(GetDataValue("jpay"), 60).Trim();
                j.Summary = Ui.Clip(GetDataValue("jsum"), 160).Trim();
                j.Body = body;
                j.Posted = posted!.Value;
                j.Open = GetDataValue("jopen2") == "1";
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, isNew ? "add position" : "position", title, true);
            response.ExecuteScript("AdminCareersJs.saved(" + (isNew ? "true" : "false") + ");");
            return response;
        }

        private static void DeleteResume(string path)
        {
            if (path.StartsWith("data/files/resumes/", StringComparison.Ordinal) && !path.Contains(".."))
            {
                string full = Path.Combine(Store.Root, path.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full))
                {
                    File.Delete(full);
                }
            }
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("jid"), 20);
            string title = string.Empty;
            List<string> files = new List<string>();
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Job? j = site.Jobs.FirstOrDefault(x => x.Id == id);
                if (j == null)
                {
                    return response;
                }
                title = j.Title;
                files = j.Apps.Select(a => a.Resume).ToList();
                site.Jobs.Remove(j);
                await Store.SaveSite(site);
            }
            foreach (string f in files)
            {
                DeleteResume(f);
            }
            await Store.Log(Store.AdminId, "delete position", title, true);
            response.ExecuteScript("AdminCareersJs.deleted();");
            return response;
        }

        private static (Job? J, JobApp? A) FindApp(SiteData site, string id)
        {
            foreach (Job j in site.Jobs)
            {
                JobApp? a = j.Apps.FirstOrDefault(x => x.Id == id);
                if (a != null)
                {
                    return (j, a);
                }
            }
            return (null, null);
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            (Job? j, JobApp? a) = FindApp(site, Ui.Clip(GetDataValue("aid"), 20));
            if (j == null || a == null)
            {
                response.ExecuteScript("AdminCareersJs.toast('That application is no longer here.');");
                return response;
            }
            string subject = Uri.EscapeDataString("Your application for " + j.Title + " · " + site.Settings.Name);
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(a.Name) + " <small style=\"font-family:var(--cz-text);font-size:14px;color:var(--cz-ink2)\">" + Ui.Enc(a.Id) + "</small></h2><div class=\"cz-f2\"><div><h3>Applicant</h3><dl class=\"cz-dl\"><dt>Position</dt><dd>" + Ui.Enc(j.Title) + (j.Open ? string.Empty : " <span class=\"cz-st cz-st-closed\">Closed</span>") + "</dd><dt>Email</dt><dd><a href=\"mailto:" + Ui.Attr(a.Email) + "?subject=" + subject + "\">" + Ui.Enc(a.Email) + "</a></dd><dt>Phone</dt><dd>" + Ui.Enc(a.Phone) + "</dd><dt>Applied</dt><dd>" + Ui.Time(a.Created, "full") + "</dd><dt>Résumé</dt><dd>" + (a.Resume != string.Empty ? "<button type=\"button\" class=\"cz-btn cz-btn-sm cz-btn-ghost\" onclick=\"AdminCareersJs.resume()\">Download " + Ui.Enc(a.ResumeName) + "</button>" : "None") + "</dd></dl></div><div><h3>Status</h3><div class=\"cz-stbtns\">");
            foreach ((string k, string n) in Ui.AppStatuses)
            {
                sb.Append("<button type=\"button\" class=\"cz-btn cz-btn-sm " + (a.Status == k ? "cz-btn-accent" : "cz-btn-ghost") + "\" onclick=\"AdminCareersJs.status('" + k + "')\"" + (a.Status == k ? " disabled" : string.Empty) + ">" + Ui.Enc(n) + "</button>");
            }
            sb.Append("</div><div style=\"margin-top:12px\"><a class=\"cz-btn cz-btn-sm\" href=\"mailto:" + Ui.Attr(a.Email) + "?subject=" + subject + "\">Reply by email</a></div></div></div>");
            if (a.Cover != string.Empty)
            {
                sb.Append("<h3>In their words</h3><div class=\"cz-msg\">" + Ui.Enc(a.Cover) + "</div>");
            }
            sb.Append("<h3>Notes</h3><div class=\"cz-notes\">");
            if (a.Notes.Count == 0)
            {
                sb.Append("<p class=\"cz-hint\">No notes yet.</p>");
            }
            foreach (Note n in a.Notes.OrderByDescending(n => n.When))
            {
                sb.Append("<div class=\"cz-note\"><small>" + Ui.Time(n.When, "full") + " &middot; " + Ui.Enc(Ui.Who(c.U, n.By)) + "</small>" + Ui.Enc(n.Text) + "</div>");
            }
            sb.Append("</div><div class=\"cz-form\" style=\"margin-top:10px\"><label class=\"cz-field\"><span>Add a note</span><textarea id=\"cz-ntext\" rows=\"2\" maxlength=\"600\" placeholder=\"Phone screen Tuesday at 10\"></textarea></label><div><button type=\"button\" class=\"cz-btn cz-btn-sm cz-btn-ghost\" onclick=\"AdminCareersJs.note()\">Add note</button></div></div><div class=\"cz-mfoot\"><button type=\"button\" class=\"cz-btn cz-btn-danger cz-left\" onclick=\"AdminCareersJs.removeApp()\">Delete</button><button type=\"button\" class=\"cz-btn cz-btn-ghost\" onclick=\"AdminCareersJs.closeModal()\">Close</button></div>");
            response.SetElementContents("cz-mbody", sb.ToString());
            response.ExecuteScript("AdminCareersJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> Status()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("aid"), 20);
            string st = Ui.Has(Ui.AppStatuses, GetDataValue("ast")) ? GetDataValue("ast") : string.Empty;
            if (st == string.Empty)
            {
                return response;
            }
            string who = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                (Job? j, JobApp? a) = FindApp(site, id);
                if (j == null || a == null)
                {
                    return response;
                }
                a.Notes.Add(new Note { When = DateTime.UtcNow, By = Store.AdminId, Text = Ui.NameOf(Ui.AppStatuses, a.Status) + " → " + Ui.NameOf(Ui.AppStatuses, st) });
                a.Status = st;
                who = a.Name + " · " + j.Title + " · " + Ui.NameOf(Ui.AppStatuses, st);
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "application status", who, true);
            response.ExecuteScript("AdminCareersJs.reopen();AdminCareersJs.toast('Saved.');");
            return response;
        }

        public async Task<ApiResponse> Note()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("aid"), 20);
            string text = Ui.Clip(GetDataValue("ntext"), 600).Trim();
            if (text == string.Empty)
            {
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                (Job? j, JobApp? a) = FindApp(site, id);
                if (j == null || a == null)
                {
                    return response;
                }
                a.Notes.Add(new Note { When = DateTime.UtcNow, By = Store.AdminId, Text = text });
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "application note", id, true);
            response.ExecuteScript("AdminCareersJs.reopen();");
            return response;
        }

        public async Task<ApiResponse> Resume()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            (Job? j, JobApp? a) = FindApp(site, Ui.Clip(GetDataValue("aid"), 20));
            if (j == null || a == null || !a.Resume.StartsWith("data/files/resumes/", StringComparison.Ordinal) || a.Resume.Contains(".."))
            {
                response.ExecuteScript("AdminCareersJs.toast('No résumé on file.');");
                return response;
            }
            string full = Path.Combine(Store.Root, a.Resume.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full))
            {
                response.ExecuteScript("AdminCareersJs.toast('The résumé file is missing on the server.');");
                return response;
            }
            string ext = Path.GetExtension(full).ToLowerInvariant();
            string mime = ext == ".pdf" ? "application/pdf" : ext == ".docx" ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/msword";
            string name = Regex.Replace(a.ResumeName, "[^A-Za-z0-9._ -]", string.Empty);
            if (!name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                name = Path.GetFileNameWithoutExtension(name) + ext;
            }
            await Store.Log(Store.AdminId, "resume download", a.Id + " · " + a.Name, true);
            response.ExecuteScript("AdminCareersJs.fileReady('" + Convert.ToBase64String(await File.ReadAllBytesAsync(full)) + "','" + Ui.Js(name) + "','" + mime + "');");
            return response;
        }

        public async Task<ApiResponse> DeleteApp()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminCareersJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("aid"), 20);
            string file = string.Empty;
            string who = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                (Job? j, JobApp? a) = FindApp(site, id);
                if (j == null || a == null)
                {
                    return response;
                }
                file = a.Resume;
                who = a.Name + " · " + j.Title;
                j.Apps.Remove(a);
                await Store.SaveSite(site);
            }
            DeleteResume(file);
            await Store.Log(Store.AdminId, "delete application", who, true);
            response.ExecuteScript("AdminCareersJs.closeModal();AdminCareersJs.list();AdminCareersJs.toast('Deleted.');");
            return response;
        }
    }
}
