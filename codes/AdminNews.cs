using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminNews : WebPage
    {
        private const string AreaKey = "news";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | News");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--nz-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("nz-menu", Auth.Menu("nz", "AdminNews", site));
            response.SetElementContents("nz-who", Auth.Who("nz", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminNewsJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminNewsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("nz-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminNewsJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"nz-tr nz-th\">");
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
            return "<button type=\"button\" class=\"nz-pki\" data-src=\"" + Ui.Attr(m.Full) + "\" data-thumb=\"" + Ui.Attr(m.Thumb) + "\" data-kind=\"" + m.Kind + "\" data-title=\"" + Ui.Attr(m.Title) + "\" onclick=\"AdminNewsJs.pkToggle(this)\" title=\"" + Ui.Attr(m.Title + (m.Tags.Count > 0 ? " · " + string.Join(", ", m.Tags) : string.Empty)) + "\"><img src=\"" + Ui.Attr(m.Thumb) + "\" alt=\"\" loading=\"lazy\">" + (vid ? "<b class=\"nz-vbadge\">" + (m.Seconds / 60) + ":" + (m.Seconds % 60).ToString("00", Ui.Inv) + "</b>" : doc ? "<b class=\"nz-dbadge\">PDF</b>" : string.Empty) + "<span>" + Ui.Enc(m.Title) + "</span><i>&#10003;</i></button>";
        }

        public async Task<ApiResponse> MediaPick()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminNewsJs.expired();");
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
                sb.Append("<p class=\"nz-none\" style=\"grid-column:1/-1\">Nothing matches. Upload a photo, or add videos and documents in Media.</p>");
            }
            string tags = page == 1 ? JsonSerializer.Serialize(site.Media.SelectMany(m => m.Tags).Distinct().OrderBy(t => t).ToList()) : "null";
            response.ExecuteScript("AdminNewsJs.pkShown(" + JsonSerializer.Serialize(sb.ToString()) + "," + (all.Count > page * size ? "true" : "false") + "," + tags + "," + (page > 1 ? "true" : "false") + ");");
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
                response.ExecuteScript("AdminNewsJs.expired();");
                return response;
            }
            byte[]? big = PickData(GetDataValue("full"), 8 * 1024 * 1024);
            byte[]? small = PickData(GetDataValue("thumb"), 3 * 1024 * 1024);
            if (big == null || small == null || PickExt(big) != PickExt(small))
            {
                response.ExecuteScript("AdminNewsJs.pkUploaded(false,'That file is not a JPG, PNG or WebP photo, or it is too large.');");
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
            response.ExecuteScript("AdminNewsJs.pkUploaded(true,'');");
            return response;
        }

        private const int Size = 20;

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            StringBuilder sb = new StringBuilder("<div class=\"nz-ptop\"><div><h1>News</h1><p>Posts on the News page and the three newest on the home page. Drafts stay here until you publish them.</p></div><div class=\"nz-acts\"><button type=\"button\" class=\"nz-btn nz-btn-accent\" onclick=\"AdminNewsJs.edit('')\">Write a post</button></div></div>");
            sb.Append("<div class=\"nz-bar\"><label>Search<input type=\"search\" id=\"nz-q\" placeholder=\"Title or text\" maxlength=\"40\" oninput=\"AdminNewsJs.search()\"></label><label>Tag<select id=\"nz-ftag\" onchange=\"AdminNewsJs.list(1)\"><option value=\"\">Any tag</option>" + Ui.Opts(Ui.PostTags.Select(t => (t.Key, t.Name)), string.Empty) + "</select></label><label>Shown<select id=\"nz-fpub\" onchange=\"AdminNewsJs.list(1)\"><option value=\"\">Published and drafts</option><option value=\"1\">Published</option><option value=\"0\">Drafts</option></select></label></div><div class=\"nz-tbl nz-nztbl\" id=\"nz-list\"></div><div id=\"nz-pager\"></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminNewsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            string term = Ui.Clip(GetDataValue("nq"), 40);
            string tag = GetDataValue("ntag");
            string pub = GetDataValue("npub");
            List<Post> all = site.Posts.Where(p => (tag == string.Empty || p.Tag == tag) && (pub == string.Empty || p.Published == (pub == "1")) && (term == string.Empty || (p.Title + " " + p.Summary + " " + p.Body).Contains(term, StringComparison.OrdinalIgnoreCase))).OrderByDescending(p => p.Date).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { (string.Empty, string.Empty), ("Post", string.Empty), ("Tag", string.Empty), ("Date", string.Empty), ("Views", "nz-num"), ("Shown", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"nz-tr\"><span class=\"nz-none nz-wide\">No posts match.</span></div>");
            }
            foreach (Post p in all.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<div class=\"nz-tr\"><span>" + (p.Image != string.Empty ? "<img class=\"nz-thumb\" src=\"" + Ui.Attr(Ui.Thumb(p.Image)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "</span><span class=\"nz-wide\"><b>" + Ui.Enc(p.Title) + "</b><small>" + Ui.Enc(p.Summary) + "</small></span><span data-l=\"Tag\">" + Ui.Enc(Ui.NameOf(Ui.PostTags, p.Tag)) + "</span><span data-l=\"Date\">" + Ui.Day(p.Date) + "</span><span data-l=\"Views\" class=\"nz-num\">" + Ui.Num(p.Views) + "</span><span data-l=\"Shown\"><span class=\"nz-st nz-st-" + (p.Published ? "live\">Published" : "draft\">Draft") + "</span></span><span><button type=\"button\" class=\"nz-btn nz-btn-sm nz-btn-ghost\" onclick=\"AdminNewsJs.edit('" + Ui.Attr(p.Id) + "')\">Edit</button></span></div>");
            }
            response.SetElementContents("nz-list", sb.ToString());
            response.SetElementContents("nz-pager", Ui.Pager("nz", page, Size, all.Count, "post", "posts", "AdminNewsJs.list"));
            return response;
        }

        public async Task<ApiResponse> Form()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminNewsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Post? found = site.Posts.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("nid"), 20));
            Post p = found ?? new Post { Date = Store.Today(site), Published = false, Tag = "plant" };
            StringBuilder sb = new StringBuilder("<h2>" + (found == null ? "Write a post" : Ui.Enc(p.Title)) + "</h2><div id=\"nz-ndata\" data-id=\"" + Ui.Attr(found?.Id ?? string.Empty) + "\" data-image=\"" + Ui.Attr(p.Image) + "\" hidden></div><div class=\"nz-form\">");
            sb.Append("<label class=\"nz-field\"><span>Title</span><input type=\"text\" id=\"nz-ntitle\" maxlength=\"120\" value=\"" + Ui.Attr(p.Title) + "\"></label><div class=\"nz-f3\"><label class=\"nz-field\"><span>Tag</span><select id=\"nz-ntag\">" + Ui.Opts(Ui.PostTags.Select(t => (t.Key, t.Name)), p.Tag) + "</select></label><label class=\"nz-field\"><span>Date</span><input type=\"date\" id=\"nz-ndate\" value=\"" + Ui.Ymd(p.Date) + "\"></label><label class=\"nz-field\"><span>Web address (slug)</span><input type=\"text\" id=\"nz-nslug\" maxlength=\"60\" value=\"" + Ui.Attr(p.Slug) + "\" placeholder=\"made from the title\"></label></div>");
            sb.Append("<label class=\"nz-field\"><span>Summary (cards and the top of the post)</span><input type=\"text\" id=\"nz-nsum\" maxlength=\"200\" value=\"" + Ui.Attr(p.Summary) + "\"></label>");
            sb.Append("<div class=\"nz-imgpick\"><button type=\"button\" class=\"nz-imgbox\" id=\"nz-nimg\" onclick=\"AdminNewsJs.pickImage()\">" + (p.Image != string.Empty ? "<img src=\"" + Ui.Attr(Ui.Thumb(p.Image)) + "\" alt=\"\">" : "<span>Add a picture</span>") + "</button><div><p class=\"nz-hint\">The picture shows on the card and at the top of the post. Pick one from Media or upload in the picker.</p><button type=\"button\" class=\"nz-btn nz-btn-sm nz-btn-ghost\" style=\"margin-top:8px\" onclick=\"AdminNewsJs.noImage()\">Remove picture</button></div></div>");
            sb.Append("<div><span class=\"nz-hint\">Body</span><div class=\"nz-rt\"><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminNewsJs.fmt('bold')\"><b>B</b></button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminNewsJs.fmt('italic')\"><i>I</i></button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminNewsJs.fmt('link')\">Link</button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminNewsJs.fmt('insertUnorderedList')\">&bull; List</button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminNewsJs.fmt('clear')\">Clear</button></div><div class=\"nz-ed\" id=\"nz-nbody\" contenteditable=\"true\">" + Blocks.Sanitize(p.Body, 20000) + "</div></div>");
            sb.Append("<label class=\"nz-chk\"><input type=\"checkbox\" id=\"nz-npub\"" + (p.Published ? " checked" : string.Empty) + "> Published on the website</label></div><em class=\"nz-err\" id=\"nz-e-save\" role=\"alert\"></em><div class=\"nz-mfoot\">");
            if (found != null)
            {
                sb.Append("<button type=\"button\" class=\"nz-btn nz-btn-danger nz-left\" onclick=\"AdminNewsJs.remove('" + Ui.Attr(found.Id) + "','" + Ui.Js(found.Title) + "')\">Delete</button>");
                if (found.Published)
                {
                    sb.Append("<a class=\"nz-btn nz-btn-ghost\" href=\"News?post=" + Uri.EscapeDataString(found.Slug) + "\" target=\"_blank\" rel=\"noopener\">Open on the website</a>");
                }
            }
            sb.Append("<button type=\"button\" class=\"nz-btn nz-btn-ghost\" onclick=\"AdminNewsJs.closeModal()\">Cancel</button><button type=\"button\" class=\"nz-btn nz-btn-accent\" onclick=\"AdminNewsJs.save()\">" + (found == null ? "Add post" : "Save") + "</button></div>");
            response.SetElementContents("nz-mbody", sb.ToString());
            response.ExecuteScript("AdminNewsJs.opened();");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminNewsJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("nid"), 20);
            string title = Regex.Replace(Ui.Clip(GetDataValue("ntitle"), 120), "\\s+", " ").Trim();
            string slug = Ui.Slug(GetDataValue("nslug") != string.Empty ? GetDataValue("nslug") : title);
            DateTime? date = Ui.ParseDay(GetDataValue("ndate"));
            string body = Blocks.Sanitize(GetDataValue("nbody"), 20000);
            string image = GetDataValue("nimage");
            string err = title.Length < 3 ? "Enter a title." : slug.Length < 3 ? "The web address needs a few letters." : date == null ? "Choose a date." : body == string.Empty ? "Write the post." : image != string.Empty && !Ui.SafeImage(image) ? "Choose the picture again." : string.Empty;
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminNewsJs.fail('" + Ui.Js(err) + "');");
                return response;
            }
            bool isNew = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                if (site.Posts.Any(p => p.Id != id && p.Slug == slug))
                {
                    response.ExecuteScript("AdminNewsJs.fail('Another post already uses that web address.');");
                    return response;
                }
                Post? p = site.Posts.FirstOrDefault(x => x.Id == id);
                if (p == null)
                {
                    isNew = true;
                    p = new Post { Id = Store.NextId(site.Posts.Select(x => x.Id), "N-", 0) };
                    site.Posts.Add(p);
                }
                p.Title = title;
                p.Slug = slug;
                p.Tag = Ui.Has(Ui.PostTags, GetDataValue("ntag2")) ? GetDataValue("ntag2") : p.Tag;
                p.Date = date!.Value;
                p.Summary = Ui.Clip(GetDataValue("nsum"), 200).Trim();
                p.Body = body;
                p.Image = image;
                p.Published = GetDataValue("npub2") == "1";
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, isNew ? "add post" : "post", title, true);
            response.ExecuteScript("AdminNewsJs.saved(" + (isNew ? "true" : "false") + ");");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminNewsJs.expired();");
                return response;
            }
            string id = Ui.Clip(GetDataValue("nid"), 20);
            string title = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Post? p = site.Posts.FirstOrDefault(x => x.Id == id);
                if (p == null)
                {
                    return response;
                }
                title = p.Title;
                site.Posts.Remove(p);
                await Store.SaveSite(site);
            }
            await Store.Log(Store.AdminId, "delete post", title, true);
            response.ExecuteScript("AdminNewsJs.deleted();");
            return response;
        }
    }
}
