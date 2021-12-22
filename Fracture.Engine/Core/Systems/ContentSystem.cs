using System;
using Microsoft.Xna.Framework.Content;
using Shattered.Content.Ui;

namespace Fracture.Engine.Core.Systems
{
    /// <summary>
    /// Interface for implementing content systems that provide interface for content related operations. This works a wrapper for content manager.
    /// </summary>
    public interface IContentSystem : IGameEngineSystem
    {
        /// <summary>
        /// Loads asset with given name and returns it to the caller. Throws if asset is not found.
        /// </summary>
        T Load<T>(string assetName);
    }

    /// <summary>
    /// Default implementation of <see cref="ContentSystem"/>.
    /// </summary>
    public sealed class ContentSystem : GameEngineSystem, IContentSystem
    {
        #region Fields
        private readonly ContentManager content;
        #endregion
        
        public ContentSystem(ContentManager content)
            => this.content = content ?? throw new ArgumentNullException(nameof(content));
        
        public T Load<T>(string assetName)
            => content.Load<T>(assetName);
    }
}