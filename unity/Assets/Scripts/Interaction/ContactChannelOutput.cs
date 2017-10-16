using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sonosthesia
{

    // TouchInfo and TouchHistory give a reusable way to allow implementations to provide their own velocity and acceleration
    // descriptions, or default to one based on previous values

    public interface IContactHistory<TContactInfo>
    {
        void Push(TContactInfo info);

        void Reset();
    }

    public interface IContactTime
    {
        float time { get; }
    }

    public interface IContactMovement
    {
        Vector3? position { get; }
        Vector3? velocity { get; }
        Vector3? acceleration { get; }
    }



    public struct ContactInfo : IContactTime, IContactMovement
    {
        public float time { get; set; }
        public Vector3? position { get; set; }
        public Vector3? velocity { get; set; }
        public Vector3? acceleration { get; set; }
    }

    public class ContactHistory<TContactInfo> : IContactHistory<TContactInfo> where TContactInfo : struct, IContactTime, IContactMovement
    {
        // fixed sized struct arrays are a real pain in the a*se so resorting to seperate variables... 
        private TContactInfo? _t1;
        private TContactInfo? _t2;
        private TContactInfo? _t3;

        public void Push(TContactInfo info)
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

    public static class ContactChannelParameters
    {
        public const string KEY_POSITION = "touch_position";
        public const string KEY_VELOCITY = "touch_velocity";
        public const string KEY_ACCELERATION = "touch_acceleration";
    }

    abstract public class ContactChannelOutput<TContactInfo, TContactHistory> : ChannelOutput 
        where TContactInfo : struct 
        where TContactHistory : IContactHistory<TContactInfo>, new()
    {
        public override IEnumerable<ChannelParameterDescription> ParameterDescriptions { get { return _parameterDescriptions; } }

        public virtual bool IsInteractive { get { return gameObject.activeInHierarchy && enabled; } }
        
        static private ObjectPool<TContactHistory> _touchHistoryPool = new ObjectPool<TContactHistory>();

        static private IEnumerable<ChannelParameterDescription> _parameterDescriptions = new List<ChannelParameterDescription>()
        {
            // note: third dimension is for pressure in the case of 
            new ChannelParameterDescription(ContactChannelParameters.KEY_POSITION, 0f, 1f, 0f, 3),
            new ChannelParameterDescription(ContactChannelParameters.KEY_VELOCITY, 0f, 1f, 0f, 3),
            new ChannelParameterDescription(ContactChannelParameters.KEY_ACCELERATION, 0f, 1f, 0f, 3)
        };

        // key used for touch/finger/mouse pointer id or whatever else 
        private Dictionary<int, ChannelInstance> _contactInstances = new Dictionary<int, ChannelInstance>();

        private Dictionary<int, TContactHistory> _contactHistories = new Dictionary<int, TContactHistory>();
        

        // used for various info gathering operations on each frame
        private List<int> _idList = new List<int>();

        private bool _currentInteractive = false;
        
        abstract protected TContactInfo GetContactInfo(int index);

        abstract protected void GetStartingContacts(List<int> list);

        abstract protected void GetEndingContacts(List<int> list);

        abstract protected void ApplyContactHistoryToInstance(TContactHistory history, ChannelInstance instance);

        protected virtual void GetCurrentContacts(List<int> list)
        {
            list.AddRange(_contactHistories.Keys);
        }

        public IEnumerable<int> CurrentContactIds
        {
            get
            {
                // to list to copy so that the enumerable can then modify the collections without going sideways
                return _contactInstances.Keys.ToList(); 
            }
        }

        public bool TouchIsOngoing(int index)
        {
            return _contactHistories.ContainsKey(index);
        }

        public TContactHistory FetchTouchHistory()
        {
            TContactHistory touchHistory = _touchHistoryPool.Fetch();
            touchHistory.Reset();
            return touchHistory;
        }

        public void StoreTouchHistory(TContactHistory history)
        {
            _touchHistoryPool.Store(history);
        }



        protected virtual void SetupContacts()
        {

        }

        protected virtual void TeardownContacts()
        {
            _idList.Clear();
            GetCurrentContacts(_idList);

            foreach (int index in _idList)
            {
                EndContact(index);
            }
        }

        protected virtual void Update()
        {
            // http://answers.unity3d.com/questions/947856/how-to-detect-click-outside-ui-panel.html

            if (IsInteractive)
            {
                if (!_currentInteractive)
                {
                    SetupContacts();
                    _currentInteractive = true;
                }

                _idList.Clear();
                GetStartingContacts(_idList);
                
                foreach(int touchId in _idList)
                {
                    StartContact(touchId);
                }

                _idList.Clear();
                GetCurrentContacts(_idList);

                foreach (int touchId in _idList)
                {
                    UpdateContact(touchId);
                }

                _idList.Clear();
                GetEndingContacts(_idList);

                foreach (int touchId in _idList)
                {
                    if (TouchIsOngoing(touchId)) EndContact(touchId);
                }
            }
            else 
            {
                if (!_currentInteractive)
                {
                    TeardownContacts();
                    _currentInteractive = false;
                }
            }
            
        }

        private void StartContact(int contactId)
        {
            //Debug.Log("StartTouch " + touchId);

            ChannelInstance instance = FetchChannelInstance();
            TContactHistory history = FetchTouchHistory();

            _contactHistories[contactId] = history;
            _contactInstances[contactId] = instance;

            history.Push(GetContactInfo(contactId));

            ApplyContactHistoryToInstance(history, instance);

            CreateInstance(instance);
        }

        private void UpdateContact(int contactId)
        {
            //Debug.Log("UpdateTouch " + touchId);

            ChannelInstance instance = _contactInstances[contactId];
            TContactHistory history = _contactHistories[contactId];

            history.Push(GetContactInfo(contactId));

            ApplyContactHistoryToInstance(history, instance);

            ControlInstance(instance);
        }

        private void EndContact(int contactId)
        {
            //Debug.Log("EndTouch " + touchId);

            ChannelInstance instance = _contactInstances[contactId];
            TContactHistory history = _contactHistories[contactId];

            history.Push(GetContactInfo(contactId));

            ApplyContactHistoryToInstance(history, instance);

            DestroyInstance(instance);

            _contactHistories.Remove(contactId);
            _contactInstances.Remove(contactId);

            StoreChannelInstance(instance);
            StoreTouchHistory(history);
        }
        

    }

}
