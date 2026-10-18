using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutoParts.Core;
using AutoParts.Models;
using SkyNet;

namespace AutoParts.codes
{
    public class AdminLogin : WebPage
    {
        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Sign in");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            AdminUser admin = await Store.LoadAdmin();
            bool fill = admin.Login == Store.SampleUser && Access.Verify(Store.SamplePassword, admin.Hash);
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--lg-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_brand}", Ui.Enc(site.Settings.Name))
                .Replace("{plhd_user}", fill ? Store.SampleUser : string.Empty)
                .Replace("{plhd_pass}", fill ? Store.SamplePassword : string.Empty)
                .Replace("{plhd_hint}", fill ? "<p class=\"lg-lhint\">Sample sign-in: <b>" + Store.SampleUser + "</b> / <b>" + Store.SamplePassword + "</b>. Change the password in My account and this hint goes away.</p>" : string.Empty);
        }

        public async Task<ApiResponse> SignIn()
        {
            ApiResponse response = new ApiResponse();
            string login = Ui.Clip(GetDataValue("user"), 40).Trim().ToLowerInvariant();
            string pass = Ui.Clip(GetDataValue("pass"), 200);
            if (login == string.Empty || pass == string.Empty)
            {
                response.ExecuteScript("AdminLoginJs.fail('Enter your user name and password.');");
                return response;
            }
            if (Auth.Locked(login))
            {
                response.ExecuteScript("AdminLoginJs.fail('Too many attempts. Please wait a few minutes.');");
                return response;
            }
            AdminUser admin = await Store.LoadAdmin();
            if (admin.Login != login || !Access.Verify(pass, admin.Hash))
            {
                Auth.Fail(login);
                await Store.Log(login, "signin", login, false);
                response.ExecuteScript("AdminLoginJs.fail('Wrong user name or password.');");
                return response;
            }
            string tok = await Auth.Open();
            await Store.Log(Store.AdminId, "signin", admin.Login, true);
            response.ExecuteScript("AdminLoginJs.signedIn('" + tok + "'," + (8 * 3600) + ");");
            return response;
        }
    }
}
