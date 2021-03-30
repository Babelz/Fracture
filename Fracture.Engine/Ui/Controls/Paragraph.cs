using System;
using System.Collections.Generic;
using System.Text;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Paragraph that serves as text display, label and header. Can span multiple
    /// lines and can be clipped to prevent leaking from parent.
    /// </summary>
    public sealed class Paragraph : Control
    {
        #region Fields
        private readonly StringBuilder wrapBuffer;
        private readonly StringBuilder lineBuffer;

        private readonly List<string> lines;
        
        private string text;

        private bool keepInTextBounds;

        private bool wrapAround;
        private bool wrap;

        private string fontName;
        #endregion

        #region Properties
        /// <summary>
        /// Actual text of the paragraph.
        /// </summary>
        public string Text
        {
            get => text;
            set
            {
                text = value;

                if (wrap)
                    WrapText();

                if (keepInTextBounds)
                    BoundToText();

                UpdateLayout();
            }
        }

        /// <summary>
        /// Should overflowing text be clipped. Meaningless if 
        /// wrap is used.
        /// </summary>
        public bool Clip
        {
            get;
            set;
        }

        /// <summary>
        /// Should size of the control be determined from the
        /// actual size of the text.
        /// </summary>
        public bool KeepInTextBounds
        {
            get => keepInTextBounds;
            set
            {
                keepInTextBounds = value;

                if (keepInTextBounds)
                    BoundToText();

                UpdateLayout();
            }
        }

        /// <summary>
        /// Should the text be wrapped to span multiple lines.
        /// </summary>
        public bool Wrap
        {
            get => wrap;
            set
            {
                wrap = value;

                if (wrap)
                    WrapText();

                UpdateLayout();
            }
        }

        /// <summary>
        /// Wraps the text around the controls center.
        /// </summary>
        public bool WrapAround
        {
            get => wrapAround;
            set
            {
                wrapAround = value;
                wrap       = wrapAround;

                if (wrap)
                    WrapText();

                UpdateLayout();
            }
        }

        /// <summary>
        /// What font should be used with this paragraph. Always looks
        /// for the fonts in paragraphs style properties, use style keys
        /// to change the font.
        /// </summary>
        public string Font
        {
            get => fontName;
            set
            {
                fontName = $"{UiStyleKeys.Target.Paragraph}\\{value}";

                if (keepInTextBounds)
                    BoundToText();

                if (wrap)
                    WrapText();

                UpdateLayout();
            }
        }

        /// <summary>
        /// Gets or sets the style of this paragraph. If wrap
        /// is used, changing this property causes the text
        /// to be wrapped.
        /// </summary>
        public override IUiStyle Style
        {
            get => base.Style;
            set
            {
                var old = base.Style;

                base.Style = value;

                if (Style != old && wrap)
                    WrapText();
            }
        }
        
        /// <summary>
        /// Override color for enabled texts.
        /// </summary>
        public Color? Color
        {
            get;
            set;
        }
        #endregion

        public Paragraph()
        {
            text = string.Empty;

            lines      = new List<string>();
            wrapBuffer = new StringBuilder();
            lineBuffer = new StringBuilder();

            Font = UiStyleKeys.Font.Normal;
            Size = new Vector2(0.5f, 0.25f);
            
            StyleChanged += Paragraph_StyleChanged;
        }

        #region Event handlers
        private void Paragraph_StyleChanged(object sender, EventArgs e)
        {
            if (Style == null) return;

            if (keepInTextBounds)
                BoundToText();

            if (wrap)
                WrapText();
        } 
        #endregion

        private void WrapText()
        {
            if (string.IsNullOrEmpty(text)) return;
            if (Style == null)              return;

            lineBuffer.Clear();
            wrapBuffer.Clear();

            lines.Clear();

            var results = WrapText(Style.Get<SpriteFont>(Font), text, GetRenderDestinationRectangle());

            foreach (var line in results.Split('\n'))
                lines.Add(line);
        }

        private string WrapText(SpriteFont font, string input, Rectangle destination)
        {
            var words      = input.Split(' ');
            var lineWidth  = 0.0f;
            var spaceWidth = font.MeasureString(" ").X;

            if (WrapAround)
            {
                var destinationCenter = destination.Width / 2;
                
                foreach (var word in words)
                {
                    var oldContents = lineBuffer.ToString();

                    lineBuffer.Append(word);

                    var newContents = lineBuffer.ToString();

                    var size   = (int)font.MeasureString(newContents).X;
                    var center = size / 2;
                    
                    if (destinationCenter + center > destination.Width || destinationCenter - center < 0)
                    {
                        wrapBuffer.Append(oldContents);
                        wrapBuffer.Append("\n");
                        
                        lineBuffer.Clear();

                        lineBuffer.Append(word);
                    }

                    lineBuffer.Append(" ");
                }

                if (lineBuffer.Length != 0)
                    wrapBuffer.Append(lineBuffer);
            }
            else
            {
                foreach (var word in words)
                {
                    var size = font.MeasureString(word);

                    if (lineWidth + size.X < destination.Width)
                    {
                        wrapBuffer.Append($"{word} ");

                        lineWidth += size.X + spaceWidth;
                    }
                    else
                    {
                        if (size.X > destination.Width)
                        {
                            wrapBuffer.Append(string.IsNullOrEmpty(wrapBuffer.ToString())
                                ? WrapText(font, word.Insert(word.Length / 2, " ") + " ", destination)
                                : $"\n{WrapText(font, word.Insert(word.Length / 2, " "), destination)} ");
                        }
                        else
                        {
                            wrapBuffer.Append($"\n{word} ");

                            lineWidth = size.X + spaceWidth;
                        }
                    }
                }
            }

            return wrapBuffer.ToString();
        }
        private void BoundToText()
        {
            if (Style == null) return;
            
            var font = Style.Get<SpriteFont>(Font);

            if (string.IsNullOrEmpty(text) || font == null)
                ActualSize = Vector2.Zero;
            else
                ActualSize = UiCanvas.ToLocalUnits(font.MeasureString(text));
        }
        
        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            var font = Style.Get<SpriteFont>(Font);

            if (font == null) return;

            var destination = GetRenderDestinationRectangle();
            var color       = Style.Get<Color>($"{UiStyleKeys.Target.Paragraph}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");

            if (Enabled && Color != null)
                color = Color.Value;

            if (wrap)
            {
                var linePosition = (float)destination.Y;

                foreach (var line in lines)
                {
                    var size = font.MeasureString(line);

                    if (linePosition + size.Y > destination.Bottom) break;

                    var position = new Vector2(destination.X, linePosition);

                    if (wrapAround)
                    {
                        var space = destination.Width - size.X;

                        position.X += (float)Math.Floor(space * 0.5f);
                    }
                    
                    fragment.DrawSpriteText(position,
                                            Vector2.One,
                                            0.0f,
                                            Vector2.Zero, 
                                            line,
                                            font,
                                            color);
                    
                    linePosition += size.Y;
                }
            }
            else
            {
                string displayText;

                if (Clip)
                {
                    var i  = 0;
                    var sb = new StringBuilder();
                    
                    while (true)
                    {
                        var token = text.Substring(i++, 1);

                        if (font.MeasureString(sb.ToString()).X + font.MeasureString(token).X >= destination.Width)
                            break;

                        sb.Append(token);
                    }

                    displayText = sb.ToString();
                }
                else
                    displayText = text;
                
                fragment.DrawSpriteText(new Vector2(destination.X, destination.Y),
                                        Vector2.One,
                                        0.0f,
                                        Vector2.Zero, 
                                        displayText,
                                        font,
                                        color);
            }
        }

        public override void UpdateLayout()
        {
            base.UpdateLayout();

            if (wrap)
                WrapText();

            if (keepInTextBounds)
                BoundToText();
        }
    }
}
