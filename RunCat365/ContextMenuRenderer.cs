// Copyright 2020 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

namespace RunCat365
{
    internal class ContextMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text) && e.Item is CustomToolStripMenuItem item)
            {
                var textRectangle = e.TextRectangle;
                textRectangle.Height = item.Bounds.Height;
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Text, 
                    e.TextFont, 
                    textRectangle,
                    item.ForeColor, 
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
