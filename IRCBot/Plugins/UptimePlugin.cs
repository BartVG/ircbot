
using System;

using IRC;

namespace IRCBot.Plugins
{
	class UptimePlugin
		: IRCPluginBase
	{
		static readonly DateTime StartTime = DateTime.Now;

		protected override bool Initialize()
		{
			Bot.SubscribeToMessage("^!uptime$", HandleMessage);
			return true;
		}

		protected void HandleMessage(IRCMessage message)
		{
			TimeSpan uptime = (DateTime.Now - StartTime);

			string uptimeMSG = "I'm running for ";
			if (uptime.Days != 0) uptimeMSG += uptime.Days + " days, ";
			if (uptime.Hours != 0) uptimeMSG += uptime.Hours + " hours, ";
			if (uptime.Minutes != 0) uptimeMSG += uptime.Minutes + " minutes and ";
			uptimeMSG += uptime.Seconds + " seconds.";

			Bot.SendChannelMessage(message.Channel, uptimeMSG);
		}
	}
}
