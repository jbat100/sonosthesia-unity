﻿namespace com.spacepuppy.Tween
{
    public interface ITweenHash
    {

        ITweenHash SetId(object id);
        ITweenHash Ease(Ease ease);
        ITweenHash Delay(float delay);
        ITweenHash UseUpdate();
        ITweenHash UseFixedUpdate();
        ITweenHash UseLateUpdate();
        ITweenHash Use(UpdateSequence type);
        ITweenHash UseNormalTime();
        ITweenHash UseRealTime();
        ITweenHash UseSmoothTime();
        ITweenHash Use(ITimeSupplier type);
        ITweenHash PlayOnce();
        ITweenHash Loop(int count = -1);
        ITweenHash PingPong(int count = -1);
        ITweenHash Wrap(TweenWrapMode wrap, int count = -1);
        ITweenHash Reverse();
        ITweenHash Reverse(bool reverse);
        ITweenHash SpeedScale(float scale);
        ITweenHash OnStep(System.EventHandler d);
        ITweenHash OnStep(System.Action<Tweener> d);
        ITweenHash OnWrap(System.EventHandler d);
        ITweenHash OnWrap(System.Action<Tweener> d);
        ITweenHash OnFinish(System.EventHandler d);
        ITweenHash OnFinish(System.Action<Tweener> d);
        ITweenHash OnStopped(System.EventHandler d);
        ITweenHash OnStopped(System.Action<Tweener> d);
        
        Tweener Play(bool autoKill, object autoKillToken = null);
        Tweener Play(float playHeadPosition, bool autoKill, object autoKillToken = null);

    }
}
