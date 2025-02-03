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
    private IUAVariable variable1;
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

		variable1 = Project.Current.GetVariable("Model/Variable1");

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

		var PlannedCut = Project.Current.GetVariable("Model/OEE Variable/PlannedCut");
		var TotalCut = Project.Current.GetVariable("Model/OEE Variable/TotalCut");
		var GoodCut = Project.Current.GetVariable("Model/OEE Variable/GoodCut");
		var TargetSpeed = Project.Current.GetVariable("Model/OEE Variable/TargetSpeed");
		var ActualSpeed = Project.Current.GetVariable("Model/OEE Variable/ActualSpeed");
		var Uptime = Project.Current.GetVariable("Model/OEE Variable/Uptime");
		var EmployeeID = Project.Current.GetVariable("Model/OEE Variable/EmployeeID");
		var MachineID = Project.Current.GetVariable("Model/OEE Variable/MachineID");
		var BatchID = Project.Current.GetVariable("Model/OEE Variable/BatchID");
		var Downtime = Project.Current.GetVariable("Model/OEE Variable/Downtime");
		var DowntimeCode = Project.Current.GetVariable("Model/OEE Variable/DowntimeCode");
		var BadCut = Project.Current.GetVariable("Model/OEE Variable/BadCut");
		var BadCutCode = Project.Current.GetVariable("Model/OEE Variable/BadCutCode");

		var data = new OeeRawData
		{
			LocalTimestamp = DateTime.Now,
			PlannedCut = (int)PlannedCut.Value,
			TotalCut = (int)TotalCut.Value,
			GoodCut = (int)GoodCut.Value,
			TargetSpeed = (float)TargetSpeed.Value,
			ActualSpeed = (float)ActualSpeed.Value,
			Uptime = (float)Uptime.Value,
			EmployeeID = (int)EmployeeID.Value,
			MachineID = (int)MachineID.Value,
			BatchID = (string)BatchID.Value,
			Downtime = (float)Downtime.Value,
			DowntimeCode = (int)DowntimeCode.Value,
			BadCut = (int)BadCut.Value,
			BadCutCode = (int)BadCutCode.Value,
		};
		var jsonStringData = Newtonsoft.Json.JsonConvert.SerializeObject(data);

		// Publish a message
		ushort msgId = publishClient.Publish("devices/sample_client/messages/events/topic=123", // topic
			System.Text.Encoding.UTF8.GetBytes(jsonStringData), // message body
			MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, // QoS level
			false); // retained
	}
}
