namespace Kentico.Kontent.Delivery.Abstractions
{
    [DisableHtmlEncode]
    [UseDisplayTemplate("InlineImage")]
    internal class InlineImage : IInlineImage
    {
        public string AltText { get; set; }

        public string Src { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }


        public override string ToString()
        {
            return $"<figure><img src=\"{Src}\" alt=\"{AltText}\"></figure>";
        }
    }
}
