using System;
using System.Collections.Generic;

namespace Shattered.Content.Ui
{
    /// <summary>
    /// Static utility class that contains value keys for styles.
    /// </summary>
    public static class UiStyleKeys
    {
        /// <summary>
        /// Static utility class that contains style targets.
        /// </summary>
        public static class Target
        {
            #region Constant fields
            public const string Button      = "button";
            public const string Panel       = "panel";
            public const string Checkbox    = "checkbox";
            public const string TextInput   = "text-input";
            public const string Paragraph   = "paragraph";
            public const string ListBox     = "list-box";
            public const string Slider      = "slider";
            public const string ImageBox    = "image-box";
            public const string ListView    = "list-view";
            public const string HeaderPanel = "header-panel";
            #endregion
        }

        /// <summary>
        /// Static utility class that contains value keys for textures.
        /// </summary>
        public static class Texture
        {
            #region Constant fields
            public const string Enabled   = "texture-enabled";
            public const string Disabled  = "texture-disabled";
            public const string Focused   = "texture-focused";
            public const string Normal    = "texture-normal";
            public const string Hover     = "texture-hover";
            public const string Checked   = "texture-checked";
            public const string Unchecked = "texture-unchecked";

            public const string Arrow  = "texture-arrow";
            public const string Circle = "texture-circle";
            #endregion
        }

        public static class Source
        {
            #region Constant fields
            public const string Enabled  = "source-focus";
            public const string Disabled = "source-disabled";
            public const string Focused  = "source-focused";
            public const string Normal   = "source-normal";
            public const string Hover    = "source-hover";
            public const string Center   = "source-center";
            public const string Text     = "source-text";
            public const string Header   = "source-header";
            #endregion
        }

        /// <summary>
        /// Static utility class that contains value keys for colors.
        /// </summary>
        public static class Color
        {
            #region Constant fields
            public const string Enabled  = "color-enabled";
            public const string Disabled = "color-disabled";
            public const string Focused  = "color-focused";
            public const string Normal   = "color-normal";
            public const string Hover    = "color-hover";

            public const string ItemSelected = "color-item-selected";
            public const string ItemNormal   = "color-item-normal";
            public const string ItemHover    = "color-item-hover";
            #endregion
        }

        /// <summary>
        /// Static utility class that contains value keys for offsets.
        /// </summary>
        public static class Offset
        {
            #region Constant fields
            public const string Enabled  = "offset-enabled";
            public const string Disabled = "offset-disabled";
            public const string Focused  = "offset-focused";
            public const string Normal   = "offset-normal";
            public const string Hover    = "offset-hover";
            public const string Down     = "offset-down";
            public const string Click    = "offset-click";
            #endregion
        }

        /// <summary>
        /// Static utility class that contains value keys for fonts.
        /// </summary>
        public static class Font
        {
            #region Constant fields
            public const string Hover    = "font-hover";
            public const string Enabled  = "font-enabled";
            public const string Disabled = "font-disabled";

            public const string HeaderLarge  = "font-header-large";
            public const string HeaderNormal = "font-header-normal";
            public const string HeaderSmall  = "font-header-small";

            public const string Large  = "font-large";
            public const string Normal = "font-normal";
            public const string Small  = "font-small";
            #endregion
        }
    }

    /// <summary>
    /// Interface for implementing control styles.
    /// </summary>
    public interface IUiStyle
    {
        #region
        /// <summary>
        /// Name of the style.
        /// </summary>
        string Name
        {
            get;
        }
        #endregion

        /// <summary>
        /// Attempts to get value from the style
        /// value container.
        /// </summary>
        T Get<T>(string name);

        /// <summary>
        /// Writes given value to value container.
        /// </summary>
        void Set<T>(string name, T value);
    }

    public sealed class UiStyle : IUiStyle
    {
        #region Fields
        private readonly Dictionary<string, object> values;
        #endregion

        #region Properties
        public string Name
        {
            get;
        }
        #endregion

        public UiStyle(string name, Dictionary<string, object> values)
        {
            Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));

            this.values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public UiStyle(string name)
            : this(name, new Dictionary<string, object>())
        {
        }

        public T Get<T>(string name)
        {
            if (values.TryGetValue(name, out var value)) return (T)value;

            return default;
        }

        public void Set<T>(string name, T value)
            => values[name] = value;
    }
}