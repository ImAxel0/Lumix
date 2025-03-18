using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using Lumix.SampleProviders.CachedSounds;
using Lumix.Views.Arrangement;
using Lumix.Views.Preferences.Audio;
using Vanara.PInvoke;

namespace Lumix.Views.Sidebar.Preview;

class AudioPreviewEngine : IDisposable
{
    private readonly MixingSampleProvider previewMixer;
    public MixingSampleProvider PreviewMixer => previewMixer;

    public AudioPreviewEngine(int sampleRate = 44100, int channelCount = 2)
    {
        previewMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
        previewMixer.ReadFully = true;
        ArrangementView.MasterTrack.AudioEngine.MasterMixer.AddMixerInput(previewMixer);
    }

    public void PlaySound(string fileName, bool isPreview = false)
    {
        if (isPreview)
        {
            StopSound();
        }

        var input = new AudioFileReader(fileName);
        AddMixerInput(input);
    }

    public void PlaySound(AudioFileReader audioFile)
    {
        var input = new AudioFileReader(audioFile.FileName);
        AddMixerInput(input);
    }

    private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
    {
        if (input.WaveFormat.Channels == previewMixer.WaveFormat.Channels)
        {
            return input;
        }
        if (input.WaveFormat.Channels == 1 && previewMixer.WaveFormat.Channels == 2)
        {
            return new MonoToStereoSampleProvider(input);
        }
        throw new NotImplementedException("Not yet implemented this channel count conversion");
    }

    public void PlaySound(CachedSound sound)
    {
        AddMixerInput(new CachedSoundSampleProvider(sound));
    }

    private void AddMixerInput(ISampleProvider input)
    {
        if (!previewMixer.WaveFormat.Equals(input.WaveFormat))
        {
            User32.MessageBox(IntPtr.Zero, "Can't play wave file", "WaveFormat exception", User32.MB_FLAGS.MB_ICONWARNING | User32.MB_FLAGS.MB_TOPMOST);
            return;
        }
        previewMixer.AddMixerInput(ConvertToRightChannelCount(input));
    }

    public void StopSound()
    {
        previewMixer.RemoveAllMixerInputs();
    }

    public void Dispose()
    {

    }

    public static AudioPreviewEngine Instance = new AudioPreviewEngine(CoreAudioEngine.SampleRate);
}