using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoParts.Models;

namespace AutoParts.Core
{
    public sealed class PageSpec
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Href { get; init; } = string.Empty;
        public string[] Data { get; init; } = Array.Empty<string>();
    }

    public sealed class PartSpec
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Desc { get; init; } = string.Empty;
        public string Fields { get; init; } = string.Empty;
        public string ItemFields { get; init; } = string.Empty;
    }

    public static class Blocks
    {
        public static readonly PageSpec[] Pages =
        {
            new PageSpec { Key = "home", Name = "Home", Href = "Home", Data = new[] { "featured", "families", "news", "quoteband" } },
            new PageSpec { Key = "capabilities", Name = "Capabilities", Href = "Capabilities", Data = new[] { "certs" } },
            new PageSpec { Key = "industries", Name = "Industries", Href = "Industries", Data = Array.Empty<string>() },
            new PageSpec { Key = "dealers", Name = "Dealers", Href = "Dealers", Data = new[] { "dealerfinder", "dealerform" } },
            new PageSpec { Key = "news", Name = "News", Href = "News", Data = new[] { "newslist" } },
            new PageSpec { Key = "careers", Name = "Careers", Href = "Careers", Data = new[] { "joblist" } },
            new PageSpec { Key = "about", Name = "About", Href = "About", Data = Array.Empty<string>() },
            new PageSpec { Key = "contact", Name = "Contact", Href = "Contact", Data = new[] { "contact" } }
        };

        public static readonly PartSpec[] Types =
        {
            new PartSpec { Key = "hero", Name = "Page top", Desc = "Big title, text, buttons and a picture or video.", Fields = "kicker,title,text,media,buttons,extra" },
            new PartSpec { Key = "text", Name = "Text", Desc = "A title and paragraphs.", Fields = "kicker,title,text" },
            new PartSpec { Key = "split", Name = "Picture and text", Desc = "A picture or video beside the text.", Fields = "kicker,title,text,media,button,flip,extra" },
            new PartSpec { Key = "cards", Name = "Cards", Desc = "A row of cards with an icon, title, text and link.", Fields = "kicker,title,text,items", ItemFields = "icon,title,text,link" },
            new PartSpec { Key = "steps", Name = "Steps", Desc = "Numbered steps, for a process or a timeline.", Fields = "kicker,title,text,items", ItemFields = "title,text" },
            new PartSpec { Key = "stats", Name = "Numbers", Desc = "A row of big numbers with a label under each.", Fields = "kicker,title,items", ItemFields = "title,text" },
            new PartSpec { Key = "boxes", Name = "Checklists", Desc = "Boxes with a title and one tick per line.", Fields = "kicker,title,items", ItemFields = "title,lines" },
            new PartSpec { Key = "people", Name = "People", Desc = "Portraits with a name and a role, for a team.", Fields = "kicker,title,text,items", ItemFields = "media,title,text" },
            new PartSpec { Key = "faq", Name = "Questions", Desc = "Questions that open to show the answer.", Fields = "kicker,title,items", ItemFields = "title,text" },
            new PartSpec { Key = "quote", Name = "Quote", Desc = "A quote with a name and a small photo.", Fields = "quote,name,media" },
            new PartSpec { Key = "media", Name = "Picture or video", Desc = "One wide picture or video with a caption.", Fields = "media,caption" },
            new PartSpec { Key = "cta", Name = "Call to action", Desc = "A colored band with a title, text and buttons.", Fields = "kicker,title,text,buttons" }
        };

        public static readonly PartSpec[] Parts =
        {
            new PartSpec { Key = "featured", Name = "Featured products", Desc = "Up to four products marked Featured in Products, topped up with the newest live products.", Fields = "kicker,title" },
            new PartSpec { Key = "families", Name = "Product families", Desc = "Every category with a short line and the number of live products in it.", Fields = "kicker,title,text" },
            new PartSpec { Key = "news", Name = "Latest news", Desc = "The three newest published posts. Hidden when there are none.", Fields = "kicker,title" },
            new PartSpec { Key = "quoteband", Name = "Quote band", Desc = "A band with the office hours, phone number and a button to the quote form.", Fields = "kicker,title,text,label" },
            new PartSpec { Key = "certs", Name = "Certifications", Desc = "The certifications and quality lines from Settings.", Fields = "kicker,title,text" },
            new PartSpec { Key = "dealerfinder", Name = "Dealer finder", Desc = "The state map, filters and the list of active dealers.", Fields = "title,text" },
            new PartSpec { Key = "dealerform", Name = "Dealer inquiry form", Desc = "The form for shops that want to carry the brand.", Fields = "kicker,title,text" },
            new PartSpec { Key = "newslist", Name = "News list", Desc = "Every published post with tag filters and paging.", Fields = "title" },
            new PartSpec { Key = "joblist", Name = "Open positions", Desc = "Every open position with department filters. Each opens with its application form.", Fields = "title,text" },
            new PartSpec { Key = "contact", Name = "Contact details and forms", Desc = "Address, office hours and the quote, message and warranty forms.", Fields = "kicker,title,text" }
        };

        public static readonly (string Key, string Name)[] Icons = { ("gear", "Gear"), ("wrench", "Wrench"), ("shield", "Shield"), ("truck", "Truck"), ("factory", "Factory"), ("flask", "Test lab"), ("bolt", "Bolt"), ("ruler", "Ruler"), ("check", "Tick"), ("pin", "Map pin"), ("clock", "Clock"), ("phone", "Phone"), ("mail", "Email"), ("doc", "Document"), ("star", "Star"), ("globe", "Globe") };

        public static readonly (string Key, string Name)[] HeroExtras = { ("", "Nothing else"), ("finder", "Vehicle fitment finder") };

        public static readonly (string Key, string Name)[] SplitExtras = { ("", "Nothing else"), ("counts", "Products, dealers and years in business") };

        private static readonly string[] Allowed = { "p", "br", "b", "i", "ul", "ol", "li", "a" };

        public static PageSpec? PageOf(string key)
        {
            return Pages.FirstOrDefault(p => p.Key == key);
        }

        public static PartSpec? Spec(Block b)
        {
            return b.Type == "data" ? Parts.FirstOrDefault(p => p.Key == b.Source) : Types.FirstOrDefault(t => t.Key == b.Type);
        }

        public static bool Has(PartSpec s, string field)
        {
            return ("," + s.Fields + ",").Contains("," + field + ",", StringComparison.Ordinal);
        }

        public static string SafeLink(string? value)
        {
            string v = (value ?? string.Empty).Trim();
            if (v.Length == 0 || v.Length > 300)
            {
                return string.Empty;
            }
            if (Regex.IsMatch(v, "^https?://[^\\s<>\"']+$", RegexOptions.IgnoreCase) || Regex.IsMatch(v, "^mailto:[^\\s<>\"']+$", RegexOptions.IgnoreCase) || Regex.IsMatch(v, "^tel:[0-9+() .-]+$", RegexOptions.IgnoreCase) || Regex.IsMatch(v, "^#[A-Za-z0-9_-]+$") || Regex.IsMatch(v, "^[A-Za-z][A-Za-z0-9_-]*(\\?[A-Za-z0-9=&%._+-]*)?(#[A-Za-z0-9_-]+)?$"))
            {
                return v;
            }
            return string.Empty;
        }

        public static string Sanitize(string? html, int max)
        {
            string src = Regex.Replace(html ?? string.Empty, "<(script|style|template|iframe|object)[\\s\\S]*?</\\1\\s*>", string.Empty, RegexOptions.IgnoreCase);
            src = Regex.Replace(src, "<!--[\\s\\S]*?-->", string.Empty);
            StringBuilder sb = new StringBuilder();
            List<string> open = new List<string>();
            foreach (Match m in Regex.Matches(src, "<[^>]*>|[^<]+|<"))
            {
                string t = m.Value;
                if (!t.StartsWith('<') || t == "<")
                {
                    sb.Append(Ui.Enc(WebUtility.HtmlDecode(t)));
                    continue;
                }
                Match tag = Regex.Match(t, "^<\\s*(/?)\\s*([a-zA-Z][a-zA-Z0-9]*)([^>]*)>$");
                if (!tag.Success)
                {
                    continue;
                }
                bool close = tag.Groups[1].Value == "/";
                string name = tag.Groups[2].Value.ToLowerInvariant() switch
                {
                    "strong" => "b",
                    "em" => "i",
                    "div" => "p",
                    "h1" or "h2" or "h3" or "h4" or "h5" or "h6" => "p",
                    string n => n
                };
                if (!Allowed.Contains(name))
                {
                    continue;
                }
                if (name == "br")
                {
                    if (!close)
                    {
                        sb.Append("<br>");
                    }
                    continue;
                }
                if (close)
                {
                    int at = open.LastIndexOf(name);
                    if (at < 0)
                    {
                        continue;
                    }
                    for (int i = open.Count - 1; i >= at; i--)
                    {
                        sb.Append("</" + open[i] + ">");
                    }
                    open.RemoveRange(at, open.Count - at);
                    continue;
                }
                if (name == "p" && open.Contains("p"))
                {
                    int at = open.LastIndexOf("p");
                    for (int i = open.Count - 1; i >= at; i--)
                    {
                        sb.Append("</" + open[i] + ">");
                    }
                    open.RemoveRange(at, open.Count - at);
                }
                if (name == "a")
                {
                    Match h = Regex.Match(tag.Groups[3].Value, "href\\s*=\\s*(\"([^\"]*)\"|'([^']*)'|([^\\s>]+))", RegexOptions.IgnoreCase);
                    string href = h.Success ? SafeLink(WebUtility.HtmlDecode(h.Groups[2].Value + h.Groups[3].Value + h.Groups[4].Value)) : string.Empty;
                    if (href == string.Empty || open.Contains("a"))
                    {
                        continue;
                    }
                    bool outside = href.StartsWith("http", StringComparison.OrdinalIgnoreCase);
                    sb.Append("<a href=\"" + Ui.Attr(href) + "\"" + (outside ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">");
                    open.Add("a");
                    continue;
                }
                sb.Append("<" + name + ">");
                open.Add(name);
            }
            for (int i = open.Count - 1; i >= 0; i--)
            {
                sb.Append("</" + open[i] + ">");
            }
            string outHtml = Regex.Replace(sb.ToString(), "<(p|b|i|li|ul|ol)>(\\s|<br>)*</\\1>", string.Empty);
            outHtml = Regex.Replace(outHtml, "(<br>\\s*){3,}", "<br><br>").Trim();
            return outHtml.Length > max ? string.Empty : outHtml;
        }

        public static string Plain(string? value, int max)
        {
            string v = Regex.Replace((value ?? string.Empty).Replace("\r", string.Empty), "[ \\t]+", " ");
            v = Regex.Replace(v, "\\n{3,}", "\n\n").Trim();
            return v.Length > max ? v.Substring(0, max).Trim() : v;
        }

        public static string NewId()
        {
            return "b" + Guid.NewGuid().ToString("N").Substring(0, 9);
        }

        public static (List<Block>? Blocks, string Error) Clean(SiteData site, PageSpec page, string json)
        {
            List<Block>? raw;
            try
            {
                raw = JsonSerializer.Deserialize<List<Block>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return (null, "The page could not be read. Reload and try again.");
            }
            if (raw == null || raw.Count == 0 || raw.Count > 40)
            {
                return (null, "A page needs between 1 and 40 blocks.");
            }
            HashSet<string> media = site.Media.Where(m => m.Kind != "doc").Select(m => m.Full).ToHashSet(StringComparer.Ordinal);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            List<Block> list = new List<Block>();
            foreach (Block r in raw)
            {
                string type = r.Type ?? string.Empty;
                PartSpec? spec = type == "data" ? Parts.FirstOrDefault(p => p.Key == r.Source && page.Data.Contains(p.Key)) : Types.FirstOrDefault(t => t.Key == type);
                if (spec == null)
                {
                    return (null, "That page has a block it cannot use. Reload and try again.");
                }
                if (type == "data" && list.Any(x => x.Type == "data" && x.Source == spec.Key))
                {
                    return (null, spec.Name + " is on the page twice.");
                }
                string id = Regex.IsMatch(r.Id ?? string.Empty, "^b[a-z0-9]{3,12}$") && !ids.Contains(r.Id!) ? r.Id! : NewId();
                ids.Add(id);
                Block b = new Block { Id = id, Type = type, Source = type == "data" ? spec.Key : string.Empty, Hidden = r.Hidden };
                b.Kicker = Has(spec, "kicker") ? Plain(r.Kicker, 80) : string.Empty;
                b.Title = Has(spec, "title") || Has(spec, "name") ? Plain(r.Title, 140).Replace("\n", " ") : string.Empty;
                if (Has(spec, "text"))
                {
                    b.Text = Sanitize(r.Text, 6000);
                    if (b.Text == string.Empty && Regex.Replace(r.Text ?? string.Empty, "<[^>]*>|\\s|&nbsp;", string.Empty).Length > 0)
                    {
                        return (null, "The text in " + spec.Name + " is too long.");
                    }
                }
                else if (Has(spec, "quote") || Has(spec, "caption"))
                {
                    b.Text = Plain(r.Text, Has(spec, "quote") ? 600 : 240);
                }
                if (Has(spec, "media"))
                {
                    string m = (r.Media ?? string.Empty).Trim();
                    if (m != string.Empty && !media.Contains(m))
                    {
                        return (null, "A picture in " + spec.Name + " is no longer in Media. Choose it again.");
                    }
                    b.Media = m;
                }
                if (Has(spec, "buttons") || Has(spec, "button"))
                {
                    b.Label = Plain(r.Label, 40).Replace("\n", " ");
                    b.Link = SafeLink(r.Link);
                    if (b.Label != string.Empty && b.Link == string.Empty)
                    {
                        return (null, "Check the link of the button “" + b.Label + "”. Use a page name like Products, #quote or a full https:// address.");
                    }
                    if (Has(spec, "buttons"))
                    {
                        b.Label2 = Plain(r.Label2, 40).Replace("\n", " ");
                        b.Link2 = SafeLink(r.Link2);
                        if (b.Label2 != string.Empty && b.Link2 == string.Empty)
                        {
                            return (null, "Check the link of the button “" + b.Label2 + "”.");
                        }
                    }
                }
                if (Has(spec, "label"))
                {
                    b.Label = Plain(r.Label, 40).Replace("\n", " ");
                }
                b.Flip = Has(spec, "flip") && r.Flip;
                if (Has(spec, "extra"))
                {
                    (string Key, string Name)[] ex = type == "hero" ? HeroExtras : SplitExtras;
                    b.Extra = ex.Any(x => x.Key == r.Extra) ? r.Extra : string.Empty;
                }
                if (Has(spec, "items"))
                {
                    foreach (BlockItem it in (r.Items ?? new List<BlockItem>()).Take(12))
                    {
                        BlockItem c = new BlockItem { Title = Plain(it.Title, 120).Replace("\n", " "), Text = Plain(it.Text, 600) };
                        if (spec.ItemFields.Contains("icon"))
                        {
                            c.Icon = Icons.Any(x => x.Key == it.Icon) ? it.Icon : string.Empty;
                        }
                        if (spec.ItemFields.Contains("media"))
                        {
                            string m = (it.Media ?? string.Empty).Trim();
                            if (m != string.Empty && !media.Contains(m))
                            {
                                return (null, "A photo in " + spec.Name + " is no longer in Media. Choose it again.");
                            }
                            c.Media = m;
                        }
                        if (spec.ItemFields.Contains("link"))
                        {
                            c.Link = SafeLink(it.Link);
                            c.Label = c.Link != string.Empty ? Plain(it.Label, 40).Replace("\n", " ") : string.Empty;
                            if ((it.Link ?? string.Empty).Trim() != string.Empty && c.Link == string.Empty)
                            {
                                return (null, "Check the link on the card “" + c.Title + "”.");
                            }
                        }
                        if (c.Title != string.Empty || c.Text != string.Empty || c.Media != string.Empty)
                        {
                            b.Items.Add(c);
                        }
                    }
                }
                list.Add(b);
            }
            string? missing = page.Data.FirstOrDefault(k => !list.Any(x => x.Type == "data" && x.Source == k));
            if (missing != null)
            {
                return (null, (Parts.FirstOrDefault(p => p.Key == missing)?.Name ?? missing) + " must stay on the page. Hide it instead.");
            }
            return (list, string.Empty);
        }

        public static void Ensure(SiteData site)
        {
            foreach (PageSpec p in Pages)
            {
                if (!site.Pages.TryGetValue(p.Key, out PageDoc? doc) || doc == null)
                {
                    doc = new PageDoc();
                    site.Pages[p.Key] = doc;
                }
                foreach (string k in p.Data)
                {
                    if (!doc.Blocks.Any(b => b.Type == "data" && b.Source == k))
                    {
                        doc.Blocks.Add(new Block { Id = NewId(), Type = "data", Source = k, Title = Parts.First(x => x.Key == k).Name });
                    }
                }
            }
        }

        public static List<string> Uses(SiteData site, string src)
        {
            List<string> u = new List<string>();
            if (src == string.Empty)
            {
                return u;
            }
            foreach (Part p in site.Products.Where(p => p.Photos.Contains(src) || p.Docs.Contains(src)))
            {
                u.Add("Product " + p.Sku);
            }
            foreach (Post p in site.Posts.Where(p => p.Image == src))
            {
                u.Add("News " + p.Title);
            }
            foreach (PageSpec p in Pages)
            {
                if (site.Pages.TryGetValue(p.Key, out PageDoc? doc) && doc.Blocks.Any(b => b.Media == src || b.Items.Any(i => i.Media == src)))
                {
                    u.Add("Page " + p.Name);
                }
            }
            return u;
        }
    }
}
