
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Azos.Apps;
using Azos.Serialization.BSON;

namespace Azos.Instrumentation
{
  /// <summary>
  /// Represents an exception event recorded by instrumentation
  /// </summary>
  [Serializable]
  [BSONSerializable("16DA09DD-AB74-469A-A1B6-A06BEA42EDF8")]
  public class ExceptionEvent : Event, IErrorInstrument
  {
    public const string BSON_FLD_EXCEPTION_TYPE = "etp";

    /// <summary>
    /// Create event from exception instance
    /// </summary>
    public static void Record(Exception error)
    {
      var inst = ExecutionContext.Application.Instrumentation;
      if (inst.Enabled)
        inst.Record(new ExceptionEvent(error));
    }

    /// <summary>
    /// Create event from exception instance and source
    /// </summary>
    public static void Record(string source, Exception error)
    {
      var inst = ExecutionContext.Application.Instrumentation;
      if (inst.Enabled)
        inst.Record(new ExceptionEvent(source, error));
    }

    /// <summary>
    /// Create event from exception instance as of utcTime
    /// </summary>
    public static void Record(string source, Exception error, DateTime utcTime)
    {
      var inst = ExecutionContext.Application.Instrumentation;
      if (inst.Enabled)
        inst.Record(new ExceptionEvent(source, error, utcTime));
    }

    private ExceptionEvent() {}

    protected ExceptionEvent(Exception error) : base() { m_ExceptionType = error.GetType().FullName; }

    protected ExceptionEvent(string source, Exception error) : base(source) { m_ExceptionType = error.GetType().FullName; }

    protected ExceptionEvent(string source, Exception error, DateTime utcTime) : base(source, utcTime) { m_ExceptionType = error.GetType().FullName; }

    private string m_ExceptionType;

    public string ExceptionType { get { return m_ExceptionType; } }

    public override void SerializeToBSON(BSONSerializer serializer, BSONDocument doc, IBSONSerializable parent, ref object context)
    {
      base.SerializeToBSON(serializer, doc, parent, ref context);
      doc.Add(BSON_FLD_EXCEPTION_TYPE, m_ExceptionType);
    }

    [NonSerialized]
    private Dictionary<string, int> m_Errors;

    protected override Datum MakeAggregateInstance() { return new ExceptionEvent() { m_Errors = new Dictionary<string, int>() }; }

    protected override void AggregateEvent(Datum evt)
    {
      var eevt = evt as ExceptionEvent;
      if (eevt == null) return;

      if (m_Errors.ContainsKey(eevt.m_ExceptionType))
        m_Errors[eevt.m_ExceptionType] += 1;
      else
        m_Errors.Add(eevt.m_ExceptionType, 1);
    }

    protected override void SummarizeAggregation()
    {
      var sb = new StringBuilder();

      foreach (var s in m_Errors.OrderBy(p => -p.Value).Take(10).Select(p => p.Key))
      {
        sb.Append(s);
        sb.Append(", ");
      }

      m_ExceptionType = sb.ToString();
    }

    public override string ToString() { return base.ToString() + " " + (m_ExceptionType ?? string.Empty); }
  }
}
