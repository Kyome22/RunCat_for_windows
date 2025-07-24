namespace RunCat365
{
    internal class ContextMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text) && e.Item is CustomToolStripMenuItem item)
            {
                var textRectangle = e.TextRectangle;
                textRectangle.Height = e.Item.Bounds.Height;
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Text, 
                    e.TextFont, 
                    textRectangle,
                    e.Item.ForeColor, 
                    item.Flags()
                );
            }
            else
            {
                base.OnRenderItemText(e);
            }
        }
    }
}
