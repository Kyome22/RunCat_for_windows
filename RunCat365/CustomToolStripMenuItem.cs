namespace RunCat365
{
    internal class CustomToolStripMenuItem : ToolStripMenuItem
    {
        public CustomToolStripMenuItem(string? text) : base(text) { }

        public CustomToolStripMenuItem(string? text, Image? image, EventHandler? onClick) : base(text, image, onClick) { }

        public CustomToolStripMenuItem(string? text, Image? image, params ToolStripItem[]? dropDownItems) : base(text, image, dropDownItems) { }

        private readonly TextFormatFlags multiLineTextFlags =
            TextFormatFlags.LeftAndRightPadding |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.WordBreak |
            TextFormatFlags.TextBoxControl;

        private readonly TextFormatFlags singleLineTextFlags =
            TextFormatFlags.LeftAndRightPadding |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis;

        public override Size GetPreferredSize(Size constrainingSize)
        {
            Size baseSize = base.GetPreferredSize(constrainingSize);
            if (string.IsNullOrEmpty(Text) || !Text.Contains('\n'))
            {
                return new Size(baseSize.Width, 22);
                // return baseSize;
            }
            var textRenderWidth = Math.Max(constrainingSize.Width - 20, 1);

            SizeF measuredSize = TextRenderer.MeasureText(
                Text,
                Font,
                new Size(textRenderWidth, int.MaxValue),
                multiLineTextFlags
            );
            var calculatedHeight = (int)Math.Ceiling(measuredSize.Height);
            var height = Math.Max(baseSize.Height, calculatedHeight + 4);
            return new Size(baseSize.Width, height);
        }

        internal TextFormatFlags Flags()
        {
            if (string.IsNullOrEmpty(Text) || !Text.Contains('\n')) return singleLineTextFlags;
            return multiLineTextFlags;
        }
    }
}