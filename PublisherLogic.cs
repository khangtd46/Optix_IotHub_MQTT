#region StandardUsing
using System;
using FTOptix.HMIProject;
using UAManagedCore;
using FTOptix.NetLogic;
#endregion
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
using Optix_PLC_to_MQTT_asynchronous_publish;

public class PublisherLogic : BaseNetLogic
{
    private IUAVariable PlannedCut;
	private IUAVariable TargetSpeed;
	private IUAVariable EmployeeID;
	private IUAVariable MachineID;
	private IUAVariable BatchID;
	private IUAVariable messageVariable;
	private Timer timer;
	public override void Start()
    {
		X509Certificate caCert = new X509Certificate("C:\\Users\\KhangTu\\Downloads\\Optix_PLC_to_MQTT_asynchronous_publish-main\\Optix_PLC_to_MQTT_asynchronous_publish-main\\ProjectFiles\\NetSolution\\root_ca.pfx","Sm@rtCity#2025");
		X509Certificate clientCert = new X509Certificate("C:\\Users\\KhangTu\\Downloads\\Optix_PLC_to_MQTT_asynchronous_publish-main\\Optix_PLC_to_MQTT_asynchronous_publish-main\\ProjectFiles\\NetSolution\\sample_client.pfx", "Sm@rtCity#2025");
		var brokerIpAddressVariable = Project.Current.GetVariable("Model/BrokerIpAddress");

		publishClient = new MqttClient(brokerIpAddressVariable.Value,
								uPLibrary.Networking.M2Mqtt.MqttSettings.MQTT_BROKER_DEFAULT_SSL_PORT,
								true,
								caCert,
								clientCert,
								MqttSslProtocols.TLSv1_2, ValidateServerCertificate);
		publishClient.ProtocolVersion = MqttProtocolVersion.Version_3_1_1;
		// Connect to the broker
		publishClient.Connect("sample_client", "KhangIoTHub.azure-devices.net/sample_client/?api-version=2021-04-12", "");
        // Assign a callback to be executed when a message is published to the broker
        publishClient.MqttMsgPublished += PublishClientMqttMsgPublished;

		PlannedCut = Project.Current.GetVariable("Model/OEE Variable/PlannedCut");
		PlannedCut.Value = 100;

		TargetSpeed = Project.Current.GetVariable("Model/OEE Variable/TargetSpeed");
		TargetSpeed.Value = 100;

		EmployeeID = Project.Current.GetVariable("Model/OEE Variable/EmployeeID");
		EmployeeID.Value = 10014;

		MachineID = Project.Current.GetVariable("Model/OEE Variable/MachineID");
		MachineID.Value = 800;

		BatchID = Project.Current.GetVariable("Model/OEE Variable/BatchID");
		BatchID.Value = 888;

		messageVariable = Project.Current.GetVariable("Model/Message");
		ushort msgId = publishClient.Subscribe(new string[] { "devices/sample_client/messages/devicebound/#" }, // topic
					new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE }); // QoS level

		publishClient.MqttMsgPublishReceived += SubscribeClientMqttMsgPublishReceived;
		timer = new Timer(PublishDataToIoTHub, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(5));
	}

    public override void Stop()
    {
		publishClient.Disconnect();
		publishClient.MqttMsgPublished -= PublishClientMqttMsgPublished;
		timer.Dispose();
	}

    private void PublishClientMqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
    {
        Log.Info("Message " + e.MessageId + " - published = " + e.IsPublished);
    }

	public bool ValidateServerCertificate(
	object sender,
	X509Certificate certificate,
	X509Chain chain,
	SslPolicyErrors sslPolicyErrors)
	{
		// For simplicity, accept any certificate
		// In production, perform proper validation
		return true;
	}

	private MqttClient publishClient;

	private void SubscribeClientMqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
	{
		messageVariable.Value = "Message received: " + System.Text.Encoding.UTF8.GetString(e.Message);
		messageVariable.Value = System.Text.Encoding.UTF8.GetString(e.Message);
	}
	private void PublishDataToIoTHub(object state)
	{

		var PlannedCutFromHMI = Project.Current.GetVariable("Model/OEE Variable/PlannedCut");
		var TotalCutFromHMI = Project.Current.GetVariable("Model/OEE Variable/TotalCut");
		var GoodCutFromHMI = Project.Current.GetVariable("Model/OEE Variable/GoodCut");
		var TargetSpeedFromHMI = Project.Current.GetVariable("Model/OEE Variable/TargetSpeed");
		var ActualSpeedFromHMI = Project.Current.GetVariable("Model/OEE Variable/ActualSpeed");
		var UptimeFromHMI = Project.Current.GetVariable("Model/OEE Variable/Uptime");
		var EmployeeIDFromHMI = Project.Current.GetVariable("Model/OEE Variable/EmployeeID");
		var MachineIDFromHMI = Project.Current.GetVariable("Model/OEE Variable/MachineID");
		var BatchIDFromHMI = Project.Current.GetVariable("Model/OEE Variable/BatchID");
		var DowntimeFromHMI = Project.Current.GetVariable("Model/OEE Variable/Downtime");
		var DowntimeCodeFromHMI = Project.Current.GetVariable("Model/OEE Variable/DowntimeCode");
		var BadCutFromHMI = Project.Current.GetVariable("Model/OEE Variable/BadCut");
		var BadCutCodeFromHMI = Project.Current.GetVariable("Model/OEE Variable/BadCutCode");

		int plannedCut = (int)PlannedCutFromHMI.Value;
		int totalCut = 0;
		int goodCut = 0;
		int badCut = 0;
		float actualSpeed = 0;
		float upTime = 5;
		int badCutCode = 0;

		Random rnd = new Random();
		int isBadCut = rnd.Next(0, 1);

		if (isBadCut == 0)
		{
			totalCut = rnd.Next(plannedCut/2, plannedCut);
			goodCut = totalCut;
			actualSpeed = totalCut / upTime;
		}
		else
		{
			totalCut = rnd.Next(plannedCut / 2, plannedCut);
			badCut = rnd.Next(1, totalCut/2);
			goodCut = totalCut - badCut;
			actualSpeed = totalCut / upTime;
			badCutCode = 801;
		}

		var data = new OeeRawData
		{
			LocalTimestamp = DateTime.Now,
			PlannedCut = (int)PlannedCutFromHMI.Value,
			TotalCut = totalCut,
			GoodCut = goodCut,
			TargetSpeed = (float)TargetSpeedFromHMI.Value,
			ActualSpeed = actualSpeed,
			Uptime = upTime,
			EmployeeID = (int)EmployeeIDFromHMI.Value,
			MachineID = (int)MachineIDFromHMI.Value,
			BatchID = (string)BatchIDFromHMI.Value,
			Downtime = (float)DowntimeFromHMI.Value,
			DowntimeCode = (int)DowntimeCodeFromHMI.Value,
			BadCut = badCut,
			BadCutCode = badCutCode,
		};
		var jsonStringData = Newtonsoft.Json.JsonConvert.SerializeObject(data);

		// Publish a message
		ushort msgId = publishClient.Publish("devices/sample_client/messages/events/topic=123", // topic
			System.Text.Encoding.UTF8.GetBytes(jsonStringData), // message body
			MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, // QoS level
			false); // retained
	}
}
