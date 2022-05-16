using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Events;
using Fracture.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Graphics;

namespace Fracture.Engine.Ecs
{
    /// <summary>
    /// Interface for implementing graphics components. Elements do not
    /// defined their origin as it is always at the center of the element.
    /// </summary>
    public interface IGraphicsComponent
    {
        #region Properties
        /// <summary>
        /// Gets or sets the color of the component.
        /// </summary>
        Color Color
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the local transform of the component.
        /// </summary>
        Transform LocalTransform
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the global transform of the component.
        /// </summary>
        Transform GlobalTransform
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the AABB of the component.
        /// </summary>
        Aabb Aabb
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets origin of the component. This value
        /// is precomputed and should not be tempered with
        /// by other than <see cref="GraphicsComponentSystem{T}"/>.
        /// </summary>
        Vector2 Origin
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets untransformed bounds of the element.
        /// </summary>
        Vector2 Bounds
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the layer of the component.
        /// </summary>
        string CurrentLayer
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the next layer of the component.
        /// </summary>
        string NextLayer
        {
            get;
            set;
        }
        #endregion
    }

    /// <summary>
    /// Interface for implementing graphics component systems. Graphics components
    /// systems are responsible of managing and drawing the components.
    /// </summary>
    public interface IGraphicsComponentSystem : IComponentSystem
    {
        #region Properties
        /// <summary>
        /// Gets the graphics element type id (or the type hint) of
        /// this graphics component system.
        /// </summary>
        int GraphicsComponentTypeId
        {
            get;
        }
        #endregion

        Aabb GetAabb(int componentId);
        Transform GetTransform(int componentId);
        Color GetColor(int componentId);
        string GetLayer(int componentId);

        void SetBounds(int componentId, in Vector2 bounds);
        void SetTransform(int componentId, in Transform transform);
        void SetColor(int componentId, in Color color);
        void SetLayer(int componentId, string name);

        void TranslatePosition(int componentId, in Vector2 translation);
        void TranslateScale(int componentId, in Vector2 translation);
        void TranslateRotation(int componentId, float translation);

        void TransformPosition(int componentId, in Vector2 transformation);
        void TransformScale(int componentId, in Vector2 transformation);
        void TransformRotation(int componentId, float transformation);

        void DrawElement(int componentId, IGraphicsFragment fragment);
    }

    public abstract class GraphicsComponentSystem<T> : SharedComponentSystem, IGraphicsComponentSystem
        where T : struct, IGraphicsComponent
    {
        #region Constant fields
        /// <summary>
        /// Initial components capacity of the system.
        /// </summary>
        protected const int ComponentsCapacity = 128;
        #endregion

        #region Fields
        private readonly IEventHandler<int, TransformChangedEventArgs> transformChangedEvents;

        private readonly HashSet<int> scrubbedComponentIds;
        private readonly HashSet<int> dirtyComponentIds;
        #endregion

        #region Properties
        protected IGraphicsLayerSystem Layers
        {
            get;
        }

        protected ITransformComponentSystem Transforms
        {
            get;
        }

        protected LinearGrowthList<T> Components
        {
            get;
        }

        public int GraphicsComponentTypeId
        {
            get;
        }
        #endregion

        protected GraphicsComponentSystem(IEventQueueSystem events,
                                          IGraphicsLayerSystem layers,
                                          ITransformComponentSystem transforms,
                                          int graphicsComponentTypeId)
            : base(events)
        {
            transformChangedEvents = events.GetEventHandler<int, TransformChangedEventArgs>();

            Layers     = layers ?? throw new ArgumentNullException(nameof(layers));
            Transforms = transforms ?? throw new ArgumentNullException(nameof(layers));

            GraphicsComponentTypeId = graphicsComponentTypeId;

            Components           = new LinearGrowthList<T>(ComponentsCapacity);
            scrubbedComponentIds = new HashSet<int>(ComponentsCapacity);
            dirtyComponentIds    = new HashSet<int>(ComponentsCapacity);
        }

        public override bool Delete(int componentId)
        {
            var deleted = base.Delete(componentId);

            if (deleted)
            {
                var layer = Components.AtIndex(componentId).CurrentLayer;

                Layers.FirstOrDefault(l => l.Name == layer)?.Remove(componentId);

                Components.Insert(componentId, default);
            }

            dirtyComponentIds.Remove(componentId);

            return deleted;
        }

        public Aabb GetAabb(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Aabb;
        }

        public Transform GetTransform(int componentId)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            return Transform.TranslateLocal(component.GlobalTransform, component.GlobalTransform);
        }

        public Color GetColor(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Color;
        }

        public string GetLayer(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).CurrentLayer;
        }

        public void SetTransform(int componentId, in Transform transform)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).LocalTransform = transform;

            dirtyComponentIds.Add(componentId);
        }

        public void SetBounds(int componentId, in Vector2 bounds)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            component.Bounds = bounds;
            component.Aabb   = new Aabb(component.LocalTransform.Position, component.LocalTransform.Rotation, bounds);

            dirtyComponentIds.Add(componentId);
        }

        public void SetColor(int componentId, in Color color)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Color = color;
        }

        public void SetLayer(int componentId, string name)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).NextLayer = name;

            dirtyComponentIds.Add(componentId);
        }

        public void TranslatePosition(int componentId, in Vector2 translation)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            component.LocalTransform = Transform.TranslatePosition(component.LocalTransform, translation);

            dirtyComponentIds.Add(componentId);
        }

        public void TranslateScale(int componentId, in Vector2 translation)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            component.LocalTransform = Transform.TranslateScale(component.LocalTransform, translation);

            dirtyComponentIds.Add(componentId);
        }

        public void TranslateRotation(int componentId, float translation)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            component.LocalTransform = Transform.TranslateRotation(component.LocalTransform, translation);

            dirtyComponentIds.Add(componentId);
        }

        public void TransformPosition(int componentId, in Vector2 transformation)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            component.LocalTransform = Transform.TransformPosition(component.LocalTransform, transformation);

            dirtyComponentIds.Add(componentId);
        }

        public void TransformScale(int componentId, in Vector2 transformation)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            component.LocalTransform = Transform.TransformScale(component.LocalTransform, transformation);

            dirtyComponentIds.Add(componentId);
        }

        public void TransformRotation(int componentId, float transformation)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            component.LocalTransform = Transform.TransformRotation(component.LocalTransform, transformation);

            dirtyComponentIds.Add(componentId);
        }

        public abstract void DrawElement(int componentId, IGraphicsFragment fragment);

        public override void Update(IGameEngineTime time)
        {
            base.Update(time);

            // Add dirty components from last frame.
            foreach (var componentId in scrubbedComponentIds)
                dirtyComponentIds.Add(componentId);

            scrubbedComponentIds.Clear();

            // Update transformations.
            transformChangedEvents.Handle((in Letter<int, TransformChangedEventArgs> letter) =>
            {
                if (!BoundTo(letter.Args.EntityId))
                    return LetterHandlingResult.Retain;

                foreach (var componentId in AllFor(letter.Args.EntityId))
                {
                    Components.AtIndex(componentId).GlobalTransform = letter.Args.Transform;

                    dirtyComponentIds.Add(componentId);
                }

                return LetterHandlingResult.Retain;
            });

            // Update all dirty elements.
            foreach (var componentId in dirtyComponentIds.Where(IsAlive))
            {
                // Get associated data.
                ref var component = ref Components.AtIndex(componentId);

                // Recompute AABB.
                component.Aabb = new Aabb(component.GlobalTransform.Position + component.LocalTransform.Position,
                                          component.GlobalTransform.Rotation + component.LocalTransform.Rotation,
                                          component.Bounds);

                // Update origin.
                component.Origin = Transform.LocalScale(component.GlobalTransform, component.LocalTransform) * component.Bounds * 0.5f;

                if (component.CurrentLayer != component.NextLayer)
                {
                    // Move to new layer.
                    var aabb = component.Aabb;

                    // Remove from current layer if component has one.
                    if (Layers.TryGetLayer(component.CurrentLayer, out var currentLayer))
                        currentLayer.Remove(componentId);

                    // Add to next layer.
                    if (!Layers.TryGetLayer(component.NextLayer, out var nextLayer))
                    {
                        scrubbedComponentIds.Add(componentId);

                        continue;
                    }

                    nextLayer.Add(componentId, GraphicsComponentTypeId, ref aabb, out var clamped);

                    // Update AABB if clamped.
                    if (clamped)
                        component.Aabb = aabb;

                    component.CurrentLayer = component.NextLayer;
                }
                else
                {
                    // Relocate inside existing layer.
                    var aabb = component.Aabb;

                    // Update on layer.
                    if (Layers.TryGetLayer(component.CurrentLayer, out var layer))
                    {
                        scrubbedComponentIds.Add(componentId);

                        continue;
                    }

                    layer.Update(componentId, ref aabb, out var clamped);

                    // Update in system if AABB was clamped.
                    if (clamped)
                        component.Aabb = aabb;
                }
            }

            dirtyComponentIds.Clear();
        }
    }

    public static class GraphicsComponentTypeId
    {
        #region Constant fields
        public const int Sprite = 0;
        public const int Quad = 1;
        public const int SpriteAnimation = 2;
        public const int SpriteText = 3;
        #endregion
    }

    public class SpriteSet
    {
        #region Fields
        private readonly Texture2D texture;

        private readonly List<Rectangle> sources;
        #endregion

        public SpriteSet(Texture2D texture, params Rectangle [] sources)
        {
            this.texture = texture ?? throw new ArgumentNullException(nameof(texture));

            this.sources = new List<Rectangle>(sources ?? throw new ArgumentNullException(nameof(sources)));
        }

        public Rectangle GetSource(int index) => sources[index];

        public static implicit operator Texture2D(SpriteSet spriteSet) => spriteSet.texture;
    }

    /// <summary>
    /// Interface for implementing sprite graphics component systems.
    /// </summary>
    public interface ISpriteComponentSystem : IGraphicsComponentSystem
    {
        int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Color color, Texture2D texture);
        int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Rectangle source, in Color color, Texture2D texture);

        Rectangle? GetSource(int componentId);
        Texture2D GetTexture(int componentId);
        SpriteEffects GetEffects(int componentId);

        void SetSource(int componentId, in Rectangle? source);
        void SetTexture(int componentId, Texture2D texture);
        void SetEffects(int componentId, SpriteEffects effects);
    }

    public sealed class SpriteComponentSystem : GraphicsComponentSystem<SpriteComponentSystem.SpriteComponent>, ISpriteComponentSystem
    {
        #region Sprite component structure
        /// <summary>
        /// Structure that contains sprite data.
        /// </summary>
        public struct SpriteComponent : IGraphicsComponent
        {
            #region Sprite member properties
            public Texture2D Texture
            {
                get;
                set;
            }

            public Rectangle? Source
            {
                get;
                set;
            }

            public Color Color
            {
                get;
                set;
            }

            public SpriteEffects Effects
            {
                get;
                set;
            }
            #endregion

            #region Graphics component member properties
            public Transform LocalTransform
            {
                get;
                set;
            }

            public Transform GlobalTransform
            {
                get;
                set;
            }

            public Vector2 Origin
            {
                get;
                set;
            }

            public Vector2 Bounds
            {
                get;
                set;
            }

            public Aabb Aabb
            {
                get;
                set;
            }

            public string CurrentLayer
            {
                get;
                set;
            }

            public string NextLayer
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        [BindingConstructor]
        public SpriteComponentSystem(IEventQueueSystem events,
                                     IGraphicsLayerSystem layers,
                                     ITransformComponentSystem transforms)
            : base(events, layers, transforms, Ecs.GraphicsComponentTypeId.Sprite)
        {
        }

        public override void DrawElement(int componentId, IGraphicsFragment fragment)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            var transform = Transform.TranslateLocal(component.GlobalTransform, component.LocalTransform);

            if (component.Source != null)
            {
                fragment.DrawSprite(Transform.ToScreenUnits(transform.Position),
                                    transform.Scale,
                                    transform.Rotation,
                                    Transform.ToScreenUnits(component.Origin),
                                    Transform.ToScreenUnits(component.Bounds),
                                    component.Source.Value,
                                    component.Texture,
                                    component.Color,
                                    component.Effects);
            }
            else
            {
                fragment.DrawSprite(Transform.ToScreenUnits(transform.Position),
                                    transform.Scale,
                                    transform.Rotation,
                                    Transform.ToScreenUnits(component.Origin),
                                    Transform.ToScreenUnits(component.Bounds),
                                    component.Texture,
                                    component.Color,
                                    component.Effects);
            }
        }

        public int Create(int entityId,
                          string layer,
                          in Transform transform,
                          in Vector2 bounds,
                          in Color color,
                          Texture2D texture)
        {
            var componentId = InitializeComponent(entityId);

            SetTransform(componentId, transform);
            SetBounds(componentId, bounds);
            SetColor(componentId, color);

            SetTexture(componentId, texture);
            SetLayer(componentId, layer);

            return componentId;
        }

        public int Create(int entityId,
                          string layer,
                          in Transform transform,
                          in Vector2 bounds,
                          in Rectangle source,
                          in Color color,
                          Texture2D texture)
        {
            var componentId = InitializeComponent(entityId);

            SetTransform(componentId, transform);
            SetBounds(componentId, bounds);
            SetColor(componentId, color);

            SetSource(componentId, source);
            SetTexture(componentId, texture);
            SetLayer(componentId, layer);

            return componentId;
        }

        public Rectangle? GetSource(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Source;
        }

        public Texture2D GetTexture(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Texture;
        }

        public SpriteEffects GetEffects(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Effects;
        }

        public void SetSource(int componentId, in Rectangle? source)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Source = source;
        }

        public void SetTexture(int componentId, Texture2D texture)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Texture = texture;
        }

        public void SetEffects(int componentId, SpriteEffects effects)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Effects = effects;
        }
    }

    public interface IQuadComponentSystem : IGraphicsComponentSystem
    {
        int Create(int entityId,
                   string layer,
                   in Transform transform,
                   in Vector2 bounds,
                   in Color color,
                   QuadDrawMode mode);

        QuadDrawMode GetMode(int componentId);

        void SetMode(int componentId, QuadDrawMode mode);
    }

    public sealed class QuadComponentSystem : GraphicsComponentSystem<QuadComponentSystem.QuadComponent>, IQuadComponentSystem
    {
        #region Quad component structure
        public struct QuadComponent : IGraphicsComponent
        {
            #region Quad member properties
            public QuadDrawMode Mode
            {
                get;
                set;
            }
            #endregion

            #region Graphics component members properties
            public Color Color
            {
                get;
                set;
            }

            public Transform LocalTransform
            {
                get;
                set;
            }

            public Transform GlobalTransform
            {
                get;
                set;
            }

            public Aabb Aabb
            {
                get;
                set;
            }

            public Vector2 Origin
            {
                get;
                set;
            }

            public Vector2 Bounds
            {
                get;
                set;
            }

            public string CurrentLayer
            {
                get;
                set;
            }

            public string NextLayer
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        [BindingConstructor]
        public QuadComponentSystem(IEventQueueSystem events,
                                   IGraphicsLayerSystem layers,
                                   ITransformComponentSystem transforms)
            : base(events, layers, transforms, Ecs.GraphicsComponentTypeId.Quad)
        {
        }

        public int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Color color, QuadDrawMode mode)
        {
            var componentId = InitializeComponent(entityId);

            SetTransform(componentId, transform);
            SetBounds(componentId, bounds);
            SetColor(componentId, color);

            SetMode(componentId, mode);
            SetLayer(componentId, layer);

            return componentId;
        }

        public QuadDrawMode GetMode(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Mode;
        }

        public void SetMode(int componentId, QuadDrawMode mode)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Mode = mode;
        }

        public override void DrawElement(int componentId, IGraphicsFragment fragment)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            var transform = Transform.TranslateLocal(component.GlobalTransform, component.LocalTransform);

            fragment.DrawQuad(Transform.ToScreenUnits(transform.Position),
                              transform.Scale,
                              transform.Rotation,
                              Transform.ToScreenUnits(component.Origin),
                              Transform.ToScreenUnits(component.Bounds),
                              component.Mode,
                              component.Color);
        }
    }

    /// <summary>
    /// Enumeration defining how sprite animations are played.
    /// </summary>
    public enum SpriteAnimationMode : byte
    {
        /// <summary>
        /// Animation is played in a loop.
        /// </summary>
        Loop = 0,

        /// <summary>
        /// Animation is played once.
        /// </summary>
        Play = 1
    }

    /// <summary>
    /// Interface for implementing sprite animation component systems.
    /// </summary>
    public interface ISpriteAnimationComponentSystem : IGraphicsComponentSystem
    {
        int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Color color, SpriteAnimationPlaylist playlist);

        void Play(int componentId, string animationName, in TimeSpan frameDurationModifier = default, float frameDurationScale = 1.0f);
        void Loop(int componentId, string animationName, in TimeSpan frameDurationModifier = default, float frameDurationScale = 1.0f);
        void Stop(int componentId);
        void Resume(int componentId);

        SpriteAnimationPlaylist GetPlaylist(int componentId);
        void SetPlaylist(int componentId, SpriteAnimationPlaylist playlist);

        string GetCurrentAnimationName(int componentId);
        SpriteEffects GetEffects(int componentId);
        void SetEffects(int componentId, SpriteEffects effects);
    }

    public readonly struct SpriteAnimationFinishedEventArgs
    {
        #region Properties
        public int ComponentId
        {
            get;
        }
        #endregion

        public SpriteAnimationFinishedEventArgs(int componentId) => ComponentId = componentId;
    }

    public sealed class SpriteAnimationComponentSystem : GraphicsComponentSystem<SpriteAnimationComponentSystem.SpriteAnimationComponent>,
                                                         ISpriteAnimationComponentSystem
    {
        #region Sprite animation structure
        public struct SpriteAnimationComponent : IGraphicsComponent
        {
            #region Sprite animation component member properties
            public TimeSpan Elapsed
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the last active playlist of the component.
            /// </summary>
            public SpriteAnimationPlaylist Playlist
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the animation name currently in use
            /// from the active playlist.
            /// </summary>
            public string AnimationName
            {
                get;
                set;
            }

            public int FrameId
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the animation mode of the component.
            /// </summary>
            public SpriteAnimationMode Mode
            {
                get;
                set;
            }

            public bool Playing
            {
                get;
                set;
            }

            public SpriteEffects Effects
            {
                get;
                set;
            }

            public float FrameDuractionScale
            {
                get;
                set;
            }

            public TimeSpan FrameDurationModifier
            {
                get;
                set;
            }
            #endregion

            #region Graphics component member properties
            public Color Color
            {
                get;
                set;
            }

            public Transform LocalTransform
            {
                get;
                set;
            }

            public Transform GlobalTransform
            {
                get;
                set;
            }

            public Aabb Aabb
            {
                get;
                set;
            }

            public Vector2 Origin
            {
                get;
                set;
            }

            public Vector2 Bounds
            {
                get;
                set;
            }

            public string CurrentLayer
            {
                get;
                set;
            }

            public string NextLayer
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        #region Fields
        private readonly FreeList<int> indices;

        private readonly Dictionary<int, SpriteAnimationPlaylist> playlists;

        private readonly IUniqueEvent<int, SpriteAnimationFinishedEventArgs> finishedEvents;
        #endregion

        [BindingConstructor]
        public SpriteAnimationComponentSystem(IEntitySystem entities,
                                              IEventQueueSystem events,
                                              IGraphicsLayerSystem layers,
                                              ITransformComponentSystem transforms)
            : base(events, layers, transforms, Ecs.GraphicsComponentTypeId.SpriteAnimation)
        {
            finishedEvents = events.CreateUnique<int, SpriteAnimationFinishedEventArgs>();

            var idc = 1;

            indices   = new FreeList<int>(() => idc++);
            playlists = new Dictionary<int, SpriteAnimationPlaylist>();
        }

        private void ChangeState(int componentId, bool playing)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Playing = playing;
        }

        private void ChangeAnimation(int componentId,
                                     string animationName,
                                     SpriteAnimationMode mode,
                                     in TimeSpan frameDurationModifier = default,
                                     float frameDurationScale = 1.0f)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            component.AnimationName         = animationName;
            component.Mode                  = mode;
            component.Elapsed               = TimeSpan.Zero;
            component.FrameId               = 0;
            component.FrameDuractionScale   = frameDurationScale;
            component.FrameDurationModifier = frameDurationModifier;

            ChangeState(componentId, true);
        }

        public int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Color color, SpriteAnimationPlaylist playlist)
        {
            var componentId = InitializeComponent(entityId);

            SetTransform(componentId, transform);
            SetBounds(componentId, bounds);
            SetColor(componentId, color);
            SetLayer(componentId, layer);
            SetPlaylist(componentId, playlist);

            finishedEvents.Create(componentId);

            return componentId;
        }

        public void Play(int componentId, string animationName, in TimeSpan frameDurationModifier = default, float frameDurationScale = 1.0f) =>
            ChangeAnimation(componentId, animationName, SpriteAnimationMode.Play, frameDurationModifier, frameDurationScale);

        public void Loop(int componentId, string animationName, in TimeSpan frameDurationModifier = default, float frameDurationScale = 1.0f) =>
            ChangeAnimation(componentId, animationName, SpriteAnimationMode.Loop, frameDurationModifier, frameDurationScale);

        public void Stop(int componentId) => ChangeState(componentId, false);

        public void Resume(int componentId) => ChangeState(componentId, true);

        public SpriteAnimationPlaylist GetPlaylist(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Playlist;
        }

        public void SetPlaylist(int componentId, SpriteAnimationPlaylist playlist)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Playlist = playlist;
        }

        public string GetCurrentAnimationName(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).AnimationName;
        }

        public SpriteEffects GetEffects(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Effects;
        }

        public void SetEffects(int componentId, SpriteEffects effects)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Effects = effects;
        }

        public override void Update(IGameEngineTime time)
        {
            base.Update(time);

            foreach (var componentId in Alive)
            {
                ref var component = ref Components.AtIndex(componentId);

                if (!component.Playing)
                    continue;

                if (component.Playlist == null)
                    continue;

                component.Elapsed += time.Elapsed;

                var animation = component.Playlist.GetAnimation(component.AnimationName);
                var duration = TimeSpan.FromTicks((long)(animation.Durations[component.FrameId].Ticks +
                                                         component.FrameDurationModifier.Ticks *
                                                         component.FrameDuractionScale));

                if (component.Elapsed >= duration)
                {
                    component.Elapsed = TimeSpan.Zero;

                    component.FrameId++;
                }

                if (component.FrameId < animation.Frames.Length)
                    continue;

                finishedEvents.Publish(componentId, new SpriteAnimationFinishedEventArgs(componentId));

                component.FrameId = 0;
                component.Playing = component.Mode == SpriteAnimationMode.Loop;
            }
        }

        public override void DrawElement(int componentId, IGraphicsFragment fragment)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            var animation = component.Playlist.GetAnimation(component.AnimationName);
            var texture   = component.Playlist.GetTexture(component.AnimationName);
            var transform = Transform.TranslateLocal(component.GlobalTransform, component.LocalTransform);

            fragment.DrawSprite(Transform.ToScreenUnits(transform.Position),
                                transform.Scale,
                                transform.Rotation,
                                Transform.ToScreenUnits(component.Origin),
                                Transform.ToScreenUnits(component.Bounds),
                                animation.Frames[component.FrameId],
                                texture,
                                component.Color,
                                component.Effects);
        }
    }

    /// <summary>
    /// Enumeration that defines how text will be drawn.
    /// </summary>
    public enum TextDrawMode : byte
    {
        /// <summary>
        /// Text can overflow element bounds. 
        /// </summary>
        Overflow = 0,

        /// <summary>
        /// Text will be fitted to the element bounds.
        /// </summary>
        Fit
    }

    public interface ISpriteTextComponentSystem : IGraphicsComponentSystem
    {
        int Create(int entityId,
                   string layer,
                   in Transform transform,
                   in Vector2 bounds,
                   in Color color,
                   string text,
                   SpriteFont font,
                   TextDrawMode mode);

        string GetText(int componentId);
        SpriteFont GetFont(int componentId);
        TextDrawMode GetMode(int componentId);

        void SetText(int componentId, string text);
        void SetFont(int componentId, SpriteFont font);
        void SetMode(int componentId, TextDrawMode mode);
    }

    public sealed class SpriteTextComponentSystem : GraphicsComponentSystem<SpriteTextComponentSystem.SpriteTextComponent>,
                                                    ISpriteTextComponentSystem
    {
        #region Sprite text component structure
        public struct SpriteTextComponent : IGraphicsComponent
        {
            #region Sprite text member properties
            public string Text
            {
                get;
                set;
            }

            public SpriteFont Font
            {
                get;
                set;
            }

            public TextDrawMode Mode
            {
                get;
                set;
            }
            #endregion

            #region Graphics component member properties
            public Color Color
            {
                get;
                set;
            }

            public Transform LocalTransform
            {
                get;
                set;
            }

            public Transform GlobalTransform
            {
                get;
                set;
            }

            public Aabb Aabb
            {
                get;
                set;
            }

            public Vector2 Origin
            {
                get;
                set;
            }

            public Vector2 Bounds
            {
                get;
                set;
            }

            public string CurrentLayer
            {
                get;
                set;
            }

            public string NextLayer
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        [BindingConstructor]
        public SpriteTextComponentSystem(IEventQueueSystem events,
                                         IGraphicsLayerSystem layers,
                                         ITransformComponentSystem transforms)
            : base(events, layers, transforms, Ecs.GraphicsComponentTypeId.SpriteText)
        {
        }

        public int Create(int entityId,
                          string layer,
                          in Transform transform,
                          in Vector2 bounds,
                          in Color color,
                          string text,
                          SpriteFont font,
                          TextDrawMode mode)
        {
            var componentId = InitializeComponent(entityId);

            SetTransform(componentId, transform);
            SetBounds(componentId, bounds);
            SetColor(componentId, color);

            SetText(componentId, text);
            SetFont(componentId, font);
            SetMode(componentId, mode);

            SetLayer(componentId, layer);

            return componentId;
        }

        public string GetText(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Text;
        }

        public SpriteFont GetFont(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Font;
        }

        public TextDrawMode GetMode(int componentId)
        {
            AssertAlive(componentId);

            return Components.AtIndex(componentId).Mode;
        }

        public void SetText(int componentId, string text)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Text = text;
        }

        public void SetFont(int componentId, SpriteFont font)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Font = font;
        }

        public void SetMode(int componentId, TextDrawMode mode)
        {
            AssertAlive(componentId);

            Components.AtIndex(componentId).Mode = mode;
        }

        public override void DrawElement(int componentId, IGraphicsFragment fragment)
        {
            AssertAlive(componentId);

            ref var component = ref Components.AtIndex(componentId);

            var transform = Transform.TranslateLocal(component.GlobalTransform, component.LocalTransform);

            switch (component.Mode)
            {
                case TextDrawMode.Overflow:
                    fragment.DrawSpriteText(Transform.ToScreenUnits(transform.Position),
                                            transform.Scale,
                                            transform.Rotation,
                                            Transform.ToScreenUnits(component.Origin),
                                            component.Text,
                                            component.Font,
                                            component.Color);
                    break;
                case TextDrawMode.Fit:
                    fragment.DrawSpriteText(Transform.ToScreenUnits(transform.Position),
                                            transform.Scale,
                                            transform.Rotation,
                                            Transform.ToScreenUnits(component.Origin),
                                            Transform.ToScreenUnits(component.Bounds),
                                            component.Text,
                                            component.Font,
                                            component.Color);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}