using System;

namespace Optix_PLC_to_MQTT_asynchronous_publish
{
	public class OeeRawData
	{
		public DateTime? LocalTimestamp { get; set; }
		public int? PlannedCut { get; set; }
		public int? TotalCut { get; set; }
		public int? GoodCut { get; set; }
		public float? TargetSpeed { get; set; }
		public float? ActualSpeed { get; set; }
		public float? Uptime { get; set; }
		public int? EmployeeID { get; set; }
		public int? MachineID { get; set; }
		public string? BatchID { get; set; }
		public float? Downtime { get; set; }
		public int? DowntimeCode { get; set; }
		public int? BadCut { get; set; }
		public int? BadCutCode { get; set; }
	}
}
