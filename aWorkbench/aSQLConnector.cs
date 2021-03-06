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
		private static aSQLConnector instance=null;
		private STATUS status;
		internal STATUS Status
		{
			get
			{
				return status;
			}

		}

		private aSQLConnector(string ipString, int port)  {
			this.ip = IPAddress.Parse(ipString);
			this.port = port;
			this.sentRequest = 0;
			status = STATUS.CLOSE;
			openConnection();
			rcvDta = new Queue<String>();
			//recvDataThread = new Thread(_receive);
			instance = this;
			//recvDataThread.Start();
		}

		public static aSQLConnector getInstance(string ipString, int port) {
			if (instance == null) instance = new aSQLConnector(ipString, port);
			return instance;
		}

		public static aSQLConnector getInstance()
		{
			return instance;
		}

		private bool openConnection() {
			switch (Status) {
				case STATUS.OPEN:
					return true;
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

		public bool send(string txt) {
			if (status != STATUS.OPEN) openConnection();
			txt = string.Format("\x1B^\x1B,{0}\x1B,\x1B$", txt);
			NetworkStream stream = client.GetStream();
			byte[] sd = Encoding.Default.GetBytes(txt);
			stream.Write(sd, 0, sd.Length);
			sentRequest++;
			byte[] rcvBuf = new byte[1024];
			int l = 0;
			while (l == 0)
				l = client.Client.Receive(rcvBuf);
			String xx = Encoding.UTF8.GetString(rcvBuf, 0, l);
			rcvDta.Enqueue(Encoding.UTF8.GetString(rcvBuf, 0, l));
			client.Close();
			status = STATUS.CLOSE;
			return true;
		}

		private void _receive() {
			byte[] rcvBuf = new byte[1024];
			int l = 0;
            while (true)
			{
				if (status != STATUS.OPEN) continue;
				try
				{
					l = client.Client.Receive(rcvBuf);
				}
				catch (SocketException e) {
					status = STATUS.CLOSE;
					openConnection();

				}
				if (l==0) continue;
				String xx = Encoding.UTF8.GetString(rcvBuf, 0, l);
				rcvDta.Enqueue(Encoding.UTF8.GetString(rcvBuf, 0, l));
			}
		}


		public string receive() {
			if (sentRequest > 0)
			{
				while (rcvDta.Count != sentRequest) ;
				sentRequest--;
				return rcvDta.Dequeue();
			}
			else return null;
		}
	}
}
