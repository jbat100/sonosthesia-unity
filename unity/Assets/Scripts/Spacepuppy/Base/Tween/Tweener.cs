using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace com.spacepuppy.Tween
{

    public abstract class Tweener : ISPDisposable, IProgressingYieldInstruction, IRadicalWaitHandle
    {

        #region Events

        /// <summary>
        /// Raised every tick of the tweener
        /// </summary>
        public event System.EventHandler OnStep;
        /// <summary>
        /// Raised every time a looping/ping-ponging tweener reaches a peek.
        /// </summary>
        public event System.EventHandler OnWrap;
        /// <summary>
        /// Raised when the tween successfully finishes
        /// </summary>
        public event System.EventHandler OnFinish;
        /// <summary>
        /// Raised when the tween is stopped, killed, or finished. You can determine if the tween finished or was 
        /// killed on this event by testing the 'IsComplete' and 'IsDead' properties respectively. 
        /// </summary>
        public event System.EventHandler OnStopped;
        
        #endregion

        #region Fields

        private UpdateSequence _updateType;
        private ITimeSupplier _timeSupplier = SPTime.Normal;
        private TweenWrapMode _wrap;
        private int _wrapCount;
        private bool _reverse;
        private float _speedScale = 1.0f;
        private float _delay;


        private bool _isPlaying;
        private float _playHeadLength;
        private float _time; //the time since the tween was first played
        private float _unwrappedPlayHeadPosition; //we need an unwrapped value so that we can pingpong/loop the playhead
        private float _normalizedPlayHeadPosition; //this position the playhead is currently at, with wrap applied

        private int _currentWrapCount;

        private object _autoKillToken;
        
        #endregion

        #region Configurable Properties

        /// <summary>
        /// An identifier for this tween to relate it to other tweens. Usually its the object being tweened, if one exists.
        /// </summary>
        public abstract object Id
        {
            get;
            set;
        }

        public object AutoKillToken
        {
            get { return _autoKillToken; }
            set
            {
                if (this.IsPlaying) throw new System.InvalidOperationException("Can only chnage the AutoKillToken on a Tweener that is not currently playing.");

                _autoKillToken = value;
            }
        }
        
        public UpdateSequence UpdateType
        {
            get { return _updateType; }
            set { _updateType = value; }
        }

        public ITimeSupplier TimeSupplier
        {
            get { return _timeSupplier; }
            set { _timeSupplier = value ?? SPTime.Normal; }
        }

        public DeltaTimeType DeltaType
        {
            get { return SPTime.GetDeltaType(_timeSupplier); }
        }

        public TweenWrapMode WrapMode
        {
            get { return _wrap; }
            set
            {
                if (_wrap == value) return;

                _wrap = value;
                //normalized time is dependent on WrapMode, so we force update the play head
                this.MovePlayHeadPosition(0f);
            }
        }

        /// <summary>
        /// Amount of times the tween should wrap if WrapMode loops or pingpongs. 
        /// A value of 0 or less will wrap infinitely.
        /// </summary>
        public int WrapCount
        {
            get { return _wrapCount; }
            set { _wrapCount = value; }
        }

        public bool Reverse
        {
            get { return _reverse; }
            set { _reverse = value; }
        }

        public float SpeedScale
        {
            get { return _speedScale; }
            set
            {
                _speedScale = value;
                if (_speedScale < 0f || float.IsNaN(_speedScale)) _speedScale = 0f;
                else if (float.IsInfinity(_speedScale)) _speedScale = float.MaxValue;
            }
        }

        public float Delay
        {
            get { return _delay; }
            set
            {
                _delay = Mathf.Clamp(value, 0f, float.MaxValue);
            }
        }

        #endregion

        #region Status Properties

        public bool IsDead
        {
            get { return float.IsNaN(_time); }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
        }

        public bool IsComplete
        {
            get
            {
                if (float.IsNaN(_time)) return true;
                switch(_wrap)
                {
                    case TweenWrapMode.Once:
                        return _time >= this.PlayHeadLength + _delay;
                    case TweenWrapMode.Loop:
                    case TweenWrapMode.PingPong:
                        if (_wrapCount <= 0)
                            return false;
                        else
                            return (_time - _delay) >= (this.PlayHeadLength * _wrapCount);
                }
                return false;
            }
        }

        public float PlayHeadLength
        {
            get
            {
                return (_isPlaying) ? _playHeadLength : this.GetPlayHeadLength();
            }
        }

        /// <summary>
        /// The amount of time that has passed for the tween (sum of all calls to update).
        /// </summary>
        public float Time
        {
            get { return _time; }
        }

        public float TotalTime
        {
            get
            {
                switch(_wrap)
                {
                    case TweenWrapMode.Once:
                        return this.PlayHeadLength;
                    case TweenWrapMode.Loop:
                    case TweenWrapMode.PingPong:
                        return (_wrapCount <= 0) ? float.PositiveInfinity : this.PlayHeadLength * _wrapCount;
                }
                return 0f;
            }
        }

        /// <summary>
        /// The position of the play-head relative to PlayHeadLength of the tween.
        /// </summary>
        public float PlayHeadPosition
        {
            get { return _normalizedPlayHeadPosition; }
        } 

        [System.Obsolete("User PlayHeadPosition")]
        public float PlayHeadTime
        {
            get { return _normalizedPlayHeadPosition; }
        }

        public int CurrentWrapCount
        {
            get { return _currentWrapCount; }
        }

        /// <summary>
        /// Does the configuration of this tween result in a tween that plays forever.
        /// </summary>
        public bool PlaysIndefinitely
        {
            get { return _wrap != TweenWrapMode.Once && _wrapCount <= 0; }
        }

        #endregion

        #region Methods

        public void Play()
        {
            if (_isPlaying) return;
            this.Play((_reverse) ? this.PlayHeadLength : 0f);
        }

        public virtual void Play(float playHeadPosition)
        {
            if (this.IsDead) throw new System.InvalidOperationException("Cannot play a dead Tweener.");

            if (!_isPlaying)
            {
                _isPlaying = true;
                _playHeadLength = this.GetPlayHeadLength();
                SPTween.AddReference(this);
            }

            _unwrappedPlayHeadPosition = playHeadPosition;
            _normalizedPlayHeadPosition = playHeadPosition;
        }

        public void Resume()
        {
            if (_isPlaying) return;
            this.Play(this.PlayHeadPosition);
        }

        public void ResumeReverse()
        {
            _reverse = !_reverse;
            if (_isPlaying) return;
            this.Play(this.PlayHeadPosition);
        }

        public virtual void Stop()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            SPTween.RemoveReference(this);
            if (this.OnStopped != null) this.OnStopped(this, System.EventArgs.Empty);
        }

        public virtual void Kill()
        {
            SPTween.RemoveReference(this);
            this.SetKilled();
        }

        /// <summary>
        /// Called internally if killed by SPTween.
        /// </summary>
        internal void SetKilled()
        {
            _isPlaying = false;
            _time = float.NaN;
            if (this.OnStopped != null) this.OnStopped(this, System.EventArgs.Empty);
            this.OnFinish = null;
            this.OnStopped = null;
            this.OnStep = null;
            this.OnWrap = null;
        }

        public virtual void Reset()
        {
            this.Stop();
            _currentWrapCount = 0;
            _time = 0f;
            _unwrappedPlayHeadPosition = 0f;
            _normalizedPlayHeadPosition = 0f;
        }

        /// <summary>
        /// Moves the playhead to the end and raises the finished event. 
        /// </summary>
        /// <returns></returns>
        public bool CompleteImmediately()
        {
            if (!this.IsPlaying) return false;

            switch (_wrap)
            {
                case TweenWrapMode.Once:
                    float odt = this.PlayHeadLength - _time;
                    _time = this.PlayHeadLength + _delay + 0.0001f;
                    _normalizedPlayHeadPosition = (_reverse) ? 0f : _time;
                    _unwrappedPlayHeadPosition = _normalizedPlayHeadPosition;
                    this.DoUpdate(odt, _normalizedPlayHeadPosition);
                    this.Stop();
                    if (this.OnFinish != null) this.OnFinish(this, System.EventArgs.Empty);
                    return true;
                case TweenWrapMode.Loop:
                case TweenWrapMode.PingPong:
                    if (_wrapCount <= 0)
                    {
                        //this doesn't make sense... you can't complete an infinite tween
                    }
                    else
                    {
                        float pdt = (this.PlayHeadLength * _wrapCount) - _time;
                        _time = this.PlayHeadLength * _wrapCount + _delay + 0.0001f;
                        _normalizedPlayHeadPosition = (_reverse) ? 0f : (_wrapCount % 2 == 0) ? 0f : this.PlayHeadLength;
                        _unwrappedPlayHeadPosition = _normalizedPlayHeadPosition;
                        this.DoUpdate(pdt, _normalizedPlayHeadPosition);
                        this.Stop();
                        if (this.OnFinish != null) this.OnFinish(this, System.EventArgs.Empty);
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Move the playhead an amount of change.
        /// </summary>
        /// <param name="dt"></param>
        public virtual void Scrub(float dt)
        {
            if (this.IsDead) return;

            this.MovePlayHeadPosition(dt);

            this.DoUpdate(dt, _normalizedPlayHeadPosition);

            switch (_wrap)
            {
                case TweenWrapMode.Once:
                    if (this.IsComplete)
                    {
                        _time = this.PlayHeadLength + _delay + 0.0001f;
                        this.Stop();
                        if (this.OnFinish != null) this.OnFinish(this, System.EventArgs.Empty);
                        break;
                    }
                    else
                    {
                        if (this.OnStep != null) this.OnStep(this, System.EventArgs.Empty);
                    }
                    break;
                case TweenWrapMode.Loop:
                case TweenWrapMode.PingPong:
                    if (_time > this.PlayHeadLength * (_currentWrapCount + 1))
                    {
                        _currentWrapCount++;
                        if (this.IsComplete)
                        {
                            _time = this.PlayHeadLength * _wrapCount + _delay + 0.0001f;
                            this.Stop();
                            if (this.OnFinish != null) this.OnFinish(this, System.EventArgs.Empty);
                        }
                        else
                        {
                            if (this.OnStep != null) this.OnStep(this, System.EventArgs.Empty);
                            if (this.OnWrap != null) this.OnWrap(this, System.EventArgs.Empty);
                        }
                    }
                    else
                    {
                        if (this.OnStep != null) this.OnStep(this, System.EventArgs.Empty);
                    }
                    break;
            }
        }

        private void MovePlayHeadPosition(float dt)
        {
            _time += Mathf.Abs(dt);
            if (_reverse)
                _unwrappedPlayHeadPosition -= dt;
            else
                _unwrappedPlayHeadPosition += dt;

            var totalDur = this.PlayHeadLength;
            if (totalDur > 0f)
            {
                switch (_wrap)
                {
                    case TweenWrapMode.Once:
                        _normalizedPlayHeadPosition = Mathf.Clamp(_unwrappedPlayHeadPosition - _delay, 0, totalDur);
                        break;
                    case TweenWrapMode.Loop:
                        if (_unwrappedPlayHeadPosition < _delay)
                            _normalizedPlayHeadPosition = 0f;
                        else
                            _normalizedPlayHeadPosition = Mathf.Repeat(_unwrappedPlayHeadPosition - _delay, totalDur);
                        break;
                    case TweenWrapMode.PingPong:
                        if (_normalizedPlayHeadPosition < _delay)
                            _normalizedPlayHeadPosition = 0f;
                        else
                            _normalizedPlayHeadPosition = Mathf.PingPong(_unwrappedPlayHeadPosition - _delay, totalDur);
                        break;
                }
            }
            else
            {
                _normalizedPlayHeadPosition = 0f;
            }
        }

        internal virtual void Update()
        {
            this.Scrub(_timeSupplier.Delta * _speedScale);
        }

        #endregion

        #region Tweener Interface

        /// <summary>
        /// Return true if the target was destroyed, used to purge unneeded tweens on a scene unload.
        /// </summary>
        protected internal abstract bool GetTargetIsDestroyed();

        protected internal abstract float GetPlayHeadLength();

        protected internal abstract void DoUpdate(float dt, float t);

        #endregion

        #region IDisposable Interface

        bool ISPDisposable.IsDisposed
        {
            get { return this.IsDead; }
        }

        void System.IDisposable.Dispose()
        {
            if (this.IsDead) return;

            this.Kill();
        }

        #endregion

        #region IRadicalYieldInstruction Interface

        bool IRadicalYieldInstruction.IsComplete
        {
            get { return this.IsComplete; }
        }

        float IProgressingYieldInstruction.Progress
        {
            get
            {
                if (this.IsComplete) return 1f;

                switch (_wrap)
                {
                    case TweenWrapMode.Once:
                        return _time / (this.PlayHeadLength + _delay);
                    case TweenWrapMode.Loop:
                    case TweenWrapMode.PingPong:
                        if (_wrapCount <= 0)
                            return 0f;
                        else
                            return _time / (this.PlayHeadLength * _wrapCount + _delay);
                }
                return 0f;
            }
        }

        bool IRadicalYieldInstruction.Tick(out object yieldObject)
        {
            yieldObject = null;
            return !this.IsComplete;
        }

        void IRadicalWaitHandle.OnComplete(System.Action<IRadicalWaitHandle> callback)
        {
            System.EventHandler d = null;
            d = (s, e) =>
            {
                this.OnStopped -= d;
                callback(this);
            };

            this.OnStopped += d;
        }

        bool IRadicalWaitHandle.Cancelled
        {
            get { return float.IsNaN(_time); }
        }

        #endregion

    }

}