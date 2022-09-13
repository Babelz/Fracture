using System;
using System.Collections.Generic;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Microsoft.Xna.Framework.Audio;

namespace Fracture.Engine.Core.Systems
{
    public readonly struct VolumeChangedEventArgs : IStructEventArgs
    {
        #region Properties
        public string ChannelName
        {
            get;
        }

        public float Value
        {
            get;
        }
        #endregion

        public VolumeChangedEventArgs(string channelName, float value)
        {
            ChannelName = channelName;
            Value       = value;
        }
    }

    public static class DefaultSoundChannels
    {
        #region Constant fields
        public const string Master = "master";

        public const string Music = "music";

        public const string Effect = "effect";
        #endregion
    }

    public interface ISoundSystem : IGameEngineSystem
    {
        #region Events
        event StructEventHandler<VolumeChangedEventArgs> VolumeChanged;
        #endregion

        void CreateChannel(string name);

        void SetVolume(string channelName, float value);
        float GetVolume(string channelName);

        void Play(SoundEffect effect);
        bool IsPlaying();

        void Stop();
    }

    public sealed class SoundSystem : GameEngineSystem, ISoundSystem
    {
        #region Fields
        private readonly Dictionary<string, float> channels;

        private SoundEffectInstance current;
        #endregion

        #region Events
        public event StructEventHandler<VolumeChangedEventArgs> VolumeChanged;
        #endregion

        [BindingConstructor]
        public SoundSystem()
        {
            channels = new Dictionary<string, float>();

            CreateChannel(DefaultSoundChannels.Master);
            CreateChannel(DefaultSoundChannels.Effect);
            CreateChannel(DefaultSoundChannels.Music);
        }

        public void CreateChannel(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            if (!channels.ContainsKey(name))
                throw new InvalidOperationException($"channel with name {name} already exists");

            channels.Add(name, default);
        }

        public void SetVolume(string channelName, float value)
        {
            channels[channelName] = value;

            VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(channelName, value));

            if ((channelName != DefaultSoundChannels.Master && channelName != DefaultSoundChannels.Music) && !IsPlaying())
                return;

            current.Stop();

            current.Volume = channels[DefaultSoundChannels.Master] * channels[DefaultSoundChannels.Music];

            current.Resume();
        }

        public float GetVolume(string channelName)
            => channelName == DefaultSoundChannels.Master ? channels[channelName] : channels[channelName] * channels[DefaultSoundChannels.Master];

        public void Play(SoundEffect effect)
        {
            current?.Dispose();

            current        = effect.CreateInstance();
            current.Volume = channels[DefaultSoundChannels.Master] * channels[DefaultSoundChannels.Music];

            current.Play();
        }

        public void Loop(SoundEffect effect)
        {
            current?.Dispose();

            current          = effect.CreateInstance();
            current.Volume   = channels[DefaultSoundChannels.Master] * channels[DefaultSoundChannels.Music];
            current.IsLooped = true;

            current.Play();
        }

        public bool IsPlaying()
            => current is { State: SoundState.Playing };

        public void Stop()
            => current?.Stop();
    }
}