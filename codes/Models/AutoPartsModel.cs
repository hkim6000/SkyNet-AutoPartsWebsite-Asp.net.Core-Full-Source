namespace AutoParts.Models
{
    public class SiteData
    {
        public DateTime Anchor { get; set; }
        public Settings Settings { get; set; } = new Settings();
        public List<Make> Makes { get; set; } = new List<Make>();
        public List<Part> Products { get; set; } = new List<Part>();
        public List<Dealer> Dealers { get; set; } = new List<Dealer>();
        public List<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
        public List<Post> Posts { get; set; } = new List<Post>();
        public List<Job> Jobs { get; set; } = new List<Job>();
        public List<MediaItem> Media { get; set; } = new List<MediaItem>();
        public Dictionary<string, PageDoc> Pages { get; set; } = new Dictionary<string, PageDoc>();
    }

    public class Settings
    {
        public string Name { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Town { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Legal { get; set; } = string.Empty;
        public string Accent { get; set; } = "#D9531E";
        public string Theme { get; set; } = "light";
        public string Units { get; set; } = "both";
        public int YearFrom { get; set; } = 2012;
        public int YearTo { get; set; } = 2027;
        public int Founded { get; set; } = 1988;
        public int WarrantyMonths { get; set; } = 24;
        public string WarrantyText { get; set; } = string.Empty;
        public string QuoteText { get; set; } = string.Empty;
        public string Certifications { get; set; } = string.Empty;
        public string ShipFrom { get; set; } = string.Empty;
        public bool Clock24 { get; set; }
        public string TimeZone { get; set; } = "America/New_York";
        public List<OpenDay> Hours { get; set; } = new List<OpenDay>();
    }

    public class OpenDay
    {
        public int Day { get; set; }
        public bool Open { get; set; }
        public string Start { get; set; } = "08:00";
        public string End { get; set; } = "17:00";
    }

    public class Make
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<VehicleModel> Models { get; set; } = new List<VehicleModel>();
    }

    public class VehicleModel
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = "sedan";
        public int From { get; set; }
        public int To { get; set; }
    }

    public class Part
    {
        public string Id { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "coilovers";
        public string Series { get; set; } = "street";
        public string Summary { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new List<string>();
        public List<SpecRow> Specs { get; set; } = new List<SpecRow>();
        public List<Fitment> Fitments { get; set; } = new List<Fitment>();
        public decimal Price { get; set; }
        public string Stock { get; set; } = "in";
        public int LeadDays { get; set; }
        public int WarrantyMonths { get; set; }
        public List<string> Photos { get; set; } = new List<string>();
        public List<string> Docs { get; set; } = new List<string>();
        public List<string> Box { get; set; } = new List<string>();
        public int InstallMinutes { get; set; }
        public string Torque { get; set; } = string.Empty;
        public bool Featured { get; set; }
        public string Status { get; set; } = "draft";
        public DateTime? Listed { get; set; }
        public DateTime Created { get; set; }
        public List<ProductEvent> History { get; set; } = new List<ProductEvent>();
        public int Views { get; set; }
        public int Compares { get; set; }
    }

    public class SpecRow
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    public class Fitment
    {
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int From { get; set; }
        public int To { get; set; }
        public string Position { get; set; } = "set";
        public string Note { get; set; } = string.Empty;
    }

    public class ProductEvent
    {
        public DateTime When { get; set; }
        public string Event { get; set; } = string.Empty;
        public string By { get; set; } = string.Empty;
    }

    public class Dealer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = "retail";
        public string Tier { get; set; } = "authorized";
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Web { get; set; } = string.Empty;
        public string Hours { get; set; } = string.Empty;
        public List<string> Services { get; set; } = new List<string>();
        public bool Active { get; set; } = true;
        public DateTime Since { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class Inquiry
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = "quote";
        public string Name { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Vehicle { get; set; } = string.Empty;
        public List<QuoteLine> Items { get; set; } = new List<QuoteLine>();
        public string Sku { get; set; } = string.Empty;
        public string DealerName { get; set; } = string.Empty;
        public DateTime? Purchased { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public decimal Quoted { get; set; }
        public DateTime Created { get; set; }
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    public class QuoteLine
    {
        public string Product { get; set; } = string.Empty;
        public int Qty { get; set; } = 1;
    }

    public class Note
    {
        public DateTime When { get; set; }
        public string By { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class Post
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Tag { get; set; } = "news";
        public DateTime Date { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public bool Published { get; set; } = true;
        public int Views { get; set; }
    }

    public class Job
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Dept { get; set; } = "production";
        public string Type { get; set; } = "full";
        public string Shift { get; set; } = string.Empty;
        public string Pay { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool Open { get; set; } = true;
        public DateTime Posted { get; set; }
        public List<JobApp> Apps { get; set; } = new List<JobApp>();
    }

    public class JobApp
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Cover { get; set; } = string.Empty;
        public string Resume { get; set; } = string.Empty;
        public string ResumeName { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public DateTime Created { get; set; }
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    public class AdminUser
    {
        public string Login { get; set; } = "admin";
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }

    public class Activity
    {
        public DateTime When { get; set; }
        public string User { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public bool Ok { get; set; } = true;
    }

    public class Session
    {
        public string Tok { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Exp { get; set; }
    }

    public class Subscriber
    {
        public string Email { get; set; } = string.Empty;
        public DateTime When { get; set; }
    }

    public class MediaItem
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = "photo";
        public string Title { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Full { get; set; } = string.Empty;
        public string Thumb { get; set; } = string.Empty;
        public int Seconds { get; set; }
        public int Pages { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long Bytes { get; set; }
        public string By { get; set; } = string.Empty;
        public DateTime When { get; set; }
    }

    public class PageDoc
    {
        public List<Block> Blocks { get; set; } = new List<Block>();
        public List<PageVersion> Versions { get; set; } = new List<PageVersion>();
        public DateTime Saved { get; set; }
        public string By { get; set; } = string.Empty;
    }

    public class PageVersion
    {
        public DateTime When { get; set; }
        public string By { get; set; } = string.Empty;
        public List<Block> Blocks { get; set; } = new List<Block>();
    }

    public class Block
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string Source { get; set; } = string.Empty;
        public bool Hidden { get; set; }
        public string Kicker { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Label2 { get; set; } = string.Empty;
        public string Link2 { get; set; } = string.Empty;
        public bool Flip { get; set; }
        public string Extra { get; set; } = string.Empty;
        public List<BlockItem> Items { get; set; } = new List<BlockItem>();
    }

    public class BlockItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
