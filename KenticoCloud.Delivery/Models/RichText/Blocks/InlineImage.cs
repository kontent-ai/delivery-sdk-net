namespace KenticoCloud.Delivery
{
    [DisableHtmlEncode]
    [UseDisplayTemplate("InlineImage")]
    internal class InlineImage : IInlineImage
    {
        public string AltText { get; set; }

        public string Src { get; set; }


        public override string ToString()
        {
            return $"<figure><img src=\"{Src}\" alt=\"{AltText}\"></figure>";
        }
    }
}
