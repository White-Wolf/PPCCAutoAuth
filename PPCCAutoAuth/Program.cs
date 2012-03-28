using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NativeWifi;

namespace PPCCAutoAuth
{
	class Program
	{
		static WlanClient.WlanInterface wifi = null;
		static Boolean authenticated = false;
		static Boolean closing = false;
		static Boolean isConnected = false;
		static Int16 sleepTime = 1000;
		static WlanClient wlan = new WlanClient();
		static Int16 spinnerAni = 0;

		static void Main(string[] args)
		{
			while (!closing)
			{
				while (isConnected && authenticated)
				{
					System.Threading.Thread.Sleep(sleepTime);
				}

				if (!isConnected)
				{
					Console.Write("Waiting for the network to connect to PPCCStudent...|");
				}

				checkConnection();

				while (!isConnected)
				{
					Spinner(52);

					if (wifi == null || !isConnected)
						checkConnection();
					System.Threading.Thread.Sleep(500);
				}

				Console.WriteLine();
				spinnerAni = 0;
				Int16 waitTime = 0;
				Console.Write("Connected to PPCCStudents, waiting for IP...|");
				while (!hasIP() && waitTime < 2000) { Spinner(44); System.Threading.Thread.Sleep(100); waitTime += 100; }
				Console.WriteLine();
				if (waitTime == 2000) //It's taking longer than usual for an IP
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("* It's taking longer than usual to get an IP...");
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.Write("Still waiting for an IP...|");
					while (!hasIP() && waitTime < 5000) { Spinner(26); System.Threading.Thread.Sleep(100); waitTime += 100; }
					Console.WriteLine();
				}

				if (!hasIP()) //If we don't have an IP by now, then quit.
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("!-- It took too long to get an IP --! Quitting...");
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine("Press any key to exit...");
					Console.ReadKey();
					closing = true;
					break;
				}

				if (!authenticated)
				{
					Console.WriteLine("Attempting to authenticate...");
					if (Ping("google.com")) //make sure we're not already authenticated
					{
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("Already Authenticated");
						Console.ForegroundColor = ConsoleColor.Gray;
						authenticated = true;
					}
					else
					{
						if (!Authenticate(true))
						{
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("!-- HTTPS Validation Failed --! Falling back to HTTP");
							Console.ForegroundColor = ConsoleColor.Gray;
							if (!Authenticate())
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.Write("!-- Unable to authenticate --!");
								Console.ForegroundColor = ConsoleColor.Gray;
								Console.WriteLine("Both HTTP and HTTPS authentications failed!");
								Console.WriteLine("Try again (y/n)? ");
								string tryAgain = Console.ReadLine();
								closing = (tryAgain.ToLower() == "no" || tryAgain.ToLower() == "n") ? true : false;
								sleepTime = 0;
							}
						}
					}
				}
			}
		}

		#region Authenticate
		public static bool Authenticate()
		{
			return Authenticate(false);
		}
		public static bool Authenticate(Boolean HTTPS)
		{
			if (HTTPS)
			{
#if (!DEBUG)
				try{
#endif
					WebRequest req = WebRequest.Create("https://1.1.1.1/login.html");
					req.Timeout = 2000;
					req.Method = "POST";
					req.AuthenticationLevel = System.Net.Security.AuthenticationLevel.None;
					ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);
					string postdata = "buttonClicked=4&redirect_url=www.google.com%2F&err_flag=0&email=drgn-hearted%40hotmail.com";
					req.ContentType = "application/x-www-form-urlencoded";
					req.ContentLength = Encoding.UTF8.GetByteCount(postdata);
					Stream datastream = req.GetRequestStream();
					datastream.Write(Encoding.UTF8.GetBytes(postdata), 0, Encoding.UTF8.GetByteCount(postdata));
					datastream.Close();
					WebResponse resp = req.GetResponse();
					StreamReader reader = new StreamReader(resp.GetResponseStream());
					string response = reader.ReadToEnd();
					if (response.Contains("<title>Logged In</title>"))
					{
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("--- Authenticated ---");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("You are now connected to the internet");
						authenticated = true;
						return true;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.Write("!-- Unable to authenticate --!");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("The server returned something other than the 'logged in' page; possibly means you are already authenticated, or there is an issue with the authentication url");
						Console.WriteLine("Try again (y/n)? ");
						string tryAgain = Console.ReadLine();
						closing = (tryAgain.ToLower() == "no" || tryAgain.ToLower() == "n") ? true : false;
						sleepTime = 0;
					}
				
#if (!DEBUG)
				} catch (Exception e) {
					if (!(e.GetBaseException() is WebException)) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("!-- Unable to authenticate --!");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("Check your network settings and make sure you have an ip");
						Console.ForegroundColor = ConsoleColor.Red;
						Console.Write("Error:");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("\t" + e.Message);
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine("\tDeveloper Info:");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("\t\t" + e.InnerException);
						Console.WriteLine();
						Console.WriteLine();
						Console.WriteLine("Press any key to retry...");
						Console.ReadKey();
					}
				}
#endif
			}
			else
			{
#if (!DEBUG)
				try {
#endif
					WebRequest req = WebRequest.Create("http://1.1.1.1/login.html?buttonClicked=4&redirect_url=www.google.com%2F&err_flag=0&email=drgn-hearted%40hotmail.com");
					req.Timeout = 3000;
					WebResponse resp = req.GetResponse();
					StreamReader reader = new StreamReader(resp.GetResponseStream());
					string response = reader.ReadToEnd();
					if (response.Contains("<title>Logged In</title>"))
					{
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("--- Authenticated ---");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("You are now connected to the internet");
						authenticated = true;
						return true;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.Write("!-- Unable to authenticate --!");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("The server returned something other than the 'logged in' page; possibly means you are already authenticated, or there is an issue with the authentication url");
						Console.WriteLine("Try again (y/n)? ");
						string tryAgain = Console.ReadLine();
						closing = (tryAgain.ToLower() == "no" || tryAgain.ToLower() == "n") ? true : false;
						sleepTime = 0;
					}
#if(!DEBUG)
				} catch (Exception e) {
					if (!(e.GetBaseException() is WebException))
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("!-- Unable to authenticate --!");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("Check your network settings and make sure you have an ip");
						Console.ForegroundColor = ConsoleColor.Red;
						Console.Write("Error:");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("\t" + e.Message);
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine("\tDeveloper Info:");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("\t\t" + e.InnerException);
						Console.WriteLine();
						Console.WriteLine();
						Console.WriteLine("Press any key to retry...");
						Console.ReadKey();
					}
				}
#endif
			}
			return false;
		}
		#endregion

		#region Check Connection
		static void checkConnection()
		{
#if(!DEBUG)
			try {
#endif
			if (wifi == null)
			{
				foreach (WlanClient.WlanInterface wlanInt in wlan.Interfaces)
				{
					if (wlanInt.InterfaceState == Wlan.WlanInterfaceState.Connected)
					{
						Wlan.Dot11Ssid ssid = wlanInt.CurrentConnection.wlanAssociationAttributes.dot11Ssid;
						string strSSID = new String(Encoding.ASCII.GetChars(ssid.SSID, 0, (int)ssid.SSIDLength));
						if (strSSID.ToLower() == "ppccstudents")
						{
							wifi = wlanInt;
							wifi.WlanNotification += new WlanClient.WlanInterface.WlanNotificationEventHandler(wifi_WlanNotification);
							sleepTime = 10000;
							isConnected = true;
						}
					}
				}
			}
			else
			{
				if (wifi.InterfaceState == Wlan.WlanInterfaceState.Connected)
				{
					Wlan.Dot11Ssid ssid = wifi.CurrentConnection.wlanAssociationAttributes.dot11Ssid;
					string strSSID = new String(Encoding.ASCII.GetChars(ssid.SSID, 0, (int)ssid.SSIDLength));
					if (strSSID.ToLower() == "ppccstudents")
					{
						sleepTime = 10000;
						isConnected = true;
					}
				}
				else
				{
					isConnected = false;
					authenticated = false;
					sleepTime = 1000;
				}
			}
#if(!DEBUG)
			} catch (Exception e) {
				
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("!-- Unable to query the Wireless NIC. Shutting down --!");
				Console.ForegroundColor = ConsoleColor.Gray;
				closing = true;
			}
#endif
		}
#endregion
		#region Has IP
		static Boolean hasIP()
		{
			if (wifi.NetworkInterface.GetIPProperties().UnicastAddresses[1].Address.Equals(null))
				return false;
			if (wifi.NetworkInterface.GetIPProperties().UnicastAddresses[1].Address.ToString().StartsWith("169."))
				return false;
			return true;
		}
		#endregion
		#region Ping
		static Boolean Ping(string host)
		{
			if (wifi == null || wifi.InterfaceState != Wlan.WlanInterfaceState.Connected)
				return false;
			System.Net.NetworkInformation.Ping pinger = new System.Net.NetworkInformation.Ping();
			try
			{
				if (pinger.Send(host, 1000).Status == System.Net.NetworkInformation.IPStatus.Success)
					return true;
			}
			catch (Exception e)
			{
				return false;
			}
			return false;
		}
		#endregion
		
		#region Spinner
		static void Spinner(int pos)
		{
			Console.CursorLeft = pos; //52;
			switch (spinnerAni)
			{
				case 0:
					Console.Write("/");
					spinnerAni++;
					break;
				case 1:
					Console.Write("-");
					spinnerAni++;
					break;
				case 2:
					Console.Write("\\");
					spinnerAni++;
					break;
				case 3:
					Console.Write("|");
					spinnerAni = 0;
					break;
			}
		}
		#endregion

		#region Certificate Validation Override
		private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
		{
			//we do this since PPCC's 1.1.1.1 certificate doesn't match and would fail validation
			return true;
		}
		#endregion

		#region Wifi Notification
		static void wifi_WlanNotification(Wlan.WlanNotificationData notifyData)
		{
			if (notifyData.notificationSource == Wlan.WlanNotificationSource.MSM)
			{
				if((Wlan.WlanNotificationCodeMsm)notifyData.NotificationCode == Wlan.WlanNotificationCodeMsm.Disconnected)
				{
					Console.WriteLine("Disconnected from the PPCCStudents network");
					isConnected = false;
					authenticated = false;
					sleepTime = 1000;
				}

				if ((Wlan.WlanNotificationCodeMsm)notifyData.NotificationCode == Wlan.WlanNotificationCodeMsm.Connected)
				{
					checkConnection();
				}
			}
		}
		#endregion
	}
}
