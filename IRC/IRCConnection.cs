﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace IRC
{
	public class IRCConnection
		: IIRCConnection, IDisposable
	{

		#region Fields

		private string _host;
		private ushort _port;
		private string _nick;

		private bool _quit = false;

		private TcpClient _client;
		private NetworkStream _stream;
		private StreamReader _reader;
		private StreamWriter _writer;
		private Thread _listenThread;

		private List<string> _channels
			 = new List<string>();

		#endregion

		#region Events

		public event EventHandler<IRCCommandEventArgs> MessageReceived;

		public void RaiseMessageReceived(IRCMessage message)
		{
			if (MessageReceived != null)
				MessageReceived(this, new IRCCommandEventArgs(message));
		}

		#endregion

		public IRCConnection(string host, ushort port, string nick, bool connect)
		{
			_host = host;
			_port = port;
			_nick = nick;

			if (connect)
				Connect();
		}

		public void Connect()
		{
			_client = new TcpClient(_host, _port);
			_stream = _client.GetStream();
			_reader = new StreamReader(_stream);
			_writer = new StreamWriter(_stream);
			_listenThread = new Thread(Listen);
			_listenThread.Start();

			IRCMessage message;

			// Send USER
			message = new IRCMessage();
			message.Command = "USER";
			message.Params.Add(_nick);
			message.Params.Add("0");
			message.Params.Add("*");
			message.Message = _nick;
			SendRawMessage(message);

			// Send NICK
			message = new IRCMessage();
			message.Command = "NICK";
			message.Params.Add(_nick);
			SendRawMessage(message);
		}

		public void Disconnect()
		{
			Disconnect(null);
		}

		public void Disconnect(string message)
		{
			_quit = true;

			IRCMessage ircMessage = new IRCMessage();
			ircMessage.Command = "QUIT";

			if (!String.IsNullOrEmpty(message))
				ircMessage.Message = message;

			SendRawMessage(ircMessage);
		}

		#region Channels

		public void Join(string channel)
		{
			if (String.IsNullOrEmpty(channel))
				throw new ArgumentNullException("channel");

			channel = channel.ToLowerInvariant();
			if (_channels.Contains(channel))
				return;

			IRCMessage message = new IRCMessage();
			message.Command = "JOIN";
			message.Params.Add("#" + channel);
			SendRawMessage(message);

			_channels.Add(channel);
		}

		public void Leave(string channel)
		{
			if (String.IsNullOrEmpty(channel))
				throw new ArgumentNullException("channel");

			channel = channel.ToLowerInvariant();
			if (!_channels.Contains(channel))
				return;

			IRCMessage message = new IRCMessage();
			message.Command = "PART";
			message.Params.Add("#" + channel);
			SendRawMessage(message);

			_channels.Remove(channel);
		}

		#endregion

		#region Methods - Private

		/// <summary>
		///  Handles the PING message
		/// </summary>
		private void HandlePing(IRCMessage message)
		{
			message.Command = "PONG";
			SendRawMessage(message);
		}

		private void Listen()
		{
			while (!_quit)
			{
				string rawMessage = _reader.ReadLine();
				if (String.IsNullOrEmpty(rawMessage))
					continue;

				IRCMessage message = IRCMessage.FromString(rawMessage);

				// Setup some basic handles
				if (message.Command == "PING")
					HandlePing(message);

				RaiseMessageReceived(message);
			}

			Console.WriteLine("Stopped listening...");
		}

		#endregion

		#region IIRCConnection Members

		public void SendRawMessage(IRCMessage message)
		{
			_writer.WriteLine(message.ToString());
			_writer.Flush();
		}

		public void SendChannelMessage(string channel, string message)
		{
			IRCMessage ircMessage = new IRCMessage();
			ircMessage.Command = "PRIVMSG";
			ircMessage.Params.Add("#" + channel);
			ircMessage.Message = message;

			SendRawMessage(ircMessage);
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Disconnect("Bye bye...");
		}

		#endregion

	}
}
