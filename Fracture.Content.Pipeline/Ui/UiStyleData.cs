using System;

namespace Fracture.Content.Pipeline.Ui
{
    public sealed class UiStyleData
    {
        #region Properties
        public string Contents
        {
            get;
        }
        #endregion

        public UiStyleData(string contents)
            => Contents = !string.IsNullOrEmpty(contents) ? contents : throw new ArgumentNullException(nameof(contents));
    }
}
