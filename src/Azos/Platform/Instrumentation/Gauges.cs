/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Azos.Apps;
using Azos.Instrumentation;
using Azos.Serialization.BSON;

namespace Azos.Platform.Instrumentation
{
  [Serializable]
  public abstract class OSLongGauge : LongGauge
  {
    protected OSLongGauge(string src, long value) : base(src, value) { }
  }

  [Serializable]
  public abstract class OSDoubleGauge : DoubleGauge
  {
    protected OSDoubleGauge(string src, double value) : base(src, value) { }
  }


  [Serializable]
  [BSONSerializable("8CAEA169-881F-43E4-92C8-6D774E913BB4")]
  public class CPUUsage : OSLongGauge, ICPUInstrument
  {
    protected CPUUsage(string src, long value) : base(src, value) { }

    public static void Record(long value, string src = null)
    {
      var inst = App.Instrumentation;
      if (inst.Enabled)
        inst.Record(new CPUUsage(src, value));
    }


    public override string Description { get { return "CPU Usage %"; } }
    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_PERCENT; } }


    protected override Datum MakeAggregateInstance()
    {
      return new CPUUsage(this.Source, 0);
    }
  }

  [Serializable]
  [BSONSerializable("F87CE5FF-12AE-4FBA-83A7-479AB63E0C07")]
  public class RAMUsage : OSLongGauge, IMemoryInstrument
  {
    protected RAMUsage(long value) : base(null, value) { }

    public static void Record(long value)
    {
      var inst = App.Instrumentation;
      if (inst.Enabled)
        inst.Record(new RAMUsage(value));
    }


    public override string Description { get { return "RAM Usage %"; } }
    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_PERCENT; } }


    protected override Datum MakeAggregateInstance()
    {
      return new RAMUsage(0);
    }
  }

  [Serializable]
  [BSONSerializable("C94378D4-8334-496D-AB28-ADF620071E97")]
  public class AvailableRAM : OSLongGauge, IMemoryInstrument
  {
    protected AvailableRAM(string src, long value) : base(src, value) { }

    public static void Record(long value, string src = null)
    {
      var inst = App.Instrumentation;
      if (inst.Enabled)
        inst.Record(new AvailableRAM(src, value));
    }


    public override string Description { get { return "Available RAM mb."; } }
    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_MB; } }


    protected override Datum MakeAggregateInstance()
    {
      return new AvailableRAM(this.Source, 0);
    }
  }
}
