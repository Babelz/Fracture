using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine.Graphics
{
    public sealed class MissingTexture2D : IDisposable
    {
        #region Fields
        private bool disposed;
        #endregion

        #region Properties
        public Texture2D Value
        {
            get;
        }
        #endregion

        public MissingTexture2D(IGraphicsDeviceSystem graphics)
        {
            var data = new byte[]
            {
                137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0,
                13, 73, 72, 68, 82, 0, 0, 0, 64, 0,
                0, 0, 64, 8, 2, 0, 0, 0, 37, 11,
                230, 137, 0, 0, 0, 4, 103, 65, 77, 65,
                0, 0, 177, 143, 11, 252, 97, 5, 0, 0,
                0, 7, 116, 73, 77, 69, 7, 226, 10, 6,
                18, 27, 42, 149, 172, 34, 6, 0, 0, 0,
                23, 116, 69, 88, 116, 83, 111, 102, 116, 119,
                97, 114, 101, 0, 71, 76, 68, 80, 78, 71,
                32, 118, 101, 114, 32, 51, 46, 52, 113, 133,
                164, 225, 0, 0, 1, 252, 73, 68, 65, 84,
                104, 67, 205, 206, 33, 14, 4, 49, 16, 196,
                192, 252, 255, 211, 57, 98, 100, 226, 14, 88,
                233, 90, 166, 165, 153, 115, 207, 125, 234, 60,
                78, 60, 131, 237, 147, 207, 96, 243, 196, 51,
                216, 62, 249, 12, 54, 79, 60, 131, 237, 147,
                207, 96, 243, 196, 51, 216, 62, 249, 12, 54,
                79, 60, 131, 237, 147, 207, 96, 243, 196, 51,
                216, 62, 249, 12, 54, 79, 60, 131, 237, 147,
                207, 96, 243, 196, 51, 216, 62, 249, 12, 54,
                79, 60, 131, 237, 147, 207, 96, 243, 196, 51,
                216, 62, 249, 12, 54, 79, 60, 131, 237, 147,
                207, 96, 243, 196, 51, 216, 62, 249, 12, 54,
                79, 60, 131, 237, 147, 207, 96, 243, 196, 51,
                216, 62, 249, 12, 54, 79, 60, 131, 237, 147,
                207, 96, 243, 196, 51, 216, 119, 211, 189, 12,
                54, 79, 60, 131, 237, 147, 207, 96, 243, 196,
                51, 216, 62, 249, 12, 54, 79, 60, 131, 237,
                147, 207, 96, 243, 196, 51, 216, 62, 249, 12,
                54, 79, 60, 131, 237, 147, 207, 96, 243, 196,
                51, 216, 62, 249, 12, 54, 79, 60, 131, 237,
                147, 207, 96, 243, 196, 51, 216, 62, 249, 12,
                54, 79, 60, 131, 237, 147, 207, 96, 243, 196,
                51, 216, 62, 249, 12, 54, 79, 60, 131, 237,
                147, 207, 96, 243, 196, 51, 216, 62, 249, 12,
                54, 79, 60, 131, 237, 147, 207, 96, 243, 196,
                51, 216, 62, 249, 12, 54, 79, 60, 131, 237,
                147, 207, 96, 243, 196, 179, 255, 123, 232, 117,
                242, 25, 108, 158, 120, 6, 219, 39, 159, 193,
                230, 137, 103, 176, 125, 242, 25, 108, 158, 120,
                6, 219, 39, 159, 193, 230, 137, 103, 176, 125,
                242, 25, 108, 158, 120, 6, 219, 39, 159, 193,
                230, 137, 103, 176, 125, 242, 25, 108, 158, 120,
                6, 219, 39, 159, 193, 230, 137, 103, 176, 125,
                242, 25, 108, 158, 120, 6, 219, 39, 159, 193,
                230, 137, 103, 176, 125, 242, 25, 108, 158, 120,
                6, 219, 39, 159, 193, 230, 137, 103, 176, 125,
                242, 25, 108, 158, 120, 6, 219, 39, 159, 193,
                230, 137, 103, 176, 125, 242, 25, 108, 158, 120,
                6, 251, 110, 186, 151, 193, 230, 137, 103, 176,
                125, 242, 25, 108, 158, 120, 6, 219, 39, 159,
                193, 230, 137, 103, 176, 125, 242, 25, 108, 158,
                120, 6, 219, 39, 159, 193, 230, 137, 103, 176,
                125, 242, 25, 108, 158, 120, 6, 219, 39, 159,
                193, 230, 137, 103, 176, 125, 242, 25, 108, 158,
                120, 6, 219, 39, 159, 193, 230, 137, 103, 176,
                125, 242, 25, 108, 158, 120, 6, 219, 39, 159,
                193, 230, 137, 103, 176, 125, 242, 25, 108, 158,
                120, 6, 219, 39, 159, 193, 230, 137, 103, 176,
                125, 242, 25, 108, 158, 120, 6, 219, 39, 159,
                193, 230, 137, 103, 176, 125, 242, 25, 108, 158,
                120, 116, 238, 15, 184, 236, 240, 226, 172, 82,
                196, 233, 0, 0, 0, 0, 73, 69, 78, 68,
                174, 66, 96, 130
            };

            Value = graphics.CreateTexture2D(data);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                Value.Dispose();

            disposed = true;
        }

        public void Dispose()
            => Dispose(true);

        public static implicit operator Texture2D(MissingTexture2D missingTexture2D)
            => missingTexture2D.Value;
    }

    public sealed class EmptyTexture2D : IDisposable
    {
        #region Fields
        private bool disposed;
        #endregion

        #region Properties
        public Texture2D Value
        {
            get;
        }
        #endregion

        public EmptyTexture2D(IGraphicsDeviceSystem graphics)
        {
            var data = new byte[]
            {
                137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0,
                13, 73, 72, 68, 82, 0, 0, 0, 1, 0,
                0, 0, 1, 8, 2, 0, 0, 0, 144, 119,
                83, 222, 0, 0, 0, 4, 103, 65, 77, 65,
                0, 0, 177, 143, 11, 252, 97, 5, 0, 0,
                0, 7, 116, 73, 77, 69, 7, 226, 10, 5,
                18, 39, 49, 126, 143, 61, 251, 0, 0, 0,
                23, 116, 69, 88, 116, 83, 111, 102, 116, 119,
                97, 114, 101, 0, 71, 76, 68, 80, 78, 71,
                32, 118, 101, 114, 32, 51, 46, 52, 113, 133,
                164, 225, 0, 0, 0, 12, 73, 68, 65, 84,
                24, 87, 99, 248, 255, 255, 63, 0, 5, 254,
                2, 254, 167, 53, 129, 132, 0, 0, 0, 0,
                73, 69, 78, 68, 174, 66, 96, 130
            };

            Value = graphics.CreateTexture2D(data);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                Value.Dispose();

            disposed = true;
        }

        public void Dispose()
            => Dispose(true);

        public static implicit operator Texture2D(EmptyTexture2D emptyTexture2D)
            => emptyTexture2D.Value;
    }

    /// <summary>
    /// Enumeration that defines how a rectangle will be drawn.
    /// </summary>
    public enum QuadDrawMode : byte
    {
        /// <summary>
        /// Rectangle edges will be rendered.
        /// </summary>
        Edges = 0,

        /// <summary>
        /// Rectangle vertices will be rendered.
        /// </summary>
        Vertices,

        /// <summary>
        /// Rectangle will be filled with color.
        /// </summary>
        Fill
    }

    /// <summary>
    /// Class representing graphics settings for fragments.
    /// </summary>
    public class GraphicsFragmentSettings
    {
        #region Properties
        public SpriteSortMode SpriteSortMode
        {
            get;
        }

        public BlendState BlendState
        {
            get;
        }

        public SamplerState SamplerState
        {
            get;
        }

        public DepthStencilState DepthStencilState
        {
            get;
        }

        public RasterizerState RasterizerState
        {
            get;
        }
        #endregion

        public GraphicsFragmentSettings(SpriteSortMode spriteSortMode = SpriteSortMode.Deferred,
                                        BlendState blendState = null,
                                        SamplerState samplerState = null,
                                        DepthStencilState depthStencilState = null,
                                        RasterizerState rasterizerState = null)
        {
            SpriteSortMode    = spriteSortMode;
            BlendState        = blendState ?? BlendState.AlphaBlend;
            SamplerState      = samplerState ?? SamplerState.PointWrap;
            DepthStencilState = depthStencilState ?? DepthStencilState.None;
            RasterizerState   = rasterizerState ?? RasterizerState.CullClockwise;
        }
    }

    /// <summary>
    /// Interface for implementing graphics fragments. Reason for fragments to exist is
    /// to split rendering to steps. Fragments use double buffered in a sense that they
    /// render initial results to working buffer. These results are then rendered to
    /// presentation back buffer that can be presented to the user. 
    /// </summary>
    public interface IGraphicsFragment
    {
        void DrawSurface(Texture2D texture, Rectangle center, Rectangle destination, Color color);

        void DrawQuad(in Vector2 position,
                      in Vector2 scale,
                      float rotation,
                      in Vector2 origin,
                      in Vector2 bounds,
                      QuadDrawMode mode,
                      in Color color);

        void DrawLine(in Vector2 beginning, in Vector2 end, float thickness, in Color color);

        void DrawSprite(in Vector2 position,
                        in Vector2 scale,
                        float rotation,
                        in Vector2 origin,
                        in Vector2 bounds,
                        Texture2D texture,
                        in Color color,
                        SpriteEffects effects = SpriteEffects.None);

        void DrawSprite(in Vector2 position,
                        in Vector2 scale,
                        float rotation,
                        in Vector2 origin,
                        in Vector2 bounds,
                        in Rectangle source,
                        Texture2D texture,
                        in Color color,
                        SpriteEffects effects = SpriteEffects.None);

        void DrawSpriteText(in Vector2 position,
                            in Vector2 scale,
                            float rotation,
                            in Vector2 origin,
                            string text,
                            SpriteFont font,
                            in Color color);

        void DrawSpriteText(in Vector2 position,
                            in Vector2 scale,
                            float rotation,
                            in Vector2 origin,
                            in Vector2 bounds,
                            string text,
                            SpriteFont font,
                            in Color color);

        /// <summary>
        /// Begins fragment operations using no view. This renders
        /// the results to default view space with no transformations.
        /// </summary>
        void Begin();

        /// <summary>
        /// Begins fragment operations using given view. This
        /// clears the working back buffer of the fragment.
        /// </summary>
        void Begin(IView view);

        /// <summary>
        /// Begins fragment operations using given view and effect. This
        /// clears the working back buffer of the fragment.
        /// </summary>
        void Begin(IView view, Effect effect);

        /// <summary>
        /// Begins fragment operation in suppressed mode using given render target
        /// as back buffer, viewport and with optional matrix and effect.
        /// </summary>
        void Begin(RenderTarget2D renderTarget, in Viewport viewport, in Matrix? matrix = null, Effect effect = null);

        /// <summary>
        /// Clears the fragments presentation back buffer.
        /// </summary>
        void Clear();

        /// <summary>
        /// Renders the fragments working buffer to the
        /// presentation back buffer of the fragment. In case
        /// the work buffer was provided via the begin method,
        /// this method does not draw the results to the presentation
        /// back buffer.
        /// </summary>
        void End();
    }

    /// <summary>
    /// Default implementation of <see cref="IGraphicsFragment"/>.
    /// </summary>
    public sealed class GraphicsFragment : IGraphicsFragment, IDisposable
    {
        #region Fields
        private readonly MissingTexture2D missingTexture;
        private readonly EmptyTexture2D   emptyTexture;

        private readonly GraphicsFragmentSettings settings;
        private readonly IGraphicsDeviceSystem    graphics;

        private readonly SpriteBatch spriteBatch;

        /// <summary>
        /// Buffer used between batches. Gets cleared every
        /// batch.
        /// </summary>
        private RenderTarget2D workBuffer;

        /// <summary>
        /// Buffer used to present work buffer results.
        /// </summary>
        private RenderTarget2D presentationBuffer;

        private bool drawing;

        /// <summary>
        /// Is drawing to presentation buffer suppressed. 
        /// </summary>
        private bool suppressed;
        #endregion

        #region Properties
        public int Order
        {
            get;
        }
        #endregion

        public GraphicsFragment(int order, IGraphicsDeviceSystem graphics, GraphicsFragmentSettings settings)
        {
            Order = order;

            this.graphics = graphics ?? throw new ArgumentException(nameof(graphics));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            missingTexture = new MissingTexture2D(graphics);
            emptyTexture   = new EmptyTexture2D(graphics);
            spriteBatch    = graphics.CreateSpriteBatch();

            UpdateBuffers();
        }

        private void AssertBeginCalled()
        {
            if (!drawing)
                throw new InvalidOperationException($"{nameof(Begin)} must be called before this operation");
        }

        private void AssertEndCalled()
        {
            if (drawing)
                throw new InvalidOperationException($"{nameof(End)} must be called before this operation");
        }

        private void UpdateBuffers()
        {
            // Update buffers if resolution has changed.
            var width   = workBuffer?.Width;
            var height  = workBuffer?.Height;
            var samples = workBuffer?.MultiSampleCount;

            if (workBuffer != null &&
                presentationBuffer != null &&
                width == graphics.BackBufferWidth &&
                height == graphics.BackBufferHeight &&
                samples == graphics.MultiSampleCount)
            {
                return;
            }

            workBuffer?.Dispose();
            presentationBuffer?.Dispose();

            workBuffer = graphics.CreateRenderTarget2D(graphics.BackBufferWidth,
                                                       graphics.BackBufferHeight,
                                                       graphics.MultiSampleCount,
                                                       false,
                                                       SurfaceFormat.Color,
                                                       DepthFormat.None,
                                                       RenderTargetUsage.DiscardContents);

            presentationBuffer = graphics.CreateRenderTarget2D(graphics.BackBufferWidth,
                                                               graphics.BackBufferHeight,
                                                               graphics.MultiSampleCount,
                                                               false,
                                                               SurfaceFormat.Color,
                                                               DepthFormat.None,
                                                               RenderTargetUsage.DiscardContents);
        }

        public void DrawSurface(Texture2D texture, Rectangle center, Rectangle destination, Color color)
        {
            var sources = new[]
            {
                // Top-left.
                new Rectangle(0, 0, center.Left, center.Top),

                // Top-right.
                new Rectangle(center.Right, 0, texture.Width - center.Right, center.Top),

                // Bottom-left.
                new Rectangle(0, center.Bottom, texture.Width - center.Right, texture.Height - center.Bottom),

                // Bottom-right.
                new Rectangle(center.Right, center.Bottom, texture.Width - center.Right, texture.Height - center.Bottom),

                // Top side.
                new Rectangle(center.Left, 0, center.Width, center.Top),

                // Bottom side.
                new Rectangle(center.Left, center.Bottom, center.Width, texture.Height - center.Bottom),

                // Left size.
                new Rectangle(0, center.Top, center.Left, center.Height),

                // Right side.
                new Rectangle(center.Right, center.Top, center.Left, center.Height),

                // Center,
                center
            };

            var centerToDestination = new Vector2(destination.Width / (float)texture.Width, destination.Height / (float)texture.Height);
            var position            = new Vector2(destination.X, destination.Y);

            foreach (var source in sources)
            {
                var batchPosition = position;

                batchPosition.X += source.X * centerToDestination.X;
                batchPosition.Y += source.Y * centerToDestination.Y;

                spriteBatch.Draw(texture, batchPosition, source, color, 0.0f, Vector2.Zero, centerToDestination, SpriteEffects.None, 0.0f);
            }
        }

        public void DrawQuad(in Vector2 position,
                             in Vector2 scale,
                             float rotation,
                             in Vector2 origin,
                             in Vector2 bounds,
                             QuadDrawMode mode,
                             in Color color)
        {
            AssertBeginCalled();

            switch (mode)
            {
                case QuadDrawMode.Edges:
                    Aabb.Rotate(position,
                                bounds,
                                rotation,
                                out var tl,
                                out var tr,
                                out var bl,
                                out var br);

                    tl -= origin;
                    tr -= origin;
                    bl -= origin;
                    br -= origin;

                    var horizontalThickness = bounds.X * 0.15f;
                    var verticalThickness   = bounds.Y * 0.15f;

                    tl.X += horizontalThickness;
                    bl.X += horizontalThickness;

                    tr.X += horizontalThickness;
                    br.X += horizontalThickness;

                    // TODO: origin is a bit fucked.
                    DrawLine(tl, tr, horizontalThickness, color);
                    DrawLine(bl, br, horizontalThickness, color);
                    DrawLine(tl, bl, verticalThickness, color);
                    DrawLine(tr, br, verticalThickness, color);

                    break;
                case QuadDrawMode.Vertices:
                    Aabb.Rotate(position,
                                bounds,
                                rotation,
                                out tl,
                                out tr,
                                out bl,
                                out br);

                    tl -= origin;
                    tr -= origin;
                    bl -= origin;
                    br -= origin;

                    var vertexBounds = bounds * 0.15f;
                    var actualOrigin = origin - bounds * 0.5f;

                    // TODO: origin is a bit fucked.
                    DrawQuad(tl, scale, rotation, actualOrigin, vertexBounds, QuadDrawMode.Fill, color);
                    DrawQuad(tr, scale, rotation, actualOrigin, vertexBounds, QuadDrawMode.Fill, color);
                    DrawQuad(bl, scale, rotation, actualOrigin, vertexBounds, QuadDrawMode.Fill, color);
                    DrawQuad(br, scale, rotation, actualOrigin, vertexBounds, QuadDrawMode.Fill, color);

                    break;
                case QuadDrawMode.Fill:
                    spriteBatch.Draw(emptyTexture,
                                     position,
                                     null,
                                     color,
                                     rotation,
                                     // This seems weird but our empty texture is just one pixel in size.
                                     // This way we can keep the origin real to the size of the texture
                                     // when we scale it.
                                     new Vector2(1.0f / bounds.X * origin.X, 1.0f / bounds.Y * origin.Y),
                                     bounds * scale,
                                     SpriteEffects.None,
                                     0.0f);

                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void DrawLine(in Vector2 beginning,
                             in Vector2 end,
                             float thickness,
                             in Color color)
        {
            AssertBeginCalled();

            spriteBatch.Draw(emptyTexture,
                             beginning,
                             null,
                             color,
                             (float)Math.Atan2(end.Y - beginning.Y, end.X - beginning.X),
                             Vector2.Zero,
                             new Vector2(Vector2.Distance(beginning, end), thickness),
                             SpriteEffects.None,
                             0.0f);
        }

        public void DrawSprite(in Vector2 position,
                               in Vector2 scale,
                               float rotation,
                               in Vector2 origin,
                               in Vector2 bounds,
                               Texture2D texture,
                               in Color color,
                               SpriteEffects effects = SpriteEffects.None)
        {
            AssertBeginCalled();

            texture ??= missingTexture;

            var actualScale  = GraphicsUtils.ActualScale(new Vector2(texture.Width, texture.Height), bounds, scale);
            var actualOrigin = GraphicsUtils.OriginInActualScale(origin, actualScale);

            spriteBatch.Draw(texture,
                             position,
                             null,
                             color,
                             rotation,
                             actualOrigin,
                             actualScale,
                             effects,
                             0.0f);
        }

        public void DrawSprite(in Vector2 position,
                               in Vector2 scale,
                               float rotation,
                               in Vector2 origin,
                               in Vector2 bounds,
                               in Rectangle source,
                               Texture2D texture,
                               in Color color,
                               SpriteEffects effects = SpriteEffects.None)
        {
            AssertBeginCalled();

            texture ??= missingTexture;

            var actualScale  = GraphicsUtils.ActualScale(new Vector2(source.Width, source.Height), bounds, scale);
            var actualOrigin = GraphicsUtils.OriginInActualScale(origin, actualScale);

            spriteBatch.Draw(texture,
                             position,
                             source,
                             color,
                             rotation,
                             actualOrigin,
                             actualScale,
                             effects,
                             0.0f);
        }

        public void DrawSpriteText(in Vector2 position,
                                   in Vector2 scale,
                                   float rotation,
                                   in Vector2 origin,
                                   string text,
                                   SpriteFont font,
                                   in Color color)
        {
            AssertBeginCalled();

            text = !string.IsNullOrEmpty(text) ? text : "MISSING TEXT";

            spriteBatch.DrawString(font,
                                   text,
                                   position,
                                   color,
                                   rotation,
                                   origin,
                                   scale,
                                   SpriteEffects.None,
                                   0.0f);
        }

        public void DrawSpriteText(in Vector2 position,
                                   in Vector2 scale,
                                   float rotation,
                                   in Vector2 origin,
                                   in Vector2 bounds,
                                   string text,
                                   SpriteFont font,
                                   in Color color)
        {
            AssertBeginCalled();

            text = !string.IsNullOrEmpty(text) ? text : "MISSING TEXT";

            var textSize     = font.MeasureString(text);
            var actualScale  = GraphicsUtils.ActualScale(textSize, bounds, scale);
            var actualOrigin = GraphicsUtils.OriginInActualScale(origin, actualScale);

            spriteBatch.DrawString(font,
                                   text,
                                   position,
                                   color,
                                   rotation,
                                   actualOrigin,
                                   actualScale,
                                   SpriteEffects.None,
                                   0.0f);
        }

        public void Begin(RenderTarget2D renderTarget, in Viewport viewport, in Matrix? transform = null, Effect effect = null)
        {
            AssertEndCalled();

            graphics.Viewport = viewport;

            graphics.SetRenderTarget(renderTarget);
            graphics.Clear(Color.Transparent);

            spriteBatch.Begin(settings.SpriteSortMode,
                              settings.BlendState,
                              settings.SamplerState,
                              settings.DepthStencilState,
                              settings.RasterizerState,
                              effect,
                              transform);

            suppressed = true;
            drawing    = true;
        }

        public void Begin(IView view, Effect effect)
        {
            AssertEndCalled();

            UpdateBuffers();

            // Update viewport.
            graphics.Viewport = view?.Viewport ?? new Viewport(0, 0, graphics.BackBufferWidth, graphics.BackBufferHeight);

            // Begin rendering.
            graphics.SetRenderTarget(workBuffer);
            graphics.Clear(Color.Transparent);

            spriteBatch.Begin(settings.SpriteSortMode,
                              settings.BlendState,
                              settings.SamplerState,
                              settings.DepthStencilState,
                              settings.RasterizerState,
                              effect,
                              view?.Matrix);

            suppressed = false;
            drawing    = true;
        }

        public void Begin(IView view)
            => Begin(view, null);

        public void Begin()
            => Begin(null, null);

        public void Clear()
        {
            graphics.SetRenderTarget(presentationBuffer);
            graphics.Clear(Color.Transparent);
            graphics.SetRenderTarget(null);
        }

        public void End()
        {
            AssertBeginCalled();

            spriteBatch.End();

            // Do not draw if we were drawing in suppressed mode.
            if (!suppressed)
            {
                // Swap buffer to presentation buffer.
                graphics.SetRenderTarget(presentationBuffer);

                // Draw the work buffer to presentation buffer.
                spriteBatch.Begin(SpriteSortMode.Deferred,
                                  BlendState.Opaque,
                                  SamplerState.PointClamp,
                                  DepthStencilState.None,
                                  RasterizerState.CullNone);

                spriteBatch.Draw(workBuffer,
                                 Vector2.Zero,
                                 null,
                                 Color.White,
                                 0.0f,
                                 Vector2.Zero,
                                 Vector2.One,
                                 SpriteEffects.None,
                                 0.0f);

                spriteBatch.End();
            }

            drawing = false;
        }

        /// <summary>
        /// Draws the presentation buffer to the default
        /// buffer.
        /// </summary>
        public void Present()
        {
            AssertEndCalled();

            // Reset render target to default.
            graphics.SetRenderTarget(null);

            // Draw the presentation buffer to the default buffer.
            spriteBatch.Begin(SpriteSortMode.Deferred,
                              BlendState.AlphaBlend,
                              SamplerState.PointClamp,
                              DepthStencilState.None,
                              RasterizerState.CullNone);

            spriteBatch.Draw(presentationBuffer,
                             Vector2.Zero,
                             null,
                             Color.White,
                             0.0f,
                             Vector2.Zero,
                             Vector2.One,
                             SpriteEffects.None,
                             0.0f);

            spriteBatch.End();
        }

        public void Dispose()
        {
            workBuffer?.Dispose();
            presentationBuffer?.Dispose();

            spriteBatch.Dispose();
            missingTexture.Dispose();
            emptyTexture.Dispose();
        }
    }

    /// <summary>
    /// Interface for implementing graphics pipeline phases. Phases
    /// handle drawing for single phase of the rendering pipeline.
    ///
    /// For example, we could render our world in the following steps:
    ///    - render foreground tiles
    ///    - render background tiles
    ///    - render entities
    ///    - render water
    ///    - render ui
    ///
    /// Each of these steps contain phase specific logic and data but
    /// do the exact same thing, render stuff.
    ///
    /// Results drawn by phases are combined and rendered in order defined
    /// by fragment order to the screen.
    /// </summary>
    public interface IGraphicsPipelinePhase : IDisposable
    {
        #region Properties
        /// <summary>
        /// Gets or sets boolean declaring whether this fragment is enabled.
        /// </summary>
        bool Disabled
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Executes the phase in the pipeline.
        /// </summary>
        void Execute(IGameEngineTime time);
    }

    /// <summary>
    /// Abstract base class for implementing phases. This base class
    /// allocates fragment with given index for the phase to use.
    /// </summary>
    public abstract class GraphicsPipelinePhase : IGraphicsPipelinePhase
    {
        #region Properties
        public bool Disabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the graphics pipeline.
        /// </summary>
        protected IGraphicsPipelineSystem Pipeline
        {
            get;
        }

        /// <summary>
        /// Gets the fragment index of this phase.
        /// </summary>
        protected int Index
        {
            get;
        }
        #endregion

        protected GraphicsPipelinePhase(IGameHost host, IGraphicsPipelineSystem pipeline, int index, GraphicsFragmentSettings settings = null)
        {
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            Index    = index;

            Pipeline.CreateFragment(index, settings ?? new GraphicsFragmentSettings());
        }

        public abstract void Execute(IGameEngineTime time);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Pipeline.DeleteFragment(Index);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Interface for implementing systems that handle the graphics pipeline
    /// and rendering associated with it. This systems handles presentation
    /// from commanding other systems to draw to commanding the GPU to draw.
    /// </summary>
    public interface IGraphicsPipelineSystem : IGameEngineSystem
    {
        void CreateFragment(int index, GraphicsFragmentSettings settings);
        void DeleteFragment(int index);

        IGraphicsFragment FragmentAtIndex(int index);

        void AddPhase(IGraphicsPipelinePhase phase);
        void RemovePhase(IGraphicsPipelinePhase phase);
    }

    /// <summary>
    /// Default implementation of <see cref="IGraphicsPipelineSystem"/>. 
    /// </summary>
    public sealed class GraphicsPipelineSystem : GameEngineSystem, IGraphicsPipelineSystem
    {
        #region Fields
        private readonly LinearRegistry<GraphicsFragment> fragments;

        private readonly List<IGraphicsPipelinePhase> phases;

        private readonly IGraphicsDeviceSystem graphics;
        #endregion

        [BindingConstructor]
        public GraphicsPipelineSystem(IGraphicsDeviceSystem graphics)
        {
            this.graphics = graphics;

            // Initialize system.
            fragments = new LinearRegistry<GraphicsFragment>();
            phases    = new List<IGraphicsPipelinePhase>();
        }

        public void CreateFragment(int index, GraphicsFragmentSettings settings)
        {
            fragments.Register(index, new GraphicsFragment(index, graphics, settings));
        }

        public void AddPhase(IGraphicsPipelinePhase phase)
            => phases.Add(phase);

        public void RemovePhase(IGraphicsPipelinePhase phase)
        {
            if (!phases.Remove(phase))
                throw new InvalidOperationException("could not remove phase");
        }

        public void DeleteFragment(int index)
        {
            // Dispose and remove the actual fragment.
            var fragment = fragments.AtLocation(index);

            fragment.Dispose();

            // Remove the fragment.
            fragments.Register(index, default);
        }

        public IGraphicsFragment FragmentAtIndex(int index)
            => fragments.AtLocation(index);

        public override void Update(IGameEngineTime time)
        {
            // Clear graphics device default back buffer.
            graphics.SetRenderTarget(null);
            graphics.Clear(Color.Transparent);

            // Clear all fragments
            foreach (var fragment in fragments.Values)
                fragment.Clear();

            // Execute all phases.
            foreach (var phase in phases.Where(p => !p.Disabled))
                phase.Execute(time);

            // Present all results.
            foreach (var fragment in fragments.Values)
                fragment.Present();
        }
    }
}