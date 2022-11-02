using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Content.Pipeline.Ui
{
    public sealed class UiStyleReader : ContentTypeReader<UiStyle>
    {
        public UiStyleReader()
        {
        }

        private static void InsertString(IUiStyle uiStyle, string target, string name, string value)
            => uiStyle.Set($"{target}\\{name}", value);

        private static bool InsertTexture(IUiStyle uiStyle, ContentReader input, string target, string name, string value)
        {
            if (!name.StartsWith("texture"))
                return false;

            uiStyle.Set($"{target}\\{name}", input.ContentManager.Load<Texture2D>(value));

            return true;
        }

        private static bool InsertFont(IUiStyle uiStyle, ContentReader input, string target, string name, string value)
        {
            if (!name.StartsWith("font"))
                return false;

            uiStyle.Set($"{target}\\{name}", input.ContentManager.Load<SpriteFont>(value));

            return true;
        }

        private static bool InsertOffset(IUiStyle uiStyle, string target, string name, string value)
        {
            if (!name.StartsWith("offset"))
                return false;

            if (!value.Contains("x") || !value.Contains("y"))
                return false;

            var tokens = value.Split(',')
                              .Select(s => s.Trim())
                              .ToArray();

            var x = float.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("x:"))?.Replace("x:", "").Trim() ?? "0.0", CultureInfo.InvariantCulture);
            var y = float.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("y:"))?.Replace("y:", "").Trim() ?? "0.0", CultureInfo.InvariantCulture);

            uiStyle.Set($"{target}\\{name}", new Vector2(x, y));

            return true;
        }

        private static bool InsertColor(IUiStyle uiStyle, string target, string name, string value)
        {
            if (!name.StartsWith("color"))
                return false;

            if (Regex.Match(value, "[A-Za-z]+").Length == value.Length)
            {
                var colors = typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var color  = colors.FirstOrDefault(p => p.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase))?.GetValue(null);

                if (color == null)
                    return false;

                uiStyle.Set($"{target}\\{name}", color);

                return true;
            }

            if (!value.Contains("r:") || !value.Contains("g:") || !value.Contains("b:") || !value.Contains("a:"))
                return false;

            var tokens = value.Split(',')
                              .Select(s => s.Trim())
                              .ToArray();

            var r = byte.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("r:"))?.Replace("r:", "").Trim() ?? "0");
            var g = byte.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("g:"))?.Replace("g:", "").Trim() ?? "0");
            var b = byte.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("b:"))?.Replace("b:", "").Trim() ?? "0");
            var a = byte.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("a:"))?.Replace("a:", "").Trim() ?? "0");

            uiStyle.Set($"{target}\\{name}", new Color(r, g, b, a));

            return true;
        }

        private static bool InsertSource(IUiStyle uiStyle, string target, string name, string value)
        {
            if (!name.StartsWith("source"))
                return false;

            if (!value.Contains("x:") || !value.Contains("y:") || !value.Contains("w:") || !value.Contains("h:"))
                return false;

            var tokens = value.Split(',')
                              .Select(s => s.Trim())
                              .ToArray();

            var x = int.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("x:"))?.Replace("x:", "").Trim() ?? "0");
            var y = int.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("y:"))?.Replace("y:", "").Trim() ?? "0");
            var w = int.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("w:"))?.Replace("w:", "").Trim() ?? "0");
            var h = int.Parse(tokens.FirstOrDefault(t => t.ToLower().StartsWith("h:"))?.Replace("h:", "").Trim() ?? "0");

            uiStyle.Set($"{target}\\{name}", new Rectangle(x, y, w, h));

            return true;
        }

        protected override UiStyle Read(ContentReader input, UiStyle existingInstance)
        {
            var length = input.ReadInt32();
            var bytes  = input.ReadBytes(length);

            using var ms = new MemoryStream(bytes);

            var document = XDocument.Load(ms);
            var style    = new UiStyle(document.Root!.Attribute("name")!.Value);

            foreach (var target in document.Root.Elements())
            {
                var targetName = target.Name.LocalName;

                foreach (var value in target.Elements())
                {
                    var valueName   = value.Name.LocalName;
                    var valueString = value.Attribute("value")!.Value;

                    if (InsertSource(style, targetName, valueName, valueString))
                        continue;

                    if (InsertColor(style, targetName, valueName, valueString))
                        continue;

                    if (InsertOffset(style, targetName, valueName, valueString))
                        continue;

                    if (InsertTexture(style, input, targetName, valueName, valueString))
                        continue;

                    if (InsertFont(style, input, targetName, valueName, valueString))
                        continue;

                    InsertString(style, targetName, valueName, valueString);
                }
            }

            return style;
        }
    }
}