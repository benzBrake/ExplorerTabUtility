using System;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using H.Hooks;
using ExplorerTabUtility.Models;
using ExplorerTabUtility.Helpers;

namespace ExplorerTabUtility.Hooks;

public sealed class Mouse : IHook
{
    private int _lastClickTime;
    private Key _lastClickKey;
    private readonly LowLevelMouseHook _lowLevelMouseHook;
    private readonly IReadOnlyCollection<HotKeyProfile> _hotkeyProfiles;
    public bool IsHookActive => _lowLevelMouseHook.IsStarted;
    public event Action<HotKeyEventArgs>? OnHotKeyProfileTriggered;

    public Mouse(IReadOnlyCollection<HotKeyProfile> hotkeyProfiles)
    {
        _hotkeyProfiles = hotkeyProfiles;
        _lowLevelMouseHook = new LowLevelMouseHook { AddKeyboardKeys = true };
        _lowLevelMouseHook.Down += LowLevelMouseHook_Down;
        _lowLevelMouseHook.Wheel += LowLevelMouseHook_Wheel;
    }

    public void StartHook() => _lowLevelMouseHook.Start();
    public void StopHook() => _lowLevelMouseHook.Stop();

    private void LowLevelMouseHook_Down(object? sender, MouseEventArgs e)
    {
        var handler = OnHotKeyProfileTriggered;
        if (handler == null) return;

        var isDoubleClick = IsDoubleClick(e.CurrentKey);

        foreach (var profile in _hotkeyProfiles)
        {
            // Skip disabled, empty or non mouse
            if (!profile.IsMouse || !profile.IsEnabled || profile.HotKeys is null || profile.HotKeys.Length == 0)
                continue;

            if (profile.MouseWheelDirection != MouseWheelDirection.None)
                continue;
            
            // Skip if it requires double-click and it is not
            if (profile.IsDoubleClick && !isDoubleClick) continue;
            
            // Skip if keys do not match
            if (!e.Keys.Are(profile.HotKeys)) continue;

            if (!Helper.MatchesHotkeyScope(profile.Scope, e.Position, out var handle)) continue;

            e.IsHandled = profile.IsHandled;

            // Queue the hotkey trigger in a separate thread.
#if NET7_0_OR_GREATER
            ThreadPool.QueueUserWorkItem(static s => s.Handler.Invoke(new HotKeyEventArgs(s.Profile, s.Handle, s.Position)),
                new State(handler, profile, handle, e.Position), false);
#else
            ThreadPool.QueueUserWorkItem(static state =>
            {
                var s = (State)state!;
                s.Handler.Invoke(new HotKeyEventArgs(s.Profile, s.Handle, s.Position));
            }, new State(handler, profile, handle, e.Position));
#endif
        }
    }

    private void LowLevelMouseHook_Wheel(object? sender, MouseEventArgs e)
    {
        if (e.Delta == 0) return;

        var handler = OnHotKeyProfileTriggered;
        if (handler == null) return;

        var direction = e.Delta > 0 ? MouseWheelDirection.Up : MouseWheelDirection.Down;
        foreach (var profile in _hotkeyProfiles)
        {
            if (!profile.IsMouse || profile.MouseWheelDirection == MouseWheelDirection.None)
                continue;

            if (!profile.IsEnabled || profile.MouseWheelDirection != direction ||
                profile.HotKeys is null || profile.HotKeys.Length == 0 || !e.Keys.Are(profile.HotKeys))
                continue;

            if (!Helper.MatchesHotkeyScope(profile.Scope, e.Position, out var handle)) continue;

            e.IsHandled = profile.IsHandled;

#if NET7_0_OR_GREATER
            ThreadPool.QueueUserWorkItem(static s => s.Handler.Invoke(new HotKeyEventArgs(s.Profile, s.Handle, s.Position)),
                new State(handler, profile, handle, e.Position), false);
#else
            ThreadPool.QueueUserWorkItem(static state =>
            {
                var s = (State)state!;
                s.Handler.Invoke(new HotKeyEventArgs(s.Profile, s.Handle, s.Position));
            }, new State(handler, profile, handle, e.Position));
#endif
        }
    }

    private bool IsDoubleClick(Key currentKey)
    {
        var isDoubleClick = false;
        
        var now = Environment.TickCount;
        if (now - _lastClickTime < 500 && _lastClickKey == currentKey)
            isDoubleClick = true;
        
        _lastClickTime = now;
        _lastClickKey = currentKey;
        return isDoubleClick;
    }

    public void Dispose()
    {
        StopHook();
        _lowLevelMouseHook.Dispose();
    }

    private readonly struct State(Action<HotKeyEventArgs> handler, HotKeyProfile profile, nint handle, Point position)
    {
        public readonly Action<HotKeyEventArgs> Handler = handler;
        public readonly HotKeyProfile Profile = profile;
        public readonly nint Handle = handle;
        public readonly Point Position = position;
    }
}
