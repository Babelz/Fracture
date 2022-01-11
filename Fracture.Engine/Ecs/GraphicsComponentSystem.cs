using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
      
      Aabb GetAabb(int id);
      Transform GetTransform(int id);
      Color GetColor(int id);
      string GetLayer(int id);
      
      void SetBounds(int id, in Vector2 bounds);
      void SetTransform(int id, in Transform transform);
      void SetColor(int id, in Color color);
      void SetLayer(int id, string name);
      
      void TranslatePosition(int id, in Vector2 translation);
      void TranslateScale(int id, in Vector2 translation);
      void TranslateRotation(int id, float translation);
      
      void TransformPosition(int id, in Vector2 transformation);
      void TransformScale(int id, in Vector2 transformation);
      void TransformRotation(int id, float transformation);

      void DrawElement(int id, IGraphicsFragment fragment);
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
      private readonly IEventHandler<int, TransformChangedEventArgs> transformChangedEvent;

      private readonly HashSet<int> dirty;
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

      protected LinearGrowthArray<T> Components
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
         
         Layers     = layers ?? throw new ArgumentNullException(nameof(layers));
         Transforms = transforms ?? throw new ArgumentNullException(nameof(layers));
         
         transformChangedEvent = events.GetEventHandler<int, TransformChangedEventArgs>();
         
         GraphicsComponentTypeId = graphicsComponentTypeId;
         
         Components = new LinearGrowthArray<T>(ComponentsCapacity);
         dirty      = new HashSet<int>(ComponentsCapacity);
      }
      
      protected override int InitializeComponent(int entityId)
      {
         var id = base.InitializeComponent(entityId);
         
         if (id >= Components.Length)
            Components.Grow();

         return id;
      }

      public override bool Delete(int id)
      {
         var deleted = base.Delete(id);
         var layer   = Components.AtIndex(id).CurrentLayer;
         
         Layers.First(l => l.Name == layer).Remove(id);
         
         if (deleted)
            Components.Insert(id, default);
         
         dirty.Remove(id);

         return deleted;
      }

      public Aabb GetAabb(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Aabb;
      }
      public Transform GetTransform(int id)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
            
         return Transform.TranslateLocal(component.GlobalTransform, component.GlobalTransform);
      }
      
      public Color GetColor(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Color;
      }
      public string GetLayer(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).CurrentLayer;
      }
      
      public void SetTransform(int id, in Transform transform)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).LocalTransform = transform;
         
         dirty.Add(id);
      }
      public void SetBounds(int id, in Vector2 bounds)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         component.Bounds = bounds; 
         component.Aabb   = new Aabb(component.LocalTransform.Position, component.LocalTransform.Rotation, bounds);

         dirty.Add(id);
      }
      public void SetColor(int id, in Color color)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Color = color;
      }
      public void SetLayer(int id, string name)
      {
         AssertAlive(id);
       
         if (!Layers.Any(l => l.Name == name))
            throw new InvalidOperationException($"layer {name} does not exist");

         Components.AtIndex(id).NextLayer = name;
         
         dirty.Add(id);
      }

      public void TranslatePosition(int id, in Vector2 translation)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         component.LocalTransform = Transform.TranslatePosition(component.LocalTransform, translation);
         
         dirty.Add(id);
      }

      public void TranslateScale(int id, in Vector2 translation)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         component.LocalTransform = Transform.TranslateScale(component.LocalTransform, translation);
         
         dirty.Add(id);
      }

      public void TranslateRotation(int id, float translation)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         component.LocalTransform = Transform.TranslateRotation(component.LocalTransform, translation);
         
         dirty.Add(id);
      }

      public void TransformPosition(int id, in Vector2 transformation)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         component.LocalTransform = Transform.TransformPosition(component.LocalTransform, transformation);
         
         dirty.Add(id);
      }

      public void TransformScale(int id, in Vector2 transformation)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         component.LocalTransform = Transform.TransformScale(component.LocalTransform, transformation);

         dirty.Add(id);
      }

      public void TransformRotation(int id, float transformation)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         component.LocalTransform = Transform.TransformRotation(component.LocalTransform, transformation);

         dirty.Add(id);
      }

      public abstract void DrawElement(int id, IGraphicsFragment fragment);
      
      public override void Update(IGameEngineTime time)
      {
         // Update transformations.
         transformChangedEvent.Handle((in Letter<int, TransformChangedEventArgs> letter) =>
         {
            if (!BoundTo(letter.Args.EntityId))
               return LetterHandlingResult.Retain;
            
            foreach (var id in AllFor(letter.Args.EntityId))
            {
               Components.AtIndex(id).GlobalTransform = letter.Args.Transform;
               
               dirty.Add(id);
            }
               
            return LetterHandlingResult.Retain;
         });
         
         // Update all dirty elements.
         var layersLookup = Layers.ToDictionary(l => l.Name, l => l);
         
         foreach (var id in dirty)
         {
            // Get associated data.
            ref var component = ref Components.AtIndex(id);
         
            // Recompute AABB.
            component.Aabb = new Aabb(component.GlobalTransform.Position + component.LocalTransform.Position,
                                      component.GlobalTransform.Rotation + component.LocalTransform.Rotation,
                                      component.Bounds);
            
            // Update origin.
            component.Origin = Transform.LocalScale(component.GlobalTransform, component.LocalTransform) * component.Bounds * 0.5f;
            
            if (component.CurrentLayer != component.NextLayer)
            {
               // Move to new layer.
               var currentLayer = component.CurrentLayer;
               var nextLayer    = component.NextLayer;
               var aabb         = component.Aabb;
               
               // Remove from current layer if component has one.
               Layers.FirstOrDefault(l => l.Name == currentLayer)?.Remove(id);
         
               // Insert to new layer.
               Layers.First(l => l.Name == nextLayer).Add(id, GraphicsComponentTypeId, ref aabb, out var clamped);
         
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
               layersLookup[component.CurrentLayer].Update(id, ref aabb, out var clamped);
            
               // Update in system if AABB was clamped.
               if (clamped) 
                  component.Aabb = aabb;  
            }
         }
         
         dirty.Clear();
      }
   }
   
   public static class GraphicsComponentTypeId
   {
      #region Constant fields
      public const int Sprite          = 0;
      public const int Quad            = 1;
      public const int SpriteAnimation = 2;
      public const int SpriteText      = 3;
      #endregion
   }
   
   public class SpriteSet
   {
      #region Fields
      private readonly Texture2D texture;
      
      private readonly List<Rectangle> sources;
      #endregion

      public SpriteSet(Texture2D texture, params Rectangle[] sources)
      {
         this.texture = texture ?? throw new ArgumentNullException(nameof(texture));
         
         this.sources = new List<Rectangle>(sources ?? throw new ArgumentNullException(nameof(sources)));
      }
      
      public Rectangle GetSource(int index)
         => sources[index];
      
      public static implicit operator Texture2D(SpriteSet spriteSet) 
         => spriteSet.texture;
   }
   
   /// <summary>
   /// Interface for implementing sprite graphics component systems.
   /// </summary>
   public interface ISpriteComponentSystem : IGraphicsComponentSystem
   {
      int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Color color, Texture2D texture);
      int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Rectangle source, in Color color, Texture2D texture);
      
      Rectangle? GetSource(int id);
      Texture2D GetTexture(int id);
      SpriteEffects GetEffects(int id);

      void SetSource(int id, in Rectangle? source);
      void SetTexture(int id, Texture2D texture);
      void SetEffects(int id, SpriteEffects effects);
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

      public override void DrawElement(int id, IGraphicsFragment fragment)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);

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
         var id = InitializeComponent(entityId);
         
         SetTransform(id, transform);
         SetBounds(id, bounds);
         SetColor(id, color);
         
         SetTexture(id, texture);
         SetLayer(id, layer);
         
         return id;
      }
      
      public int Create(int entityId,
                        string layer,
                        in Transform transform,
                        in Vector2 bounds,
                        in Rectangle source,
                        in Color color,
                        Texture2D texture)
      {         
         var id = InitializeComponent(entityId);
         
         SetTransform(id, transform);
         SetBounds(id, bounds);
         SetColor(id, color);
         
         SetSource(id, source);
         SetTexture(id, texture);
         SetLayer(id, layer);
         
         return id;
      }

      public Rectangle? GetSource(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Source;
      }

      public Texture2D GetTexture(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Texture;
      }
      
      public SpriteEffects GetEffects(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Effects;
      }
      
      public void SetSource(int id, in Rectangle? source)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Source = source;
      }

      public void SetTexture(int id, Texture2D texture)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Texture = texture;
      }

      public void SetEffects(int id, SpriteEffects effects)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Effects = effects;
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
      
      QuadDrawMode GetMode(int id);
      
      void SetMode(int id, QuadDrawMode mode);
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
         var id = InitializeComponent(entityId);
         
         SetTransform(id, transform);
         SetBounds(id, bounds);
         SetColor(id, color);
         
         SetMode(id, mode);
         SetLayer(id, layer);
         
         return id;
      }
      
      public QuadDrawMode GetMode(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Mode;
      }

      public void SetMode(int id, QuadDrawMode mode)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Mode = mode;
      }
      
      public override void DrawElement(int id, IGraphicsFragment fragment)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
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
      int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Color color);
      
      int AddPlaylist(SpriteAnimationPlaylist playlist);
      void RemovePlaylist(int playlistId);
      
      void Play(int componentId, int playlistId, string animationName);
      void Loop(int componentId, int playlistId, string animationName);
      void Stop(int componentId);
      void Resume(int componentId);
      
      int GetCurrentPlaylistId(int id);
      string GetCurrentAnimationName(int id);
      SpriteEffects GetEffects(int id);
      void SetEffects(int id, SpriteEffects effects);
   }
   
   public sealed class SpriteAnimationComponentSystem : 
      GraphicsComponentSystem<SpriteAnimationComponentSystem.SpriteAnimationComponent>,
      ISpriteAnimationComponentSystem
   {
      #region Sprite animation structure
      public struct SpriteAnimationComponent : IGraphicsComponent
      {
         #region Constant fields
         /// <summary>
         /// Const animation index indicating the component
         /// is not animating and has no animation if this
         /// index is active.
         /// </summary>
         public const int NoAnimation = 0;
         #endregion
         
         #region Sprite animation component member properties
         public TimeSpan Elapsed
         {
            get;
            set;
         }
         
         /// <summary>
         /// Gets or sets the animation index of this component.
         /// 0 indicates no animation is being played.
         /// </summary>
         public int PlaylistId
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
      
      private readonly IUniqueEvent<int, ComponentEventArgs> finishedEvents;
      #endregion

      [BindingConstructor]
      public SpriteAnimationComponentSystem(IEntitySystem entities, 
                                            IEventQueueSystem events, 
                                            IGraphicsLayerSystem layers, 
                                            ITransformComponentSystem transforms) 
         : base(events, layers, transforms, Ecs.GraphicsComponentTypeId.SpriteAnimation)
      {
         finishedEvents = events.CreateUniqueEvent<int, ComponentEventArgs>();

         var idc = 1;
         
         indices   = new FreeList<int>(() => idc++);
         playlists = new Dictionary<int, SpriteAnimationPlaylist>();
      }

      private void ChangeState(int componentId, bool playing)
      {
         AssertAlive(componentId);
         
         Components.AtIndex(componentId).Playing = playing;
      }
      
      private void ChangeAnimation(int componentId, int animationId, string animationName, SpriteAnimationMode mode)
      {
         AssertAlive(componentId);
         
         ref var component = ref Components.AtIndex(componentId);
         
         component.PlaylistId    = animationId;
         component.AnimationName = animationName;
         component.Mode          = mode;
         component.Elapsed       = TimeSpan.Zero;
         component.FrameId       = 0;
         
         ChangeState(componentId, true);
      }
      
      public int Create(int entityId, string layer, in Transform transform, in Vector2 bounds, in Color color)
      {
         var id = InitializeComponent(entityId);
         
         SetTransform(id, transform);
         SetBounds(id, bounds);
         SetColor(id, color);
         SetLayer(id, layer);
         
         finishedEvents.Create(id);
         
         return id;
      }

      public int AddPlaylist(SpriteAnimationPlaylist playlist)
      {
         var id = indices.Take();
         
         playlists.Add(id, playlist);
         
         return id;
      }

      public void RemovePlaylist(int playlistId)
      {
         indices.Return(playlistId);
         
         for (var i = 0; i < Count; i++)
         {
            ref var component = ref Components.AtIndex(i);
            
            if (component.PlaylistId == playlistId)
               component.PlaylistId = SpriteAnimationComponent.NoAnimation;
         }
      }
      
      public void Play(int componentId, int animationId, string animationName)
         => ChangeAnimation(componentId, animationId, animationName, SpriteAnimationMode.Play);
      
      public void Loop(int componentId, int animationId, string animationName)
         => ChangeAnimation(componentId, animationId, animationName, SpriteAnimationMode.Loop);

      public void Stop(int componentId)
         => ChangeState(componentId, false);
      
      public void Resume(int componentId)
         => ChangeState(componentId, true);
      
      public int GetCurrentPlaylistId(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).PlaylistId;
      }
      public string GetCurrentAnimationName(int id)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         return component.PlaylistId == SpriteAnimationComponent.NoAnimation ? string.Empty : component.AnimationName;
      }
      
      public SpriteEffects GetEffects(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Effects;
      }
      
      public void SetEffects(int id, SpriteEffects effects)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Effects = effects;
      }

      public override void Update(IGameEngineTime time)
      {
         base.Update(time);

         for (var i = 0; i < Count; i++)
         {
            ref var component = ref Components.AtIndex(i);
            
            if (!component.Playing)
               continue;

            component.Elapsed += time.Elapsed;

            var animation = playlists[component.PlaylistId].Animations[component.AnimationName];
            
            ref var duration = ref animation.Durations[component.FrameId];

            if (component.Elapsed >= duration)
            {
               component.Elapsed = TimeSpan.Zero;
               
               component.FrameId++;
            }

            if (component.FrameId < animation.Frames.Length)
               continue;
               
            component.FrameId = 0;

            finishedEvents.Publish(i, new ComponentEventArgs(i));
                  
            if (component.Mode != SpriteAnimationMode.Play)
               continue;
            
            component.PlaylistId = SpriteAnimationComponent.NoAnimation;
         }
      }

      public override void DrawElement(int id, IGraphicsFragment fragment)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);
         
         var playlist  = playlists[component.PlaylistId];
         var animation = playlist.Animations[component.AnimationName];
         var transform = Transform.TranslateLocal(component.GlobalTransform, component.LocalTransform);

         fragment.DrawSprite(Transform.ToScreenUnits(transform.Position),
                             transform.Scale,
                             transform.Rotation,
                             Transform.ToScreenUnits(component.Origin),
                             Transform.ToScreenUnits(component.Bounds),
                             animation.Frames[component.FrameId],
                             playlist.Texture,
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

      string GetText(int id);
      SpriteFont GetFont(int id);
      TextDrawMode GetMode(int id);
      
      void SetText(int id, string text);
      void SetFont(int id, SpriteFont font);
      void SetMode(int id, TextDrawMode mode);
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
         var id = InitializeComponent(entityId);
         
         SetTransform(id, transform);
         SetBounds(id, bounds);
         SetColor(id, color);
         
         SetText(id, text);
         SetFont(id, font);
         SetMode(id, mode);
         
         SetLayer(id, layer);
         
         return id;
      }

      public string GetText(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Text;
      }

      public SpriteFont GetFont(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Font;
      }
      
      public TextDrawMode GetMode(int id)
      {
         AssertAlive(id);
         
         return Components.AtIndex(id).Mode;
      }

      public void SetText(int id, string text)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Text = text;
      }

      public void SetFont(int id, SpriteFont font)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Font = font;
      }
      
      public void SetMode(int id, TextDrawMode mode)
      {
         AssertAlive(id);
         
         Components.AtIndex(id).Mode = mode;
      }
      
      public override void DrawElement(int id, IGraphicsFragment fragment)
      {
         AssertAlive(id);
         
         ref var component = ref Components.AtIndex(id);

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