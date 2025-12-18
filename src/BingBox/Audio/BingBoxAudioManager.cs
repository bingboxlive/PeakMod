using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;

namespace BingBox.Audio;

public class BingBoxAudioManager : MonoBehaviour
{
    public static BingBoxAudioManager? Instance { get; private set; }

    private readonly List<Item> _trackedItems = new List<Item>();
    private AudioSource? _audioSource;
    private AudioClip? _clip;

    private void Awake()
    {
        Instance = this;
        CreateAudioSource();
    }

    private static readonly HashSet<string> _targetNames = new HashSet<string>
    {
        "BingBong",
        "BingBong(Clone)",
        "BingBong_Prop Variant",
        "BingBong_Prop Variant(Clone)",
        "BingBong_Prop",
        "BingBong_Prop(Clone)"
    };



    private void CreateAudioSource()
    {
        var go = new GameObject("BingBox_AudioSource");
        go.transform.SetParent(this.transform);
        _audioSource = go.AddComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.spatialBlend = 1.0f;
        _audioSource.dopplerLevel = Plugin.DopplerConfig.Value ? 1.0f : 0.0f;
        _audioSource.playOnAwake = false;
        _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        _audioSource.minDistance = 1.0f;
        _audioSource.maxDistance = 30.0f;
    }

    private BingBox.WebRTC.BingBoxAudioSink? _audioSink;

    private void Start()
    {
        // 10 seconds buffer length for the clip, but it streams via callback so length is virtual
        _clip = AudioClip.Create("BingLive", 48000 * 2, 1, 48000, true, OnAudioRead);

        if (_audioSource != null)
        {
            _audioSource.clip = _clip;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }

    private void OnAudioRead(float[] data)
    {
        if (_audioSink == null)
        {
            // Silence
            Array.Clear(data, 0, data.Length);
            return;
        }

        _audioSink.Read(data, 1);
    }

    private void Update()
    {
        // Resolve Sink ref on main thread
        if (_audioSink == null)
        {
            var rtcManager = GetComponent<BingBox.WebRTC.BingBoxRtcManager>();
            if (rtcManager != null)
            {
                _audioSink = rtcManager.AudioSink;
            }
        }
    }

    public static bool IsTargetItem(string name)
    {
        return _targetNames.Contains(name);
    }

    public void RegisterItem(Item item)
    {
        if (!_trackedItems.Contains(item))
        {
            _trackedItems.Add(item);
            UpdateTarget();
        }
    }

    public void UnregisterItem(Item item)
    {
        if (_trackedItems.Contains(item))
        {
            _trackedItems.Remove(item);
            UpdateTarget();
        }
    }

    private Item? _currentTarget;

    private void UpdateTarget()
    {
        if (_audioSource == null || _clip == null) return;

        // Optimization: Lazy cleanup from the top of the stack
        while (_trackedItems.Count > 0 && _trackedItems[_trackedItems.Count - 1] == null)
        {
            _trackedItems.RemoveAt(_trackedItems.Count - 1);
        }

        if (_trackedItems.Count > 0)
        {
            _currentTarget = _trackedItems[_trackedItems.Count - 1];

            if (_audioSource.transform.parent != this.transform)
            {
                _audioSource.transform.SetParent(this.transform, false);
            }

            _audioSource.volume = 1.0f;
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
        else
        {
            _currentTarget = null;
            _audioSource.volume = 0.0f;

            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
    }

    private void LateUpdate()
    {
        // Safety check: if current target is destroyed unexpectedly (bypassing Unregister), handle it.
        // In Unity, a destroyed object compares equal to null.
        if (_currentTarget == null && _trackedItems.Count > 0)
        {
            // If we have items but no target, something is stale. Prune all nulls and retry.
            // This is O(N) but only happens on error/unexpected destroy.
            _trackedItems.RemoveAll(item => item == null);
            UpdateTarget();
            return;
        }

        if (_audioSource != null && _currentTarget != null)
        {
            _audioSource.transform.position = _currentTarget.transform.position;
        }
    }
}
