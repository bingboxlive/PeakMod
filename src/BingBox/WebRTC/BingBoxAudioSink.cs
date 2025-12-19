using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;

namespace BingBox.WebRTC
{
    public class BingBoxAudioSink : IAudioSink
    {
        private readonly short[] _buffer;
        private readonly int _mask;
        private int _writePos;
        private int _readPos;

        public BingBoxAudioSink()
        {
            int size = 65536;
            _buffer = new short[size];
            _mask = size - 1;
#pragma warning disable CS0618
            _opusDecoder = new Concentus.Structs.OpusDecoder(48000, 1);
#pragma warning restore CS0618
            _decodeBuffer = new short[5760];
        }

        private readonly Concentus.Structs.OpusDecoder _opusDecoder;
        private readonly short[] _decodeBuffer;

        public void AddSamples(short[] samples)
        {
            int len = samples.Length;
            int currentWrite = Volatile.Read(ref _writePos);

            for (int i = 0; i < len; i++)
            {
                _buffer[currentWrite & _mask] = samples[i];
                currentWrite++;
            }

            Volatile.Write(ref _writePos, currentWrite);
        }

        private bool _isBuffering = true;
        private const int MIN_BUFFER_COUNT = 7200;
        private const int MAX_LATENCY_COUNT = 24000;
        private const int TARGET_LATENCY_COUNT = 12000;

        public void Read(float[] data, int channels)
        {
            int currentRead = Volatile.Read(ref _readPos);
            int currentWrite = Volatile.Read(ref _writePos);

            int available = currentWrite - currentRead;

            if (_isBuffering)
            {
                if (available < MIN_BUFFER_COUNT)
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }
                _isBuffering = false;
            }
            else if (available <= 0)
            {
                _isBuffering = true;
                Array.Clear(data, 0, data.Length);
                return;
            }

            int count = data.Length;

            for (int i = 0; i < count; i++)
            {
                if (currentRead < currentWrite)
                {
                    data[i] = _buffer[currentRead & _mask] / 32768.0f;
                    currentRead++;
                }
                else
                {
                    data[i] = 0.0f;
                    _isBuffering = true;
                }
            }

            int remaining = currentWrite - currentRead;
            if (remaining > MAX_LATENCY_COUNT)
            {
                currentRead = currentWrite - TARGET_LATENCY_COUNT;
            }

            Volatile.Write(ref _readPos, currentRead);
        }

#pragma warning disable CS0067
        public event SourceErrorDelegate? OnAudioSinkError;
#pragma warning restore CS0067

        public System.Collections.Generic.List<AudioFormat> GetAudioSinkFormats()
        {
            return new System.Collections.Generic.List<AudioFormat>
             {
                 new AudioFormat(AudioCodecsEnum.PCMU, 1),
                 new AudioFormat(AudioCodecsEnum.PCMA, 2),
                 new AudioFormat(AudioCodecsEnum.OPUS, 3)
             };
        }

        public void SetAudioSinkFormat(AudioFormat audioFormat)
        {
        }

        public Task StartAudioSink()
        {
            return Task.CompletedTask;
        }

        public Task PauseAudioSink()
        {
            return Task.CompletedTask;
        }

        public Task ResumeAudioSink()
        {
            return Task.CompletedTask;
        }

        public Task CloseAudioSink()
        {
            return Task.CompletedTask;
        }

        public void GotAudioRtp(System.Net.IPEndPoint remoteEndPoint, uint ssrc, uint seqNum, uint timestamp, int payloadID, bool marker, byte[] payload)
        {
            if (payload == null || payload.Length == 0) return;

            try
            {
                lock (_opusDecoder)
                {
#pragma warning disable CS0618
                    int decodedSamples = _opusDecoder.Decode(payload, 0, payload.Length, _decodeBuffer, 0, _decodeBuffer.Length, false);
#pragma warning restore CS0618
                    if (decodedSamples > 0)
                    {
                        int currentWrite = Volatile.Read(ref _writePos);
                        for (int i = 0; i < decodedSamples; i++)
                        {
                            _buffer[currentWrite & _mask] = _decodeBuffer[i];
                            currentWrite++;
                        }
                        Volatile.Write(ref _writePos, currentWrite);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void RestrictFormats(Func<AudioFormat, bool> restrictionFunc) { }
        public void GotAudioSample(byte[] sample, uint timestamp, uint ssrc, int samplingRate)
        {
            int sampleCount = sample.Length / 2;
            int currentWrite = Volatile.Read(ref _writePos);

            for (int i = 0; i < sampleCount; i++)
            {
                short val = (short)(sample[i * 2] | (sample[i * 2 + 1] << 8));
                _buffer[currentWrite & _mask] = val;
                currentWrite++;
            }

            Volatile.Write(ref _writePos, currentWrite);
        }
    }
}
