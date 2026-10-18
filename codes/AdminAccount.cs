using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminAccount : WebPage
    {
        private const string AreaKey = "";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | My account");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--ac-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("ac-menu", Auth.Menu("ac", "AdminAccount", site));
            response.SetElementContents("ac-who", Auth.Who("ac", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminAccountJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminAccountJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            response.SetElementContents("ac-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminAccountJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"ac-tr ac-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            AdminUser u = c.U;
            bool sample = u.Login == Store.SampleUser && Access.Verify(Store.SamplePassword, u.Hash);
            StringBuilder sb = new StringBuilder("<div class=\"ac-ptop\"><div><h1>My account</h1><p>Signed in as <b>" + Ui.Enc(u.Login) + "</b>. This site has one admin account.</p></div></div>" + (sample ? "<div class=\"ac-warn-box\" style=\"margin-bottom:18px\">The sample password is still in use and is shown on the sign-in page. Change it before the site goes live.</div>" : string.Empty) + "<div class=\"ac-grid2\" style=\"margin-top:0\"><section class=\"ac-panel\"><h2>Profile</h2><form class=\"ac-form\" onsubmit=\"return AdminAccountJs.save()\" novalidate><div class=\"ac-f2\"><label class=\"ac-field\"><span>Name</span><input type=\"text\" id=\"ac-aname\" maxlength=\"60\" value=\"" + Ui.Attr(u.Name) + "\"></label><label class=\"ac-field\"><span>Job title</span><input type=\"text\" id=\"ac-atitle\" maxlength=\"60\" value=\"" + Ui.Attr(u.Title) + "\"></label></div><label class=\"ac-field\"><span>User name</span><input type=\"text\" id=\"ac-alogin\" maxlength=\"20\" autocapitalize=\"none\" value=\"" + Ui.Attr(u.Login) + "\"></label><h3>Change password</h3><label class=\"ac-field\"><span>Current password</span><input type=\"password\" id=\"ac-acur\" maxlength=\"100\" autocomplete=\"current-password\"></label><label class=\"ac-field\"><span>New password (optional)</span><input type=\"password\" id=\"ac-anew\" maxlength=\"100\" autocomplete=\"new-password\"></label><em class=\"ac-err\" id=\"ac-e-acc\"></em><div><button type=\"submit\" class=\"ac-btn ac-btn-accent\">Save</button></div></form></section><section class=\"ac-panel\"><h2>About this account</h2><p>The admin account manages everything: products, quotes, dealers, news, careers, pages, media and settings.</p><p style=\"margin-top:10px\">Changing the user name or the password signs out every other browser. If the password is lost, delete <code>data/admin.json</code> on the server and the sample sign-in comes back.</p></section></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminAccountJs.expired();");
                return response;
            }
            string name = Regex.Replace(Ui.Clip(GetDataValue("aname"), 60), "\\s+", " ").Trim();
            string title = Ui.Clip(GetDataValue("atitle"), 60).Trim();
            string login = Regex.Replace(Ui.Clip(GetDataValue("alogin"), 20).ToLowerInvariant(), "[^a-z0-9._-]", string.Empty);
            string cur = GetDataValue("acur");
            string nw = GetDataValue("anew");
            if (name.Length < 2)
            {
                response.SetElementContents("ac-e-acc", "Enter your name.");
                return response;
            }
            if (login.Length < 3)
            {
                response.SetElementContents("ac-e-acc", "Choose a user name of 3 characters or more.");
                return response;
            }
            AdminUser u = await Store.LoadAdmin();
            bool loginChanged = u.Login != login;
            if ((nw != string.Empty || loginChanged) && !Access.Verify(cur, u.Hash))
            {
                response.SetElementContents("ac-e-acc", "Enter your current password to change the user name or password.");
                return response;
            }
            if (nw != string.Empty && (nw.Length < 8 || nw.Length > 100))
            {
                response.SetElementContents("ac-e-acc", "Use 8 characters or more.");
                return response;
            }
            u.Name = name;
            u.Title = title;
            u.Login = login;
            if (nw != string.Empty)
            {
                u.Hash = Access.HashPassword(nw);
            }
            await Store.SaveAdmin(u);
            if (nw != string.Empty || loginChanged)
            {
                await Auth.CloseOthers(Store.CleanId(GetDataValue("tok")));
            }
            await Store.Log(Store.AdminId, "account", nw != string.Empty ? "password changed" : loginChanged ? "user name changed" : "profile", true);
            response.ExecuteScript("AdminAccountJs.toast('Saved');setTimeout(function(){location.reload();},700);");
            return response;
        }
    }
}
