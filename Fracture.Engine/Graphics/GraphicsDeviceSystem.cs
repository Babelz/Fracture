using System.IO;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine.Graphics
{
    public readonly struct BackbufferSizeChangedEventArgs
    {
        #region Properties
        public int NewWidth
        {
            get;
        }

        public int NewHeight
        {
            get;
        }
        
        public int OldWidth
        {
            get;
        }

        public int OldHeight
        {
            get;
        }
        #endregion

        public BackbufferSizeChangedEventArgs(int newWidth, int newHeight, int oldWidth, int oldHeight)
        {
            NewWidth  = newWidth;
            NewHeight = newHeight;
            OldWidth  = oldWidth;
            OldHeight = oldHeight;
        }
    }

    public readonly struct ViewportChangedEventArgs
    {
        #region Properties
        public Viewport NewViewport
        {
            get;
        }

        public Viewport OldViewport
        {
            get;
        }
        #endregion

        public ViewportChangedEventArgs(in Viewport newViewport, in Viewport oldViewport)
        {
            NewViewport = newViewport;
            OldViewport = oldViewport;
        }
    }
    
    /// <summary>
    /// Interface for implementing graphics device systems that provide device free interface for working with graphics devices. Works as wrapper for the
    /// actual graphics device. This is s core system of the engine.
    /// </summary>
    public interface IGraphicsDeviceSystem : IGameEngineSystem
    {
        #region Properties
        public IWindow Window
        {
            get;
        }

        int BackBufferWidth
        {
            get;
            set;
        }

        int BackBufferHeight
        {
            get;
            set;
        }

        bool PreferMultiSampling
        {
            get;
            set;
        }

        bool IsFullScreen
        {
            get;
            set;
        }

        int MultiSampleCount
        {
            get;
            set;
        }

        Viewport Viewport
        {
            get;
            set;
        }
        #endregion

        void SetRenderTarget(RenderTarget2D renderTarget);
        void Clear(Color color);

        Texture2D CreateTexture2D(byte[] data);

        RenderTarget2D CreateRenderTarget2D(int width,
                                            int height,
                                            int samples,
                                            bool mipmap,
                                            SurfaceFormat surfaceFormat,
                                            DepthFormat depthFormat,
                                            RenderTargetUsage usage);

        SpriteBatch CreateSpriteBatch(int capacity = 64);
    }

    /// <summary>
    /// Default implementation of <see cref="IGraphicsDeviceSystem"/>.
    /// </summary>
    public sealed class GraphicsDeviceSystem : GameEngineSystem, IGraphicsDeviceSystem
    {
        #region Fields
        private readonly GraphicsDeviceManager manager;
        #endregion

        #region Properties
        public IWindow Window
        {
            get;
        }

        public int BackBufferWidth
        {
            get => manager.PreferredBackBufferWidth;
            set
            {
                manager.PreferredBackBufferWidth = value;

                manager.ApplyChanges();
            }
        }

        public int BackBufferHeight
        {
            get => manager.PreferredBackBufferHeight;
            set
            {
                manager.PreferredBackBufferHeight = value;

                manager.ApplyChanges();
            }
        }

        public int MultiSampleCount
        {
            get => manager.GraphicsDevice.PresentationParameters.MultiSampleCount;
            set
            {
                manager.GraphicsDevice.PresentationParameters.MultiSampleCount = value;

                manager.ApplyChanges();
            }
        }

        public bool PreferMultiSampling
        {
            get => manager.PreferMultiSampling;
            set
            {
                manager.PreferMultiSampling = value;

                manager.ApplyChanges();
            }
        }

        public bool IsFullScreen
        {
            get => manager.IsFullScreen;
            set
            {
                manager.IsFullScreen = value;

                manager.ApplyChanges();
            }
        }

        public Viewport Viewport
        {
            get => manager.GraphicsDevice.Viewport;
            set
            {
                manager.GraphicsDevice.Viewport = value;

                manager.ApplyChanges();
            }
        }
        #endregion

        [BindingConstructor]
        public GraphicsDeviceSystem(GraphicsDeviceManager manager, GameWindow window, IEventQueueSystem events)
        {
            this.manager = manager;

            Window = new Window(window);
        }

        public void SetRenderTarget(RenderTarget2D renderTarget)
            => manager.GraphicsDevice.SetRenderTarget(renderTarget);

        public void Clear(Color color)
            => manager.GraphicsDevice.Clear(color);

        public Texture2D CreateTexture2D(byte[] data)
        {
            using var ms = new MemoryStream(data);

            return Texture2D.FromStream(manager.GraphicsDevice, ms);
        }

        public SpriteBatch CreateSpriteBatch(int capacity)
            => new SpriteBatch(manager.GraphicsDevice, capacity);

        public RenderTarget2D CreateRenderTarget2D(int width,
                                                   int height,
                                                   int samples,
                                                   bool mipmap,
                                                   SurfaceFormat surfaceFormat,
                                                   DepthFormat depthFormat,
                                                   RenderTargetUsage usage)
            => new RenderTarget2D(manager.GraphicsDevice,
                                  width,
                                  height,
                                  mipmap,
                                  surfaceFormat,
                                  depthFormat,
                                  samples,
                                  usage);
    }
}