/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;


using Azos.Conf;
using Azos.Time;

namespace Azos.Apps
{
    /// <summary>
    /// Represents a lightweight service that can be controlled by Start/SignalStop-like commands.
    /// This class serves a a base for various implementations (i.e. LogService) including their composites.
    /// This class is thread-safe
    /// </summary>
    public abstract class Service : ApplicationComponent, IService, ILocalizedTimeProvider
    {
        #region CONSTS
              public const string CONFIG_NAME_ATTR = "name";

        #endregion



        #region .ctor


                protected Service() : base()
                {
                }

                protected Service(object director) : base(director)
                {
                }

                protected override void Destructor()
                {
                   SignalStop();
                   WaitForCompleteStop();

                   base.Destructor();
                }


        #endregion

        #region Private Fields

            private object m_StatusLock = new object();
            private volatile ServiceStatus m_Status;

            private volatile bool m_PendingWaitingStop;

            [Config]
            private string m_Name;

            [Config(TimeLocation.CONFIG_TIMELOCATION_SECTION)]
            private TimeLocation m_TimeLocation = new TimeLocation();

        #endregion


        #region Properties

            /// <summary>
            /// Checks whether the class is decorated with ApplicationDontAutoStartServiceAttribute
            /// </summary>
            public bool ApplicationDontAutoStartService
            {
              get{ return Attribute.IsDefined(GetType(), typeof(ApplicationDontAutoStartServiceAttribute));}
            }

            /// <summary>
            /// Current service status
            /// </summary>
            public ServiceStatus Status
            {
                get { return m_Status; }
            }

            /// <summary>
            /// Returns true when service is active or about to become active.
            /// Check in service implementation loops/threads/tasks
            /// </summary>
            public bool Running
            {
                get
                {
                  var status = m_Status;
                  return status == ServiceStatus.Active || status == ServiceStatus.Starting;
                }
            }

            /// <summary>
            /// Provides textual name for the service
            /// </summary>
            public string Name
            {
                get { return m_Name ?? this.GetType().Name; }
                protected set { m_Name = value; }
            }

            /// <summary>
            /// Returns time location of this LocalizedTimeProvider implementation
            /// </summary>
            public TimeLocation TimeLocation
            {
                get { return m_TimeLocation ?? TimeLocation.Parent;}
                set { m_TimeLocation = value; }
            }

            /// <summary>
            /// Returns current time localized per TimeLocation
            /// </summary>
            public DateTime LocalizedTime
            {
                get { return UniversalTimeToLocalizedTime(App.TimeSource.UTCNow); }
            }


        #endregion

        #region Public

            /// <summary>
            /// Configures service from configuration node (and possibly it's sub-nodes)
            /// </summary>
            public void Configure(IConfigSectionNode fromNode)
            {
                EnsureObjectNotDisposed();
                lock (m_StatusLock)
                {
                    CheckServiceInactive();
                    ConfigAttribute.Apply(this, fromNode);
                    DoConfigure(fromNode);
                }
            }

            /// <summary>
            /// Blocking call that starts the service instance
            /// </summary>
            internal bool StartByApplication()
            {
                if (ApplicationDontAutoStartService) return false;
                Start();
                return true;
            }

            /// <summary>
            /// Blocking call that starts the service instance
            /// </summary>
            public void Start()
            {
                EnsureObjectNotDisposed();
                lock (m_StatusLock)
                    if (m_Status == ServiceStatus.Inactive)
                    {
                        m_Status = ServiceStatus.Starting;
                        try
                        {
                          Behavior.ApplyBehaviorAttributes(this);
                          DoStart();
                          m_Status = ServiceStatus.Active;
                        }
                        catch
                        {
                          m_Status = ServiceStatus.Inactive;
                          throw;
                        }
                    }
            }

            /// <summary>
            /// Non-blocking call that initiates the stopping of the service
            /// </summary>
            public void SignalStop()
            {
                lock (m_StatusLock)
                    if (m_Status == ServiceStatus.Active)
                    {
                        m_Status = ServiceStatus.Stopping;
                        DoSignalStop();
                    }
            }


            /// <summary>
            /// Non-blocking call that returns true when the service instance has completely stopped after SignalStop()
            /// </summary>
            public bool CheckForCompleteStop()
            {
                lock (m_StatusLock)
                {
                    if (m_Status == ServiceStatus.Inactive) return true;

                    if (m_Status == ServiceStatus.Stopping)
                        return DoCheckForCompleteStop();
                    else
                        return false;
                }
            }

            /// <summary>
            /// Blocks execution of current thread until this service has completely stopped.
            /// This call must be performed by only 1 thread otherwise exception is thrown
            /// </summary>
            public void WaitForCompleteStop()
            {
                lock (m_StatusLock)
                {
                    if (m_Status == ServiceStatus.Inactive) return;

                    if (m_Status != ServiceStatus.Stopping) SignalStop();

                    if (m_PendingWaitingStop) throw new AzosException(StringConsts.SERVICE_INVALID_STATE + "{0}.{1}".Args(Name,"WaitForCompleteStop() already blocked"));

                    m_PendingWaitingStop = true;
                    try
                    {
                      DoWaitForCompleteStop();
                    }
                    finally
                    {
                      m_PendingWaitingStop = false;
                    }

                    m_Status = ServiceStatus.Inactive;
                }
            }


            /// <summary>
            /// Accepts a visit of a manager entity - this call is useful for periodic updates of service status,
            /// i.e.  when service does not have a thread of its own it can be periodically managed by some other service through this method.
            /// The default implementation of DoAcceptManagerVisit(object, DateTime) does nothing
            /// </summary>
            public void AcceptManagerVisit(object manager, DateTime managerNow)
            {
              if (!Running) return;
              //call rooted at non-virt method for future extensibility with state checks here
              EnsureObjectNotDisposed();
              DoAcceptManagerVisit(manager, managerNow);
            }

            /// <summary>
            /// Converts universal time to local time as of TimeLocation property
            /// </summary>
            public DateTime UniversalTimeToLocalizedTime(DateTime utc)
            {
                if (utc.Kind!=DateTimeKind.Utc)
                 throw new TimeException(StringConsts.ARGUMENT_ERROR+GetType().Name+".UniversalTimeToLocalizedTime(utc.Kind!=UTC)");

                var loc = TimeLocation;
                if (!loc.UseParentSetting)
                {
                   return DateTime.SpecifyKind(utc + loc.UTCOffset, DateTimeKind.Local);
                }
                else
                {
                  if (ComponentDirector is ILocalizedTimeProvider)
                    return ((ILocalizedTimeProvider)ComponentDirector).UniversalTimeToLocalizedTime(utc);

                  return App.UniversalTimeToLocalizedTime(utc);
                }
            }

            /// <summary>
            /// Converts localized time to UTC time as of TimeLocation property
            /// </summary>
            public DateTime LocalizedTimeToUniversalTime(DateTime local)
            {
                if (local.Kind!=DateTimeKind.Local)
                 throw new TimeException(StringConsts.ARGUMENT_ERROR+GetType().Name+".LocalizedTimeToUniversalTime(utc.Kind!=Local)");

                var loc = TimeLocation;
                if (!loc.UseParentSetting)
                {
                   return DateTime.SpecifyKind(local - loc.UTCOffset, DateTimeKind.Utc);
                }
                else
                {
                   if (ComponentDirector is ILocalizedTimeProvider)
                    return ((ILocalizedTimeProvider)ComponentDirector).LocalizedTimeToUniversalTime(local);

                  return App.LocalizedTimeToUniversalTime(local);
                }
            }



        #endregion


        #region Protected

            /// <summary>
            /// Allows to abort unsuccessful DoStart() overridden implementation.
            /// This method must be called from within DoStart()
            /// </summary>
            protected void AbortStart()
            {
                var trace = new StackTrace(1, false);

                if (!trace.GetFrames().Any(f => f.GetMethod().Name.Equals("DoStart", StringComparison.Ordinal)))
                    Debugging.Fail(
                        text:   "Service.AbortStart() must be called from within DoStart()",
                        action: DebugAction.ThrowAndLog);

                m_Status = ServiceStatus.AbortingStart;
            }

            /// <summary>
            /// Provides implementation that starts the service
            /// </summary>
            protected virtual void DoStart()
            {
            }

            /// <summary>
            /// Provides implementation that signals service to stop. This is expected not to block
            /// </summary>
            protected virtual void DoSignalStop()
            {
            }

            /// <summary>
            /// Provides implementation for checking whether the service has completely stopped
            /// </summary>
            protected virtual bool DoCheckForCompleteStop()
            {
                return m_Status == ServiceStatus.Inactive;
            }

            /// <summary>
            /// Provides implementation for a blocking call that returns only after a complete service stop
            /// </summary>
            protected virtual void DoWaitForCompleteStop()
            {
            }


            /// <summary>
            /// Provides implementation that configures service from configuration node (and possibly it's sub-nodes)
            /// </summary>
            protected virtual void DoConfigure(IConfigSectionNode node)
            {

            }

            /// <summary>
            /// Checks for service inactivity and throws exception if service is running (started, starting or stopping)
            /// </summary>
            protected void CheckServiceInactive()
            {
                if (Status!= ServiceStatus.Inactive)
                throw new AzosException(StringConsts.SERVICE_INVALID_STATE + Name);
            }

            /// <summary>
            /// Checks for service activity and throws exception if service is not in ControlStatus.Active state
            /// </summary>
            protected void CheckServiceActive()
            {
                if (m_Status!=ServiceStatus.Active)
                throw new AzosException(StringConsts.SERVICE_INVALID_STATE + Name);
            }

            /// <summary>
            /// Checks for service activity and throws exception if service is not in ControlStatus.Active state
            /// </summary>
            protected void CheckServiceActiveOrStarting()
            {
                if (m_Status!=ServiceStatus.Active && m_Status!=ServiceStatus.Starting)
                throw new AzosException(StringConsts.SERVICE_INVALID_STATE + Name);
            }


            /// <summary>
            /// Accepts a visit from external manager. Base implementation does nothing.
            ///  Override in services that need external management calls
            ///   to update their state periodically, i.e. when they don't have a thread on their own
            /// </summary>
            protected virtual void DoAcceptManagerVisit(object manager, DateTime managerNow)
            {

            }

            /// <summary>
            /// WARNING: Developers never call this method!!!
            /// It is used by advanced derived implementations that need to synchronize status updates.
            /// We do not want to make statuslock protected as it is almost never needed, hence this accessor
            /// </summary>
            protected object ____ObtainPrivateServiceStatusLockObject()
            {
              return m_StatusLock;
            }

        #endregion


        #region Private Utils


        #endregion


    }



    /// <summary>
    /// Represents service with typed ComponentDirector property
    /// </summary>
    public class Service<TDirector> : Service where TDirector : class
    {

        protected Service() : base()
        {
        }

        protected Service(TDirector director) : base(director)
        {
        }

        public new TDirector ComponentDirector
        {
            get { return (TDirector)base.ComponentDirector; }
        }

    }


}
