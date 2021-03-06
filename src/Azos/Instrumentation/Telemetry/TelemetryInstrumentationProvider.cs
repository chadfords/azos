
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Azos.Log;
using Azos.Conf;

namespace Azos.Instrumentation.Telemetry
{
  /// <summary>
  /// Represents a provider that writes aggregated datums into remote telemetry receiver
  /// </summary>
  public class TelemetryInstrumentationProvider : LogInstrumentationProvider
  {
    #region .ctor
    public TelemetryInstrumentationProvider(InstrumentationService director) : base(director) { }

    protected override void Destructor()
    {
      cleanupClient();
      base.Destructor();
    }
    #endregion

    #region Fields
    private TelemetryReceiverClient m_Client;
    #endregion

    #region Properties
    /// <summary>
    /// Determines whether to write to log as well
    /// </summary>
    [Config]
    public bool UseLog { get; set; }

    /// <summary>
    /// Provides remote telemetry receiver node
    /// </summary>
    [Config]
    public string ReceiverNode { get; set; }

    /// <summary>
    /// Provides name for reporting site, if this property is blank then App.Name is used instead
    /// </summary>
    [Config]
    public string SiteName { get; set; }
    #endregion

    #region Public
    protected internal override void Write(Datum aggregatedDatum, object batchContext, object typeContext)
    {
      if (!App.Available) return;

      if (UseLog)
        base.Write(aggregatedDatum, batchContext, typeContext);

      var node = ReceiverNode;

      if (node.IsNullOrWhiteSpace()) return;


      string site;
      if (SiteName.IsNotNullOrWhiteSpace())
        site = SiteName;
      else
        site = "{0}::{1}@{2}".Args(App.Name, App.InstanceID, System.Environment.MachineName);

      try
      {
        if (m_Client == null)
          m_Client = new TelemetryReceiverClient(ReceiverNode);

        m_Client.Send(site, aggregatedDatum);
      }
      catch (Exception error)
      {
        cleanupClient();
        WriteLog(MessageType.Error, error.ToMessageWithType(), from: "{0}.{1}".Args(GetType().Name, "Write(datum)"));
      }
    }
    #endregion

    #region Private
    private void cleanupClient()
    {
      var cl = m_Client;
      if (cl == null) return;

      try
      {
        m_Client = null;
        cl.Dispose();
      }
      catch (Exception error)
      {
        WriteLog(MessageType.Error, error.ToMessageWithType(), from: "{0}.{1}".Args(GetType().Name, "cleanupClient()"));
      }
    }
    #endregion
  }
}
