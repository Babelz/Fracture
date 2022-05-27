using System.Collections.Generic;
using Fracture.Engine.Events;
using Microsoft.Xna.Framework.Audio;

namespace Fracture.Engine.Ecs
{
    public interface ISoundComponentSystem : IComponentSystem
    {
        int Create(int entityId);

        void Play(int componentId, SoundEffect effect, int listeningEntityId, float maxDistance, float minDistance);
        void Loop(int componentId, SoundEffect effect, int listeningEntityId, float maxDistance, float minDistance);

        bool IsPlaying(int componentId);

        void Stop(int componentId);
    }

    public sealed class SoundComponentSystem : ComponentSystem, ISoundComponentSystem
    {
        #region Properties
        #endregion

        public SoundComponentSystem(IEventQueueSystem events)
            : base(events)
        {
        }

        public override bool BoundTo(int entityId)
            => throw new System.NotImplementedException();

        public override int FirstFor(int entityId)
            => throw new System.NotImplementedException();

        public override IEnumerable<int> AllFor(int entityId)
            => throw new System.NotImplementedException();

        public int Create(int entityId)
            => throw new System.NotImplementedException();

        public void Play(int componentId, SoundEffect effect, int listeningEntityId, float maxDistance, float minDistance)
            => throw new System.NotImplementedException();

        public void Loop(int componentId, SoundEffect effect, int listeningEntityId, float maxDistance, float minDistance)
            => throw new System.NotImplementedException();

        public bool IsPlaying(int componentId)
            => throw new System.NotImplementedException();

        public void Stop(int componentId)
            => throw new System.NotImplementedException();
    }
}