using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sonosthesia
{

    // TouchInfo and TouchHistory give a reusable way to allow implementations to provide their own velocity and acceleration
    // descriptions, or default to one based on previous values

    public struct TouchInfo
    {
        public float time;
        public Vector3? position;
        public Vector3? velocity;
        public Vector3? acceleration;
    }

    public class TouchHistory
    {
        // fixed sized struct arrays are a real pain in the a*se so resorting to seperate variables... 
        private TouchInfo? _t1;
        private TouchInfo? _t2;
        private TouchInfo? _t3;

        public void Push(TouchInfo info)
        {
            _t3 = _t2;
            _t2 = _t1;
            _t1 = info;
        }

        public void Reset()
        {
            _t3 = null;
            _t2 = null;
            _t1 = null;
        } 

        public Vector3 Position
        {
            get
            {
                if (_t1 != null && _t1.Value.position != null)
                {
                    return _t1.Value.position.Value;
                }
                return Vector3.zero;
            }
        }

        public Vector3 Velocity
        {
            get
            {
                // if we have velocity info, use that
                if (_t1 != null && _t1.Value.velocity != null)
                {
                    return _t1.Value.velocity.Value;
                }
                // or calculate based on position
                else if (_t1 != null && _t1.Value.position != null && _t2 != null && _t2.Value.position != null)
                {
                    float deltaTime = (_t1.Value.time - _t2.Value.time);
                    return (Mathf.Approximately(deltaTime, 0f)) ? Vector3.zero :(_t1.Value.position.Value - _t2.Value.position.Value) / deltaTime;
                }
                return Vector3.zero;
            }
        }

        public Vector3 Acceleration
        {
            get
            {
                // if we have acceleration info, use that
                if (_t1 != null && _t1.Value.acceleration != null)
                {
                    return _t1.Value.acceleration.Value;
                }
                // else if we have velocity info, use that
                else if (_t1 != null && _t1.Value.velocity != null && _t2 != null && _t2.Value.velocity != null)
                {
                    return (_t1.Value.velocity.Value - _t2.Value.velocity.Value) / (_t1.Value.time - _t2.Value.time);
                }
                // else if we have position info, use that
                else if (_t1 != null && _t1.Value.position != null && _t2 != null && _t2.Value.position != null && _t3 != null && _t3.Value.position != null)
                {
                    float deltaTime1 = (_t1.Value.time - _t2.Value.time);
                    if (Mathf.Approximately(deltaTime1, 0f)) return Vector3.zero;
                    Vector3 vel1 = (_t1.Value.position.Value - _t2.Value.position.Value) / deltaTime1;
                    float deltaTime2 = (_t2.Value.time - _t3.Value.time);
                    if (Mathf.Approximately(deltaTime2, 0f)) return Vector3.zero;
                    Vector3 vel2 = (_t2.Value.position.Value - _t3.Value.position.Value) / deltaTime2;
                    return (vel1 - vel2) / deltaTime1;
                }
                return Vector3.zero;
            }
        }
    }

    abstract public class PadChannelOutput : ChannelOutput
    {
        public RectTransform pad;

        public Camera rayCamera;

        public float maxPressure = 10f;
        public float maxSpeed = 10f;
        public float maxAcceleration = 10f;

        public const string KEY_POSITION = "touch_position";
        public const string KEY_VELOCITY = "touch_velocity";
        public const string KEY_ACCELERATION = "touch_acceleration";

        public override IEnumerable<ChannelParameterDescription> ParameterDescriptions { get { return _parameterDescriptions; } }

        public bool IsInteractive { get { return gameObject.activeInHierarchy && enabled && pad && pad.gameObject.activeSelf && pad.gameObject.activeInHierarchy; } }


        static private ObjectPool<TouchHistory> _touchHistoryPool = new ObjectPool<TouchHistory>();

        static private IEnumerable<ChannelParameterDescription> _parameterDescriptions = new List<ChannelParameterDescription>()
        {
            // note: third dimension is for pressure in the case of 
            new ChannelParameterDescription(KEY_POSITION, 0f, 1f, 0f, 3),
            new ChannelParameterDescription(KEY_VELOCITY, 0f, 1f, 0f, 3),
            new ChannelParameterDescription(KEY_ACCELERATION, 0f, 1f, 0f, 3)
        };

        // key used for touch/finger/mouse pointer id or whatever else 
        private Dictionary<int, ChannelInstance> _touchInstances = new Dictionary<int, ChannelInstance>();

        private Dictionary<int, TouchHistory> _touchHistories = new Dictionary<int, TouchHistory>();
        

        // used for various info gathering operations on each frame
        private List<int> _idList = new List<int>();

        private bool _currentInteractive = false;
        
        abstract protected TouchInfo GetTouchInfo(int index);

        abstract protected void GetStartingTouches(List<int> list);

        abstract protected void GetEndingTouches(List<int> list);

        protected virtual void GetCurrentTouches(List<int> list)
        {
            list.AddRange(_touchHistories.Keys);
        }

        public IEnumerable<int> CurrentTouchIds
        {
            get
            {
                // to list to copy so that the enumerable can then modify the collections without going sideways
                return _touchInstances.Keys.ToList(); 
            }
        }

        public bool TouchIsOngoing(int index)
        {
            return _touchHistories.ContainsKey(index);
        }

        public TouchHistory FetchTouchHistory()
        {
            TouchHistory touchHistory = _touchHistoryPool.Fetch();
            touchHistory.Reset();
            return touchHistory;
        }

        public void StoreTouchHistory(TouchHistory history)
        {
            _touchHistoryPool.Store(history);
        }

        protected override void Awake()
        {
            base.Awake();

            if (!rayCamera)
            {
                rayCamera = Camera.main;
            }
        }


        protected virtual void SetupInteraction()
        {

        }

        protected virtual void TeardownInteraction()
        {
            _idList.Clear();
            GetCurrentTouches(_idList);

            foreach (int index in _idList)
            {
                EndTouch(index);
            }
        }

        protected virtual void Update()
        {
            // http://answers.unity3d.com/questions/947856/how-to-detect-click-outside-ui-panel.html

            if (IsInteractive)
            {
                if (!_currentInteractive)
                {
                    SetupInteraction();
                    _currentInteractive = true;
                }

                _idList.Clear();
                GetStartingTouches(_idList);
                
                foreach(int touchId in _idList)
                {
                    StartTouch(touchId);
                }

                _idList.Clear();
                GetCurrentTouches(_idList);

                foreach (int touchId in _idList)
                {
                    UpdateTouch(touchId);
                }

                _idList.Clear();
                GetEndingTouches(_idList);

                foreach (int touchId in _idList)
                {
                    if (TouchIsOngoing(touchId)) EndTouch(touchId);
                }
            }
            else 
            {
                if (!_currentInteractive)
                {
                    TeardownInteraction();
                    _currentInteractive = false;
                }
            }
            
        }

        private void StartTouch(int touchId)
        {
            //Debug.Log("StartTouch " + touchId);

            ChannelInstance instance = FetchChannelInstance();
            TouchHistory history = FetchTouchHistory();

            _touchHistories[touchId] = history;
            _touchInstances[touchId] = instance;

            history.Push(GetTouchInfo(touchId));

            ApplyTouchHistoryToInstance(history, instance);

            CreateInstance(instance);
        }

        private void UpdateTouch(int touchId)
        {
            //Debug.Log("UpdateTouch " + touchId);

            ChannelInstance instance = _touchInstances[touchId];
            TouchHistory history = _touchHistories[touchId];

            history.Push(GetTouchInfo(touchId));

            ApplyTouchHistoryToInstance(history, instance);

            ControlInstance(instance);
        }

        private void EndTouch(int touchId)
        {
            //Debug.Log("EndTouch " + touchId);

            ChannelInstance instance = _touchInstances[touchId];
            TouchHistory history = _touchHistories[touchId];

            history.Push(GetTouchInfo(touchId));

            ApplyTouchHistoryToInstance(history, instance);

            DestroyInstance(instance);

            _touchHistories.Remove(touchId);
            _touchInstances.Remove(touchId);

            StoreChannelInstance(instance);
            StoreTouchHistory(history);
        }
        
        protected virtual void ApplyTouchHistoryToInstance(TouchHistory history, ChannelInstance instance)
        {
            instance.parameters.SetParameter(KEY_POSITION, history.Position);
            instance.parameters.SetParameter(KEY_VELOCITY, history.Velocity);
            instance.parameters.SetParameter(KEY_ACCELERATION, history.Acceleration);
        }

        protected virtual bool ScreenPointIsInPanel(Vector3 position)
        {
            if (!pad)
            {
                return false;
            }

            Canvas canvas = pad.GetComponentInParent<Canvas>();

            // https://forum.unity3d.com/threads/whats-wrong-with-recttransformutility-rectanglecontainsscreenpoint-camera-argument.328618/
            Camera cam = (canvas && (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)) ? rayCamera : null;

            bool result = RectTransformUtility.RectangleContainsScreenPoint(pad, position, rayCamera);

            return result;
        }
    }

}
