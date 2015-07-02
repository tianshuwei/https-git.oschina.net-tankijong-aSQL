﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace aWorkbench
{
	class aSQLConnector
	{
		public class jsonResult{
			[DataMember(Order = 0)]
			public int ok;
			[DataMember(Order = 1)]
			public string result;
		}
		public enum STATUS{ OPEN,SUSPEND,CLOSE};
		private IPAddress ip;
		private int port;
		private TcpClient client;
		private Queue<String> rcvDta;
		private Thread recvDataThread;
		private int sentRequest;
		private STATUS status;
		internal STATUS Status
		{
			get
			{
				return status;
			}

		}

		public aSQLConnector(string ipString, int port)  {
			this.ip = IPAddress.Parse(ipString);
			this.port = port;
			this.sentRequest = 0;
			status = STATUS.CLOSE;
			recvDataThread = new Thread(_receive);
        }

		private bool openConnection() {
			switch (Status) {
				case STATUS.OPEN:
					return false;
				case STATUS.SUSPEND:
					return false;
				case STATUS.CLOSE:
					client = new TcpClient();
					try
					{
						client.Connect(ip, port);
						status = STATUS.OPEN;
						recvDataThread.Start();
					}
					catch (Exception e)
					{
						//do catch
						return false;
					}
					return true;
			}
			return false;//should never run to here!
		}

		public bool send(string txt,bool keepOpen=true) {
			if (status != STATUS.OPEN)
				if (!openConnection()) return false;
			NetworkStream stream = client.GetStream();
			byte[] sd = Encoding.Default.GetBytes(txt);
			stream.Write(sd, 0, sd.Length);
			stream.Close();
			sentRequest++;
			if (!keepOpen) client.Close();
			return true;
		}

		private void _receive() {
			byte[] rcvBuf = new byte[1024];
			int l;
            while (true)
			{
				if (status != STATUS.OPEN) continue;
				l = client.Client.Receive(rcvBuf);
				rcvDta.Enqueue(Encoding.UTF8.GetString(rcvBuf, 0, l));
			}
		}


		public string receive() {
			if (sentRequest > 0)
			{
				while (rcvDta.Count != sentRequest) ;
				return rcvDta.Dequeue();
			}
			else return null;
		}
	}
}
