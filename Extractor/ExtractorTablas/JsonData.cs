namespace ExtractorTablas
{
    public class JsonData
    {
        //public Dictionary<string, Box>? Boxes { get; set; }
        public List<ColorItem>? Colors { get; set; }
        public Dictionary<string, int> ColorPercentage { get; set; }
        public string FontColor { get; set; }

    }

    public class ColorData
    {
        public string? Name { get; set; }
        public int Combos { get; set; }
        public string? ComboPercentage { get; set; }
        public string? Color { get; set; }
    }

    public class Box
    {
        public List<ColorItem> Colors { get; set; }
        public Dictionary<string, int> ColorPercentage { get; set; }
        public string FontColor { get; set; }
    }

    public class ColorItem
    {
        public string Name { get; set; }
        public int Percentage { get; set; }
        public int Place { get; set; }
    }

}
